namespace SprintFlow.Web.Server.Interfaces.Services;

/// <summary>
/// What day it is, in the company's timezone.
/// </summary>
/// <remarks>
/// The one place a calendar day is derived from a clock. Everything that compares a sprint against
/// "now" goes through this, so the answer cannot differ between two parts of the application and
/// cannot change because the server was moved.
/// </remarks>
public interface IApplicationClock
{
    /// <summary>
    /// Today's date where the company is, taken from the current UTC instant and converted through
    /// the configured timezone.
    /// </summary>
    DateOnly Today();
}
