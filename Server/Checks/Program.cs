using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;

string folder = Path.Combine(Path.GetTempPath(), "scrap-server-checks-" + Guid.NewGuid().ToString("N"));
string dbPath = Path.Combine(folder,"test.db");
var now = DateTimeOffset.UtcNow;
var repo = new GameRepository(dbPath, () => now);
int count = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL " + name); Console.WriteLine("PASS " + name); count++; }
void Fails(Action action, int status, string name)
{
    try { action(); throw new Exception("Expected rejection: " + name); }
    catch (ApiFault e) { Check(e.Status == status, name); }
}
string Secret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
ServerReply Apply(string key, GameCommand cmd) { cmd.revision = repo.Bootstrap(key).revision; return repo.Apply(key,cmd); }
try
{
    string a = Secret(), b = Secret(); var initial = repo.Register(a); var second = repo.Register(b);
    Check(initial.playerId != second.playerId && !initial.state.publicProfile, "isolated accounts, private by default");
    Check(repo.Register(a).playerId == initial.playerId, "registration retry preserves account");
    Fails(() => repo.Bootstrap(Secret()),401,"unknown bearer rejected");
    var attend = new GameCommand("attendance") { revision = 0 };
    var attended = repo.Apply(a,attend); var retry = repo.Apply(a,attend);
    Check(attended.state.gold == initial.state.gold + 1000 && retry.state.gold == attended.state.gold && retry.revision == 1, "idempotent attendance receipt");
    attend.value = "changed"; Fails(() => repo.Apply(a,attend),409,"request ID content collision rejected");
    Fails(() => repo.Apply(a,new GameCommand("hero_upgrade") { revision = 0 }),409,"stale revision rejected");
    Fails(() => Apply(a,new GameCommand("demo_gems")),400,"no test gems on server");
    Fails(() => Apply(a,new GameCommand("demo_reward") { value = "gold" }),400,"no mock ad claims on server");
    Fails(() => Apply(a,new GameCommand("buy")),400,"missing offer rejected without crash");
    Check(repo.Bootstrap(a).state.gems == 0,"rejected commands preserve balance");
    Fails(() => repo.GetProfile(b,initial.playerId),404,"private profile hidden");
    Apply(a,new GameCommand("rename") { value = "고철장인" });
    Apply(a,new GameCommand("visibility") { enabled = true });
    var gallery = repo.Gallery(b,""); Check(gallery.profiles.Count == 1 && gallery.profiles[0].name == "고철장인","opt-in gallery");
    string serialized = JsonSerializer.Serialize(gallery,GameRepository.Json);
    Check(!serialized.Contains(a) && !serialized.Contains("gems") && !serialized.Contains("equipped") && !serialized.Contains("secret"),"public DTO excludes wallet and credentials");
    Check(repo.Like(b,initial.playerId,true).likes == 1 && repo.Like(b,initial.playerId,true).likes == 1,"like set is idempotent");
    Fails(() => repo.Like(a,initial.playerId,true),400,"self like denied");
    repo.Report(b,initial.playerId,"name"); repo.Report(b,initial.playerId,"name");
    Fails(() => repo.Report(b,initial.playerId,"arbitrary"),400,"report reason allowlist");
    repo.Block(b,initial.playerId,true); Check(repo.Gallery(b,"").profiles.Count == 0,"blocked profile omitted");
    Fails(() => repo.GetProfile(b,initial.playerId),404,"block also suppresses direct lookup");
    repo.Block(b,initial.playerId,false); Check(repo.GetProfile(b,initial.playerId).likes == 0,"unblock restores view without old likes");
    Fails(() => Apply(a,new GameCommand("battle_start") { rulesVersion=0 }),409,"stale combat request rejected after client compatibility gate");
    var started = Apply(a,new GameCommand("battle_start") { stage = 0 });
    Check(started.ticket.rulesVersion==Combat.RulesVersion,"ticket identifies combat rules");
    Fails(() => Apply(a,new GameCommand("battle_finish",started.ticket.id) {rulesVersion=2,replay=new()}),409,"mismatched rules cannot settle");
    var recorder = new ReplayRecorder(); var simulation = new Combat(started.state,0,started.ticket.id,started.ticket.seed);
    int frameCount = 0;
    while (!simulation.Ended && frameCount < 2400)
    {
        if (simulation.Choosing) { int skill = simulation.Choices[0]; simulation.Choose(skill); recorder.Choose(skill); }
        if (simulation.CanUltimate) { simulation.UseUltimate(); recorder.Ultimate(); if(simulation.Ended)break; }
        var movement = new V2((float)Math.Sin(frameCount * .009), (float)Math.Cos(frameCount * .009));
        simulation.Tick(ReplayRecorder.Step,recorder.Move(movement)); frameCount++;
    }
    if (!simulation.Ended) simulation.Abandon();
    var finish = new GameCommand("battle_finish",started.ticket.id) { stage = 4, won = true, kills = 999999, seconds = 999999, replay = recorder.Steps, revision = started.revision };
    Fails(() => repo.Apply(a,finish),400,"fast-forwarded battle rejected by server time");
    now = now.AddMinutes(3);
    Check(simulation.UltimateUses>0,"server replay exercises ultimate");
    var finished = repo.Apply(a,finish); var receipt = finished.state.receipts.Last();
    Check(receipt.kills == simulation.Kills && receipt.won == simulation.Won && receipt.seconds == simulation.Elapsed && finished.state.clearedStages.Count == 0,"replayed results override forged score and stage");
    Check(repo.Apply(a,finish).state.gold == finished.state.gold,"battle reward retry cannot double pay");
    var next = Apply(a,new GameCommand("battle_start") { stage = 0 });
    Fails(() => Apply(a,new GameCommand("battle_finish",next.ticket.id) { replay = new() { new ReplayStep { skill = 0 } } }),400,"illegal skill replay rejected");
    Apply(a,new GameCommand("battle_cancel"));
    Check(string.IsNullOrEmpty(repo.Bootstrap(a).state.activeRun),"interrupted run can be cancelled");
    Fails(() => Apply(a,new GameCommand("battle_start") { quick = true }),400,"quick test blocked on server");
    for (int i = 0; i < 22; i++) { now = now.AddDays(1); Apply(a,new GameCommand("attendance")); }
    var before = repo.Bootstrap(a).state;
    var cosmetic = Apply(a,new GameCommand("cosmetic_buy") { value = "frame_solar" });
    Check(cosmetic.state.gems == before.gems - 250 && Catalog.Attack(before) == Catalog.Attack(cosmetic.state),"cosmetic purchase spends gems without power increase");
    Fails(() => Apply(a,new GameCommand("cosmetic_buy") { value = "frame_solar" }),400,"duplicate cosmetic purchase rejected");
    Apply(a,new GameCommand("customize") { hero = 2, title = 2, frame = 3 });
    Check(repo.GetProfile(b,initial.playerId).frame == 3,"earned and owned display selection persisted");
    var restart = new GameRepository(dbPath, () => now); Check(restart.Bootstrap(a).state.displayName == "고철장인","SQLite survives repository restart");
    using (var db = new SqliteConnection("Data Source=" + dbPath))
    {
        db.Open(); using var query = db.CreateCommand(); query.CommandText = "SELECT COUNT(*) FROM ledger WHERE type='attendance' AND currency='gold'";
        Check((long)query.ExecuteScalar() > 0,"currency ledger recorded");
        query.CommandText = "SELECT COUNT(*) FROM reports"; Check((long)query.ExecuteScalar() == 1,"reports deduplicate");
        query.CommandText = "SELECT sqlite_version()"; Console.WriteLine("SQLite " + query.ExecuteScalar());
    }
    var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string> { ["BETA_INVITE_CODE"]="server-tests-invite-code" }).Build();
    var operations=new ServerOperations(config);
    Fails(()=>operations.CheckRegistration(null),403,"invitation required");
    Fails(()=>operations.CheckRegistration("wrong"),403,"wrong invitation rejected");
    operations.CheckRegistration("server-tests-invite-code");Check(true,"valid invitation accepted");
    config["PUBLIC_REGISTRATION"]="true";
    new ServerOperations(config).CheckRegistration(null);Check(true,"public guest registration does not require the retained invite");
    config["REGISTRATION_ENABLED"]="false";
    Fails(()=>new ServerOperations(config).CheckRegistration(null),503,"public signup still respects registration shutdown");
    config["PUBLIC_REGISTRATION"]="false";
    Fails(()=>new ServerOperations(config).CheckRegistration("server-tests-invite-code"),503,"registration can close");
    config["MAINTENANCE_MODE"]="true";Check(new ServerOperations(config).Maintenance,"maintenance flag parsed");
    var proxy=new ForwardedHeadersOptions();ServerOperations.ConfigureProxy(proxy,"172.31.248.2");
    Check(proxy.KnownProxies.Count==1 && proxy.KnownProxies[0].ToString()=="172.31.248.2" && proxy.KnownIPNetworks.Count==0 && proxy.ForwardLimit==1,"proxy trust restricted to explicit address");
    string backup=Path.Combine(folder,"backup.db");DatabaseMaintenance.Backup(dbPath,backup);
    var recovered=new GameRepository(backup,()=>now);Check(recovered.Bootstrap(a).state.gold==repo.Bootstrap(a).state.gold,"online backup restores committed state including WAL");
    Check(DatabaseMaintenance.Ready(dbPath) && !DatabaseMaintenance.Ready(Path.Combine(folder,"missing.db")),"readiness checks existing database without creating missing file");
    bool collision=false;try { DatabaseMaintenance.Backup(dbPath,backup); } catch(IOException) {collision=true;}Check(collision,"backup cannot overwrite a previous snapshot");
    // Emulate the v0.2 schema with an active run, then start a new repository instance.
    var oldRun=Apply(a,new GameCommand("battle_start"));
    using(var oldDb=new SqliteConnection("Data Source="+dbPath))
    {
        oldDb.Open();using var alter=oldDb.CreateCommand();alter.CommandText="ALTER TABLE accounts DROP COLUMN run_rules";alter.ExecuteNonQuery();
    }
    repo=new GameRepository(dbPath,()=>now);
    Check(repo.Bootstrap(a).ticket.rulesVersion==2,"old database migrates active run to legacy version");
    Fails(()=>Apply(a,new GameCommand("battle_finish",oldRun.ticket.id){replay=new()}),409,"legacy active run cannot receive new-rule rewards");
    Apply(a,new GameCommand("battle_cancel"));Check(string.IsNullOrEmpty(repo.Bootstrap(a).state.activeRun),"legacy run remains cancellable");
    repo.Delete(a); Fails(() => repo.Bootstrap(a),401,"deleted bearer revoked");
    Check(repo.Gallery(b,"").profiles.Count == 0,"deleted public profile removed");
    using (var db = new SqliteConnection("Data Source=" + dbPath))
    {
        db.Open(); using var query = db.CreateCommand(); query.CommandText = "SELECT (SELECT COUNT(*) FROM reports)+(SELECT COUNT(*) FROM ledger)+(SELECT COUNT(*) FROM requests)";
        Check((long)query.ExecuteScalar() == 0,"account deletion cascades social and economy rows");
    }
    EndgameChecks.Run(folder,Check);
    CampaignChecks.Run(folder,Check);
    WorkshopChecks.Run(folder,Check);
    ExpansionChecks.Run(folder,Check);
    OutfitChecks.Run(folder,Check);
    ExperienceChecks.Run(folder,Check);
    CommerceChecks.Run(folder,Check);
    IdentityChecks.Run(folder,Check);
    RestorationChecks.Run(folder,Check);
    WardrobeChecks.Run(folder,Check);
    ReleaseChecks.Run(Check);
    ServicePreparationChecks.Run(folder,Check);
    ReviveChecks.Run(folder,Check);
    LiveOpsChecks.Run(folder,Check);
    RefundChecks.Run(folder,Check);
    Console.WriteLine(count + " checks passed");
}
finally { SqliteConnection.ClearAllPools(); if (Directory.Exists(folder)) Directory.Delete(folder,true); }
