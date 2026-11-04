using System.Text.Json;
namespace ScrapGuard.Server;
// Local CLI only: use the same OS/container permissions that protect the database.
public static class OperationsCommand
{
    public static void Run(string path,IConfiguration config)
    {
        if(!File.Exists(path))throw new InvalidOperationException("Operations require an existing database.");
        var repo=new GameRepository(path);string command=config["ops"];
        object result=command switch {
            "summary"=>repo.OperationsSummary(),
            "refund-status"=>repo.RefundHealth(),
            "metrics"=>repo.MetricsDashboard(config.GetValue("include-tests",false)),
            "content"=>repo.Content(),
            "content-set"=>repo.ScheduleContent(new ContentChange(config["request-id"],config["operator"],config["reason"],config.GetValue("outfit",0),config["starts"],config["ends"],config.GetValue("paused",false),config.GetValue("min-build",ScrapGuard.Core.AppRelease.Build))),
            "reports" or "audit"=>repo.OperationsList(command),
            "account"=>repo.OperationsAccount(config["account"]??""),
            "hide" or "unhide" or "resolve" or "compensate"=>repo.Operate(new OperatorAction(config["request-id"],config["operator"],config["account"],command,config["reason"],config.GetValue("gold",0),config.GetValue("parts",0))),
            _=>throw new InvalidOperationException("ops: summary, reports, audit, account, hide, unhide, resolve, compensate, refund-status, metrics, content, content-set")
        };
        Console.WriteLine(JsonSerializer.Serialize(result,new JsonSerializerOptions(GameRepository.Json){WriteIndented=true}));
    }
}
