namespace SprintFlow.Web.Server.Common;

public sealed record SprintTaskOutcome(
    int Total,
    int CompletedCount,
    int CompletedPercent,
    int CancelledCount,
    int CancelledPercent,
    int OngoingCount,
    int OngoingPercent)
{
    public static SprintTaskOutcome From(int completed, int cancelled, int ongoing)
    {
        var total = completed + cancelled + ongoing;

        if (total == 0)
        {
            return new SprintTaskOutcome(0, 0, 0, 0, 0, 0, 0);
        }

        var completedPercent = Percent(completed, total);
        var cancelledPercent = Percent(cancelled, total);

        return new SprintTaskOutcome(
            total,
            completed,
            completedPercent,
            cancelled,
            cancelledPercent,
            ongoing,
            100 - completedPercent - cancelledPercent);
    }

    private static int Percent(int part, int total)
        => (int)Math.Round(part * 100.0 / total, MidpointRounding.AwayFromZero);
}
