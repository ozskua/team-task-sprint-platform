namespace SprintFlow.Web.Server.Options;

/// <summary>
/// Application-wide settings bound from the <c>Application</c> configuration section.
/// </summary>
public class ApplicationOptions
{
    public const string SectionName = "Application";

    /// <summary>
    /// The timezone the company works in, as an IANA id — for example <c>Europe/Istanbul</c>.
    /// </summary>
    /// <remarks>
    /// What "today" means. Sprint dates are whole calendar days, so deciding which day it is has to
    /// come from somewhere: reading the machine's own clock would make the answer depend on where
    /// the server happens to be running, and using UTC would end a sprint hours early or late for
    /// anyone not on it.
    /// <para>
    /// One value because the application represents one company (domain rule 1). This is not
    /// per-user timezone support, and it does not give sprints a time of day.
    /// </para>
    /// </remarks>
    public string TimeZone { get; set; } = string.Empty;
}
