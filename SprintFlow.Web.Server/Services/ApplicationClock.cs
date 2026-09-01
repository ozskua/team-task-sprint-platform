using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Today's date in the company's timezone.
/// </summary>
/// <remarks>
/// The instant always comes from UTC; the timezone only decides which calendar day that instant
/// falls in. Reading the machine's local time instead would make a sprint's status depend on where
/// the server is deployed, and staying in UTC would move the day boundary for everyone using the
/// application — three hours early in Turkey, so a sprint that ended yesterday still reads as
/// running until 03:00.
/// <para>
/// The timezone is resolved once at construction rather than on every call: an unknown id is a
/// configuration mistake, and it should stop the application starting rather than throw on the
/// first request that happens to look at a sprint.
/// </para>
/// </remarks>
public class ApplicationClock : IApplicationClock
{
    private readonly TimeZoneInfo timeZone;

    public ApplicationClock(IOptions<ApplicationOptions> options)
    {
        var configured = options.Value.TimeZone;

        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                $"{ApplicationOptions.SectionName}:{nameof(ApplicationOptions.TimeZone)} is not configured. "
                + "Sprint dates are whole calendar days, so the application has to be told which timezone decides what 'today' is.");
        }

        try
        {
            // IANA ids resolve on Windows as well as Linux — .NET maps them through ICU — so one
            // configured value works wherever this runs.
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(configured);
        }
        catch (Exception exception) when (
            exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException(
                $"{ApplicationOptions.SectionName}:{nameof(ApplicationOptions.TimeZone)} is '{configured}', which is not a timezone this system knows. "
                + "Use an IANA id such as 'Europe/Istanbul'.",
                exception);
        }
    }

    public DateOnly Today()
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
}
