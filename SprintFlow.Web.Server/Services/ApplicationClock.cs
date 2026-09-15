using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Services;

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
