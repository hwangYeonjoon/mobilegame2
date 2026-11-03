using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;

namespace ScrapGuard.Server;

public sealed class ApiFault(int status, string message, ServerReply reply = null) : Exception(message)
{
    public int Status { get; } = status;
    public ServerReply Reply { get; } = reply;
}

public sealed partial class GameRepository
{
    public static readonly JsonSerializerOptions Json = new() { IncludeFields = true, PropertyNameCaseInsensitive = true };
    readonly string connectionString;
    readonly Func<DateTimeOffset> clock;
    readonly object gate = new();
    public GameRepository(string path, Func<DateTimeOffset> clock = null)
    {
        this.clock = clock ?? (() => DateTimeOffset.UtcNow);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = path, ForeignKeys = true, DefaultTimeout = 10 }.ToString();
        using var db = Open();
        Execute(db, null, """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS accounts(
              id TEXT PRIMARY KEY, token_hash TEXT UNIQUE NOT NULL, state TEXT NOT NULL, revision INTEGER NOT NULL,
              run_snapshot TEXT, run_seed INTEGER NOT NULL DEFAULT 0, run_started TEXT, created TEXT NOT NULL, run_rules INTEGER NOT NULL DEFAULT 2);
            CREATE TABLE IF NOT EXISTS requests(
              owner TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE, request_id TEXT NOT NULL, hash TEXT NOT NULL,
              message TEXT NOT NULL, PRIMARY KEY(owner,request_id));
            CREATE TABLE IF NOT EXISTS ledger(
              owner TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE, request_id TEXT NOT NULL,
              currency TEXT NOT NULL, delta INTEGER NOT NULL, balance INTEGER NOT NULL, type TEXT NOT NULL, created TEXT NOT NULL,
              PRIMARY KEY(owner,request_id,currency));
            CREATE TABLE IF NOT EXISTS likes(
              viewer TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
              target TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE, PRIMARY KEY(viewer,target));
            CREATE TABLE IF NOT EXISTS blocks(
              viewer TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
              target TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE, PRIMARY KEY(viewer,target));
            CREATE TABLE IF NOT EXISTS reports(
              viewer TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
              target TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE, reason TEXT NOT NULL, created TEXT NOT NULL,
              PRIMARY KEY(viewer,target));
            """);
        using var columns = Sql(db,null,"PRAGMA table_info(accounts)");
        bool hasRules = false;
        using (var reader = columns.ExecuteReader()) while (reader.Read()) if (reader.GetString(1) == "run_rules") hasRules = true;
        if (!hasRules) Execute(db,null,"ALTER TABLE accounts ADD COLUMN run_rules INTEGER NOT NULL DEFAULT 2");
        InitializeEndgame(db);
        InitializeCommerce(db);
    }
    SqliteConnection Open() { var db = new SqliteConnection(connectionString); db.Open(); return db; }
    static SqliteCommand Sql(SqliteConnection db, SqliteTransaction tx, string text, params (string, object)[] args)
    {
        var command = db.CreateCommand(); command.Transaction = tx; command.CommandText = text;
        foreach (var (name, value) in args) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }
    static int Execute(SqliteConnection db, SqliteTransaction tx, string text, params (string, object)[] args)
    { using var c = Sql(db, tx, text, args); return c.ExecuteNonQuery(); }
    static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    static string Encode<T>(T value) => JsonSerializer.Serialize(value, Json);
    static T Decode<T>(string value) => JsonSerializer.Deserialize<T>(value, Json);
    sealed class Account
    {
        public string Id, RunSnapshot, RunStarted, RunSupport;
        public GameState State;
        public long Revision;
        public int RunSeed, RunRules;
    }
    static string TokenHash(string token)
    {
        if (token == null || !Regex.IsMatch(token, "^[A-Za-z0-9_-]{43}$")) throw new ApiFault(401, "계정 복구 코드가 올바르지 않아요.");
        return Hash(token);
    }
    Account Load(SqliteConnection db, SqliteTransaction tx, string token, bool byId = false)
    {
        using var c = Sql(db, tx, "SELECT id,state,revision,run_snapshot,run_seed,run_started,run_rules,run_support FROM accounts WHERE " + (byId ? "id=$key" : "token_hash=$key"), ("$key", byId ? token : TokenHash(token)));
        using var r = c.ExecuteReader();
        if (!r.Read()) throw new ApiFault(byId ? 404 : 401, byId ? "프로필을 찾을 수 없어요." : "계정을 찾을 수 없어요.");
        var a = new Account { Id = r.GetString(0), State = Decode<GameState>(r.GetString(1)), Revision = r.GetInt64(2), RunSnapshot = r.IsDBNull(3) ? null : r.GetString(3), RunSeed = r.GetInt32(4), RunStarted = r.IsDBNull(5) ? null : r.GetString(5), RunRules = r.GetInt32(6), RunSupport=r.IsDBNull(7)?null:r.GetString(7) };
        Social.Normalize(a.State); return a;
    }
    ServerReply Reply(Account a, string message = "동기화 완료", bool success = true)
    {
        new Economy(a.State, clock).RefreshDay();
        return new ServerReply { success = success, message = message, playerId = a.Id, state = a.State, revision = a.Revision, serverTime = clock().ToString("O"),
            ticket = string.IsNullOrEmpty(a.State.activeRun) ? null : new BattleTicket { id = a.State.activeRun, stage = a.State.activeStage, seed = a.RunSeed, rulesVersion = a.RunRules, mode=a.State.activeMode, week=a.State.activeWeek, hasSupport=a.RunSupport!=null, support=a.RunSupport==null?null:Decode<SupportSnapshot>(a.RunSupport) } };
    }
    public ServerReply Register(string secret)
    {
        string hash = TokenHash(secret);
        lock (gate)
        {
            using var db = Open(); using var tx = db.BeginTransaction();
            var state = GameState.New(); new Economy(state, clock); Social.Normalize(state);
            Execute(db, tx, "INSERT OR IGNORE INTO accounts(id,token_hash,state,revision,created) VALUES($id,$hash,$state,0,$time)",
                ("$id", Guid.NewGuid().ToString("N").Substring(0,12).ToUpperInvariant()), ("$hash", hash), ("$state", Encode(state)), ("$time", clock().ToString("O")));
            var a = Load(db, tx, secret); tx.Commit(); return Reply(a);
        }
    }
    public ServerReply Bootstrap(string token) { using var db = Open(); return Reply(Load(db, null, token)); }
    public ServerReply Apply(string token, GameCommand command)
    {
        if (command == null || !Regex.IsMatch(command.requestId ?? "", "^[a-f0-9]{32}$")) throw new ApiFault(400, "요청 ID가 올바르지 않아요.");
        string hash = Hash(Encode(command));
        lock (gate)
        {
            using var db = Open(); using var tx = db.BeginTransaction(); var a = Load(db, tx, token);
            using (var previous = Sql(db, tx, "SELECT hash,message FROM requests WHERE owner=$owner AND request_id=$id", ("$owner", a.Id), ("$id", command.requestId)))
            using (var r = previous.ExecuteReader())
            {
                if (r.Read())
                {
                    if (r.GetString(0) != hash) throw new ApiFault(409, "같은 요청 ID에 다른 내용이 들어왔어요.", Reply(a, "요청 내용 충돌", false));
                    return Reply(a, r.GetString(1));
                }
            }
            if (a.Revision != command.revision) throw new ApiFault(409, "다른 기기에서 진행이 변경됐어요. 최신 상태에서 다시 시도해 주세요.", Reply(a, "진행 상태가 변경됐어요. 다시 시도해 주세요.", false));
            int paidBefore=a.State.paidGems;
            int gold = a.State.gold, gems = a.State.gems, parts = a.State.parts, shards=a.State.coreShards;
            var blueprintBalances=Enumerable.Range(0,Campaign.Regions.Length).Select(i=>Workshop.Region(a.State,i).blueprints).ToArray();
            var e = new Economy(a.State, clock); CommandResult result;
            if (command.type == "battle_start")
            {
                if (command.rulesVersion != Combat.RulesVersion) throw new ApiFault(409,"이전 버전에서 보낸 출격 요청이에요. 최신 상태에서 다시 출격해 주세요.");
                a.RunRules = Combat.RulesVersion;
                if (command.quick) throw new ApiFault(400, "서버 작전에는 빠른 테스트를 사용할 수 없어요.");
                string id = Guid.NewGuid().ToString("N"); a.RunSeed = RandomNumberGenerator.GetInt32(1, int.MaxValue);
                if(Endgame.IsChallenge(command.mode))
                {
                    if(!string.IsNullOrEmpty(command.supportId))throw new ApiFault(400,"주간 도전은 본인 대원으로 진행해요.");
                    result=e.BeginChallenge(id,command.mode);
                    if(!result.Success)throw new ApiFault(400,result.Message);
                    a.RunSeed=Endgame.Seed(a.State.activeWeek);a.RunSupport=null;
                }
                else
                {
                    if(!string.IsNullOrEmpty(command.mode))throw new ApiFault(400,"없는 전투 모드예요.");
                    result=e.BeginRun(id,command.stage);
                    var support=ReserveSupport(db,tx,a,command.supportId);a.RunSupport=support==null?null:Encode(support);
                }
                a.State.activeGuild=GuildId(db,tx,a.Id)??"";
                a.RunSnapshot = Encode(a.State); a.RunStarted = clock().ToString("O");
            }
            else if (command.type == "battle_finish")
            {
                if (a.State.activeRun != command.id || a.RunSnapshot == null || a.RunStarted == null) throw new ApiFault(400, "진행 중인 서버 작전을 찾을 수 없어요.");
                if (a.RunRules != Combat.RulesVersion || command.rulesVersion != Combat.RulesVersion) throw new ApiFault(409,"이전 버전의 작전이에요. 계정 화면에서 중단 작전을 종료해 주세요.");
                Combat simulation;
                try { simulation = ReplayRecorder.Verify(Decode<GameState>(a.RunSnapshot), a.State.activeStage, command.id, a.RunSeed, command.replay, (clock() - DateTimeOffset.Parse(a.RunStarted)).TotalSeconds, a.State.activeMode, a.RunSupport==null?null:Decode<SupportSnapshot>(a.RunSupport)); }
                catch (ArgumentException ex) { throw new ApiFault(400, ex.Message); }
                RecordWeekly(db,tx,a,simulation);
                RecordSupport(db,tx,a,simulation);
                RecordRestoration(db,tx,a,simulation);
                result = simulation.Challenge ? e.FinishChallenge(simulation) : e.FinishCampaign(simulation);
                a.RunSnapshot = a.RunStarted = a.RunSupport = null;
            }
            else
            {
                // No client supplied wallet, score, entitlement or arbitrary reward is accepted.
                result = command.type!=null && command.type.StartsWith("guild_",StringComparison.Ordinal) ? GuildCommand(db,tx,a,command) : CommandDispatcher.Apply(e, command, false);
                if (command.type == "battle_cancel") a.RunSnapshot = a.RunStarted = a.RunSupport = null;
            }
            if (!result.Success) throw new ApiFault(400, result.Message);
            AllocatePaidSpend(db,tx,a.Id,paidBefore-a.State.paidGems);
            Social.Normalize(a.State); a.Revision++;
            Execute(db, tx, "UPDATE accounts SET state=$state,revision=$revision,run_snapshot=$snapshot,run_seed=$seed,run_started=$started,run_rules=$rules,run_support=$support WHERE id=$id",
                ("$state", Encode(a.State)), ("$revision", a.Revision), ("$snapshot", a.RunSnapshot), ("$seed", a.RunSeed), ("$started", a.RunStarted), ("$rules",a.RunRules), ("$support",a.RunSupport), ("$id", a.Id));
            Execute(db, tx, "INSERT INTO requests(owner,request_id,hash,message) VALUES($owner,$id,$hash,$message)", ("$owner",a.Id), ("$id",command.requestId), ("$hash",hash), ("$message",result.Message));
            foreach (var value in new[] { ("gold", a.State.gold-gold, a.State.gold), ("gems", a.State.gems-gems, a.State.gems), ("parts", a.State.parts-parts, a.State.parts), ("core_shards",a.State.coreShards-shards,a.State.coreShards) })
                if (value.Item2 != 0) Execute(db, tx, "INSERT INTO ledger VALUES($owner,$id,$currency,$delta,$balance,$type,$time)",
                    ("$owner",a.Id), ("$id",command.requestId), ("$currency",value.Item1), ("$delta",value.Item2), ("$balance",value.Item3), ("$type",command.type), ("$time",clock().ToString("O")));
            for(int region=0;region<Campaign.Regions.Length;region++)
            {
                int balance=Workshop.Region(a.State,region).blueprints,delta=balance-blueprintBalances[region];
                if(delta!=0)Execute(db,tx,"INSERT INTO ledger VALUES($owner,$id,$currency,$delta,$balance,$type,$time)",
                    ("$owner",a.Id),("$id",command.requestId),("$currency","blueprints:"+region),("$delta",delta),("$balance",balance),("$type",command.type),("$time",clock().ToString("O")));
            }
            tx.Commit(); return Reply(a, result.Message);
        }
    }
    static bool Blocked(SqliteConnection db, SqliteTransaction tx, string viewer, string target)
    {
        using var c = Sql(db, tx, "SELECT COUNT(*) FROM blocks WHERE (viewer=$viewer AND target=$target) OR (viewer=$target AND target=$viewer)", ("$viewer",viewer), ("$target",target));
        return (long)c.ExecuteScalar() > 0;
    }
    PublicProfile Public(SqliteConnection db, SqliteTransaction tx, Account viewer, Account target)
    {
        if (viewer.Id != target.Id && (!target.State.publicProfile || Blocked(db, tx, viewer.Id, target.Id))) throw new ApiFault(404, "공개된 프로필을 찾을 수 없어요.");
        var profile = Social.Build(target.State, target.Id, false);
        using var likes = Sql(db, tx, "SELECT COUNT(*),COALESCE(SUM(CASE WHEN viewer=$viewer THEN 1 ELSE 0 END),0) FROM likes WHERE target=$target", ("$viewer",viewer.Id), ("$target",target.Id));
        using var r = likes.ExecuteReader(); r.Read(); profile.likes = r.GetInt32(0); profile.liked = r.GetInt64(1) > 0; return profile;
    }
    public PublicProfile GetProfile(string token, string id)
    { using var db = Open(); var viewer = Load(db, null, token); return Public(db, null, viewer, Load(db, null, id.ToUpperInvariant(), true)); }
    public ProfileList Gallery(string token, string after)
    {
        using var db = Open(); var viewer = Load(db, null, token); var list = new ProfileList();
        using var c = Sql(db, null, "SELECT id FROM accounts WHERE id>$after AND id<>$viewer ORDER BY id LIMIT 100", ("$after",after ?? ""), ("$viewer",viewer.Id));
        var ids = new List<string>(); using (var r = c.ExecuteReader()) while (r.Read()) ids.Add(r.GetString(0));
        foreach (string id in ids)
        {
            list.next = id;
            try { list.profiles.Add(Public(db, null, viewer, Load(db, null, id, true))); } catch (ApiFault ex) when (ex.Status == 404) { }
            if (list.profiles.Count == 20) return FeatureGallery(list);
        }
        if (ids.Count < 100) list.next = ""; return FeatureGallery(list);
    }
    ProfileList FeatureGallery(ProfileList list)
    {
        list.featuredId=list.profiles.Where(p=>!p.test).OrderBy(p=>Hash(p.id+Endgame.Week(clock())),StringComparer.Ordinal).Select(p=>p.id).FirstOrDefault()??"";
        return list;
    }
    public PublicProfile Like(string token, string id, bool enabled)
    {
        lock (gate)
        {
            using var db = Open(); using var tx = db.BeginTransaction(); var viewer = Load(db, tx, token); var target = Load(db, tx, id, true);
            if (viewer.Id == target.Id) throw new ApiFault(400, "내 프로필에는 응원할 수 없어요.");
            Public(db, tx, viewer, target);
            Execute(db, tx, enabled ? "INSERT OR IGNORE INTO likes(viewer,target) VALUES($viewer,$target)" : "DELETE FROM likes WHERE viewer=$viewer AND target=$target", ("$viewer",viewer.Id), ("$target",target.Id));
            var result = Public(db, tx, viewer, target); tx.Commit(); return result;
        }
    }
    public BlockList Blocks(string token)
    {
        using var db = Open(); var viewer = Load(db,null,token);
        using var c = Sql(db,null,"SELECT target FROM blocks WHERE viewer=$viewer ORDER BY target",("$viewer",viewer.Id));
        using var r = c.ExecuteReader(); var result = new BlockList(); while (r.Read()) result.ids.Add(r.GetString(0)); return result;
    }
    public void Block(string token, string id, bool enabled)
    {
        lock (gate)
        {
            using var db = Open(); using var tx = db.BeginTransaction(); var viewer = Load(db, tx, token); var target = Load(db, tx, id, true);
            if (viewer.Id == target.Id) throw new ApiFault(400, "내 계정은 차단할 수 없어요.");
            Execute(db, tx, enabled ? "INSERT OR IGNORE INTO blocks(viewer,target) VALUES($viewer,$target)" : "DELETE FROM blocks WHERE viewer=$viewer AND target=$target", ("$viewer",viewer.Id), ("$target",target.Id));
            if (enabled) Execute(db, tx, "DELETE FROM likes WHERE (viewer=$viewer AND target=$target) OR (viewer=$target AND target=$viewer)", ("$viewer",viewer.Id), ("$target",target.Id));
            tx.Commit();
        }
    }
    public void Report(string token, string id, string reason)
    {
        if (!new[] { "name", "cheating", "other" }.Contains(reason)) throw new ApiFault(400, "신고 사유를 선택해 주세요.");
        lock (gate)
        {
            using var db = Open(); using var tx = db.BeginTransaction(); var viewer = Load(db, tx, token); var target = Load(db, tx, id, true);
            if (viewer.Id == target.Id) throw new ApiFault(400, "내 계정은 신고할 수 없어요.");
            Public(db, tx, viewer, target);
            Execute(db, tx, "INSERT OR IGNORE INTO reports VALUES($viewer,$target,$reason,$time)", ("$viewer",viewer.Id), ("$target",target.Id), ("$reason",reason), ("$time",clock().ToString("O")));
            tx.Commit();
        }
    }
    public void Delete(string token)
    {
        lock (gate) { using var db = Open(); using var tx = db.BeginTransaction(); var a = Load(db, tx, token); LeaveGuild(db,tx,a.Id); Execute(db, tx, "DELETE FROM accounts WHERE id=$id", ("$id",a.Id)); tx.Commit(); }
    }
}
