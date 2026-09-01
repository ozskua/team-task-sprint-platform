namespace SprintFlow.Web.Server.Common.Enums;

public enum CreateSprintOutcome
{
    Created,

    BoardNotFound,

    EndNotAfterStart,

    OverlapsExistingSprint
}
