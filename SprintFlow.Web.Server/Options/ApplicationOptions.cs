namespace SprintFlow.Web.Server.Options;

// The company's IANA timezone, e.g. Europe/Istanbul. Sprint dates are whole calendar days, so
// something has to decide which day it is - see IApplicationClock.
public class ApplicationOptions
{
    public const string SectionName = "Application";

    public string TimeZone { get; set; } = string.Empty;
}
