namespace SprintFlow.Web.Server.Common;

/// <summary>
/// One sprint's tasks split three ways, each share also expressed as a whole percentage of the
/// sprint.
/// </summary>
/// <remarks>
/// The rounding rule lives here rather than in the service so there is one place it can be read.
/// Completed and Cancelled are rounded normally and Ongoing takes whatever is left over, so the
/// three always total 100 and the two figures the report is read for are the exact ones. A sprint
/// with no tasks is all zeros rather than a division by zero.
/// </remarks>
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
            // The remainder rather than its own rounding: three shares rounded independently can
            // total 99 or 101, and the two above are the ones being reported on.
            100 - completedPercent - cancelledPercent);
    }

    // Away from zero, so a single task in a sprint of two hundred reads as 1% rather than 0%.
    private static int Percent(int part, int total)
        => (int)Math.Round(part * 100.0 / total, MidpointRounding.AwayFromZero);
}
