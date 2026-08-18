namespace SprintFlow.Web.Server.Common.Enums;

/// <summary>
/// The three workflow columns every board is required to have. A column whose kind is
/// <c>null</c> is a custom column the user created.
/// </summary>
/// <remarks>
/// Explicit values, not the compiler's defaults: the numbers are stored in the database, so
/// reordering the members must never change what an existing row means.
/// </remarks>
public enum SystemColumnKind
{
    ToDo = 1,
    Completed = 2,
    Cancelled = 3
}
