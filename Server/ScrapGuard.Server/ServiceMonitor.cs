namespace ScrapGuard.Server;
public sealed class ServiceMonitor
{
    readonly object gate=new();
    readonly DateTimeOffset started=DateTimeOffset.UtcNow;
    DateTimeOffset? refundSuccess,refundFailure;
    int failures;
    public void RefundResult(bool success)
    {lock(gate){if(success){refundSuccess=DateTimeOffset.UtcNow;failures=0;}else{refundFailure=DateTimeOffset.UtcNow;failures++;}}}
    public object Snapshot(bool commerceEnabled)
    {
        lock(gate)return new {
            status=!commerceEnabled?"disabled":refundSuccess==null?"waiting":failures>0?"retrying":"ok",
            lastSuccess=refundSuccess,lastFailure=refundFailure,consecutiveFailures=failures,
            needsAttention=commerceEnabled&&(failures>0||DateTimeOffset.UtcNow-(refundSuccess??started)>TimeSpan.FromHours(2))
        };
    }
}
