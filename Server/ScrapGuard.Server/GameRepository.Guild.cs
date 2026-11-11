using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
namespace ScrapGuard.Server;

public sealed partial class GameRepository
{
    void InitializeEndgame(SqliteConnection db)
    {
        Execute(db,null,"""
            CREATE TABLE IF NOT EXISTS weekly_records(
                week TEXT NOT NULL, owner TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
                score INTEGER NOT NULL, achieved TEXT NOT NULL, hero INTEGER NOT NULL, skin INTEGER NOT NULL, rules INTEGER NOT NULL,
                PRIMARY KEY(week,owner));
            CREATE INDEX IF NOT EXISTS weekly_score ON weekly_records(week,rules,score DESC,achieved,owner);
            CREATE TABLE IF NOT EXISTS skill_records(
                week TEXT NOT NULL, owner TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
                score INTEGER NOT NULL, achieved TEXT NOT NULL, hero INTEGER NOT NULL, skin INTEGER NOT NULL, rules INTEGER NOT NULL, outfit INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY(week,owner));
            CREATE INDEX IF NOT EXISTS skill_score ON skill_records(week,rules,score DESC,achieved,owner);
            CREATE TABLE IF NOT EXISTS guilds(
                id TEXT PRIMARY KEY,name TEXT NOT NULL,code TEXT UNIQUE NOT NULL,leader TEXT REFERENCES accounts(id) ON DELETE SET NULL);
            CREATE TABLE IF NOT EXISTS guild_members(
                owner TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
                guild TEXT NOT NULL REFERENCES guilds(id) ON DELETE CASCADE,hero INTEGER NOT NULL DEFAULT -1);
            CREATE INDEX IF NOT EXISTS guild_roster ON guild_members(guild,owner);
            CREATE TABLE IF NOT EXISTS restoration_runs(
                run TEXT PRIMARY KEY, week TEXT NOT NULL, day TEXT NOT NULL,
                guild TEXT NOT NULL REFERENCES guilds(id) ON DELETE CASCADE,
                owner TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE);
            CREATE INDEX IF NOT EXISTS restoration_week ON restoration_runs(guild,week);
            CREATE INDEX IF NOT EXISTS restoration_daily ON restoration_runs(owner,day);
            CREATE TABLE IF NOT EXISTS guild_memorials(
                guild TEXT NOT NULL REFERENCES guilds(id) ON DELETE CASCADE,
                week TEXT NOT NULL, tier INTEGER NOT NULL, points INTEGER NOT NULL,
                PRIMARY KEY(guild,week));
            CREATE TABLE IF NOT EXISTS support_runs(
                run TEXT PRIMARY KEY, week TEXT NOT NULL, guild TEXT NOT NULL REFERENCES guilds(id) ON DELETE CASCADE,
                owner TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
                borrower TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE, won INTEGER NOT NULL);
            CREATE INDEX IF NOT EXISTS support_week ON support_runs(guild,week,owner);
            """);
        // Backfill completed 0.12 weeks without currency or personal identities. Preserve earned records.
        Execute(db,null,"""
            INSERT INTO guild_memorials(guild,week,tier,points)
            SELECT guild,week,CASE WHEN COUNT(*)>=120 THEN 3 WHEN COUNT(*)>=60 THEN 2 ELSE 1 END,COUNT(*)
            FROM restoration_runs GROUP BY guild,week HAVING COUNT(*)>=30
            ON CONFLICT(guild,week) DO UPDATE SET tier=MAX(tier,excluded.tier),points=MAX(points,excluded.points);
            """);
        using var columns=Sql(db,null,"PRAGMA table_info(accounts)");bool present=false;
        using(var r=columns.ExecuteReader())while(r.Read())if(r.GetString(1)=="run_support")present=true;
        if(!present)Execute(db,null,"ALTER TABLE accounts ADD COLUMN run_support TEXT");
        bool hasOutfit=false;
        using(var query=Sql(db,null,"PRAGMA table_info(weekly_records)"))
        using(var reader=query.ExecuteReader())while(reader.Read())if(reader.GetString(1)=="outfit")hasOutfit=true;
        if(!hasOutfit)Execute(db,null,"ALTER TABLE weekly_records ADD COLUMN outfit INTEGER NOT NULL DEFAULT 0");
    }
    string GuildId(SqliteConnection db,SqliteTransaction tx,string owner)
    {
        using var query=Sql(db,tx,"SELECT guild FROM guild_members WHERE owner=$owner",("$owner",owner));
        return query.ExecuteScalar() as string;
    }
    int SupportPower(GameState state,int hero)
    {
        var weapon=state.items.Find(i=>i.id==state.equipped[0]);
        return Math.Min(350,Catalog.AttackFor(hero,state.heroes[hero].level,weapon));
    }
    public GuildView Guild(string token)
    {
        using var db=Open();using var tx=db.BeginTransaction();var viewer=Load(db,tx,token);new Economy(viewer.State,clock);
        var result=new GuildView {week=Endgame.Week(clock()),remaining=Math.Max(0,3-viewer.State.dailySupportStarts)};
        string guild=GuildId(db,tx,viewer.Id);if(guild==null)return result;
        using(var query=Sql(db,tx,"SELECT name,code,leader FROM guilds WHERE id=$id",("$id",guild)))
        using(var r=query.ExecuteReader())
        {
            if(!r.Read())return result;
            result.id=guild;result.name=r.GetString(0);result.code=r.GetString(1);result.leader=!r.IsDBNull(2)&&r.GetString(2)==viewer.Id;
        }
        var members=new List<(string id,int hero)>();
        using(var query=Sql(db,tx,"SELECT owner,hero FROM guild_members WHERE guild=$guild ORDER BY owner",("$guild",guild)))
        using(var r=query.ExecuteReader())while(r.Read())members.Add((r.GetString(0),r.GetInt32(1)));
        foreach(var member in members)
        {
            if(Blocked(db,tx,viewer.Id,member.id))continue;
            var target=Load(db,tx,member.id,true);int hero=member.hero;
            result.members.Add(new GuildMember {id=target.Id,name=target.State.displayName,hero=hero,self=target.Id==viewer.Id,
                level=hero<0?0:target.State.heroes[hero].level,skin=target.State.selectedSkin,outfit=Outfits.Active(target.State,hero),power=hero<0?0:SupportPower(target.State,hero)});
        }
        foreach(var member in result.members)
        {
            using var stats=Sql(db,tx,"SELECT borrower,won FROM support_runs WHERE guild=$guild AND week=$week AND owner=$owner",("$guild",guild),("$week",result.week),("$owner",member.id));
            var helped=new HashSet<string>();using var rows=stats.ExecuteReader();
            while(rows.Read())
            {
                string borrower=rows.GetString(0);
                if(!result.members.Exists(m=>m.id==borrower))continue;
                member.supportUses++;if(rows.GetInt32(1)>0){member.supportWins++;helped.Add(borrower);}
            }
            member.helpedMembers=helped.Count;
        }
        result.featuredId=result.members.Where(m=>m.hero>=0 && m.helpedMembers>0).OrderByDescending(m=>m.helpedMembers).ThenByDescending(m=>m.supportWins).ThenBy(m=>m.id,StringComparer.Ordinal).Select(m=>m.id).FirstOrDefault();
        using(var q=Sql(db,tx,"SELECT COUNT(*) FROM restoration_runs WHERE guild=$guild AND week=$week",("$guild",guild),("$week",result.week)))result.restorationPoints=Convert.ToInt32(q.ExecuteScalar());
        using(var q=Sql(db,tx,"SELECT COUNT(*) FROM restoration_runs WHERE owner=$owner AND day=$day",("$owner",viewer.Id),("$day",Catalog.ServiceDate(clock()))))result.restorationToday=Convert.ToInt32(q.ExecuteScalar());
        foreach(var member in result.members)
        {
            using var q=Sql(db,tx,"SELECT COUNT(*) FROM restoration_runs WHERE guild=$guild AND week=$week AND owner=$owner",("$guild",guild),("$week",result.week),("$owner",member.id));
            member.restorationPoints=Convert.ToInt32(q.ExecuteScalar());
        }
        using(var q=Sql(db,tx,"SELECT COUNT(*) FROM guild_memorials WHERE guild=$guild",("$guild",guild)))result.memorialWeeks=Convert.ToInt32(q.ExecuteScalar());
        using(var q=Sql(db,tx,"SELECT week,tier,points FROM guild_memorials WHERE guild=$guild ORDER BY week DESC LIMIT 52",("$guild",guild)))
        using(var r=q.ExecuteReader())while(r.Read())result.memorials.Add(new GuildMemorial{week=r.GetString(0),tier=r.GetInt32(1),points=r.GetInt32(2)});
        return result;
    }
    void RecordRestoration(SqliteConnection db,SqliteTransaction tx,Account account,Combat combat)
    {
        if(combat.Challenge || combat.Practice || combat.ApexBattle || !combat.Won || !combat.TaskCompleted || combat.Elapsed<30 || account.State.testProgress)return;
        string guild=GuildId(db,tx,account.Id);
        if(string.IsNullOrEmpty(guild) || guild!=combat.GuildAtStart)return;
        string day=Catalog.ServiceDate(clock());
        using var count=Sql(db,tx,"SELECT COUNT(*) FROM restoration_runs WHERE owner=$owner AND day=$day",("$owner",account.Id),("$day",day));
        if(Convert.ToInt32(count.ExecuteScalar())>=GuildRestoration.DailyLimit)return;
        Execute(db,tx,"INSERT OR IGNORE INTO restoration_runs(run,week,day,guild,owner) VALUES($run,$week,$day,$guild,$owner)",("$run",combat.RunId),("$week",Endgame.Week(clock())),("$day",day),("$guild",guild),("$owner",account.Id));
        using var total=Sql(db,tx,"SELECT COUNT(*) FROM restoration_runs WHERE guild=$guild AND week=$week",("$guild",guild),("$week",Endgame.Week(clock())));
        int points=Convert.ToInt32(total.ExecuteScalar()),tier=GuildRestoration.Honor(points);
        if(tier>0)Execute(db,tx,"INSERT INTO guild_memorials(guild,week,tier,points) VALUES($guild,$week,$tier,$points) ON CONFLICT(guild,week) DO UPDATE SET tier=MAX(tier,excluded.tier),points=MAX(points,excluded.points)",("$guild",guild),("$week",Endgame.Week(clock())),("$tier",tier),("$points",points));
    }
    void RecordSupport(SqliteConnection db,SqliteTransaction tx,Account account,Combat combat)
    {
        var support=combat.Support;
        if(combat.Challenge || combat.Practice || combat.ApexBattle || !combat.SupportUsed || combat.Elapsed<30 || support==null || string.IsNullOrEmpty(support.guild))return;
        if(GuildId(db,tx,account.Id)!=support.guild || GuildId(db,tx,support.owner)!=support.guild || Blocked(db,tx,account.Id,support.owner))return;
        // Only verified calls count. Abandoned runs may count as use, never as a helped clear.
        Execute(db,tx,"INSERT OR IGNORE INTO support_runs(run,week,guild,owner,borrower,won) VALUES($run,$week,$guild,$owner,$borrower,$won)",
            ("$run",combat.RunId),("$week",Endgame.Week(clock())),("$guild",support.guild),("$owner",support.owner),("$borrower",account.Id),("$won",combat.Won?1:0));
    }
    CommandResult GuildCommand(SqliteConnection db,SqliteTransaction tx,Account a,GameCommand command)
    {
        string guild=GuildId(db,tx,a.Id);
        if(command.type=="guild_create")
        {
            if(guild!=null)return CommandResult.Fail("이미 길드에 가입되어 있어요.");
            string name=Regex.Replace((command.value??"").Normalize().Trim()," +"," ");
            if(!Regex.IsMatch(name,@"^[가-힣a-zA-Z0-9 _-]{2,12}$"))return CommandResult.Fail("길드 이름은 한글·영문·숫자 2~12자로 입력하세요.");
            string id=Guid.NewGuid().ToString("N"),code=Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
            Execute(db,tx,"INSERT INTO guilds VALUES($id,$name,$code,$leader)",("$id",id),("$name",name),("$code",code),("$leader",a.Id));
            Execute(db,tx,"INSERT INTO guild_members(owner,guild) VALUES($owner,$guild)",("$owner",a.Id),("$guild",id));
            return CommandResult.Ok("길드 창설 완료 · 초대 코드를 동료에게 알려 주세요.");
        }
        if(command.type=="guild_join")
        {
            if(guild!=null)return CommandResult.Fail("기존 길드에서 탈퇴한 뒤 가입하세요.");
            using var query=Sql(db,tx,"SELECT id,leader FROM guilds WHERE code=$code",("$code",(command.value??"").Trim().ToUpperInvariant()));
            string leader;
            using(var r=query.ExecuteReader()){if(!r.Read())return CommandResult.Fail("초대 코드를 찾을 수 없어요.");guild=r.GetString(0);leader=r.GetString(1);}
            if(Blocked(db,tx,a.Id,leader))return CommandResult.Fail("가입할 수 없는 길드예요.");
            using var count=Sql(db,tx,"SELECT COUNT(*) FROM guild_members WHERE guild=$guild",("$guild",guild));
            if((long)count.ExecuteScalar()>=20)return CommandResult.Fail("길드 정원 20명이 가득 찼어요.");
            Execute(db,tx,"INSERT INTO guild_members(owner,guild) VALUES($owner,$guild)",("$owner",a.Id),("$guild",guild));
            return CommandResult.Ok("길드에 합류했어요. 지원 대원을 등록해 보세요.");
        }
        if(guild==null)return CommandResult.Fail("먼저 길드에 가입하세요.");
        if(command.type=="guild_support")
        {
            int hero=command.enabled?command.hero:-1;
            if(hero>=0 && (hero>=a.State.heroes.Count || !a.State.heroes[hero].unlocked))return CommandResult.Fail("보유한 대원만 등록할 수 있어요.");
            if(command.enabled && hero<0)return CommandResult.Fail("대원을 선택하세요.");
            Execute(db,tx,"UPDATE guild_members SET hero=$hero WHERE owner=$owner",("$hero",hero),("$owner",a.Id));
            return CommandResult.Ok(hero<0 ? "지원 등록을 해제했어요." : "지원 대원 등록 완료 · 소유권은 그대로 유지돼요.");
        }
        if(command.type=="guild_leave") { LeaveGuild(db,tx,a.Id);return CommandResult.Ok("길드에서 탈퇴했어요."); }
        return CommandResult.Fail("없는 길드 요청이에요.");
    }
    void LeaveGuild(SqliteConnection db,SqliteTransaction tx,string owner)
    {
        string guild=GuildId(db,tx,owner);if(guild==null)return;
        Execute(db,tx,"DELETE FROM guild_members WHERE owner=$owner",("$owner",owner));
        using var next=Sql(db,tx,"SELECT owner FROM guild_members WHERE guild=$guild ORDER BY owner LIMIT 1",("$guild",guild));
        string successor=next.ExecuteScalar() as string;
        if(successor==null)Execute(db,tx,"DELETE FROM guilds WHERE id=$guild",("$guild",guild));
        else Execute(db,tx,"UPDATE guilds SET leader=$next WHERE id=$guild AND leader=$owner",("$next",successor),("$guild",guild),("$owner",owner));
    }
    SupportSnapshot ReserveSupport(SqliteConnection db,SqliteTransaction tx,Account a,string id)
    {
        if(string.IsNullOrEmpty(id))return null;
        string guild=GuildId(db,tx,a.Id);
        if(guild==null || id==a.Id || GuildId(db,tx,id)!=guild || Blocked(db,tx,a.Id,id))throw new ApiFault(400,"같은 길드의 지원 대원을 선택하세요.");
        if(a.State.dailySupportStarts>=3)throw new ApiFault(400,"오늘 길드 지원 3회를 모두 사용했어요.");
        using var query=Sql(db,tx,"SELECT hero FROM guild_members WHERE owner=$owner",("$owner",id));int hero=Convert.ToInt32(query.ExecuteScalar());
        var target=Load(db,tx,id,true);
        if(hero<0 || hero>=HeroCatalog.Count || !target.State.heroes[hero].unlocked)throw new ApiFault(400,"지원 등록이 해제된 대원이에요.");
        a.State.dailySupportStarts++;
        bool show=target.State.publicProfile;
        return new SupportSnapshot {owner=id,guild=guild,name=show?target.State.displayName:"길드 지원 대원",hero=hero,skin=show?target.State.selectedSkin:0,outfit=show?Outfits.Active(target.State,hero):0,power=SupportPower(target.State,hero),
            showIdentity=show,title=show?target.State.showcaseTitle:0,frame=show?target.State.showcaseFrame:0,trophy=show?Endgame.Trophy(target.State):-1};
    }
}
