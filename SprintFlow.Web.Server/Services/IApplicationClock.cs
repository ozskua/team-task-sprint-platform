namespace SprintFlow.Web.Server.Services;

/// <summary>
/// What day it is in the company's timezone. The one place a calendar day is derived from a clock,
/// so the answer cannot differ between two parts of the application or change with the server's
/// location.
/// </summary>
public interface IApplicationClock
{
    DateOnly Today();
}
