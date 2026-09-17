namespace SprintFlow.Web.Server.Services;

public sealed record SprintReportWebhookResult(int Delivered, int Failed, int Skipped)
{
    public override string ToString() => $"delivered={Delivered};failed={Failed};skipped={Skipped}";
}
