namespace SprintFlow.Web.Server.Common.Constants;

/// <summary>
/// The role names stored in the <c>Roles</c> table. Referenced by the seed data today and by
/// authorization attributes once protected endpoints exist, so the strings live in one place.
/// </summary>
public static class RoleNames
{
    public const string Admin = "Admin";

    public const string Member = "Member";
}
