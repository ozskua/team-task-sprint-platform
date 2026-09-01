namespace SprintFlow.Web.Server.Common.Enums;

public enum CreateTaskOutcome
{
    Created,

    BoardNotFound,

    ColumnNotFound,

    ColumnOnDifferentBoard,

    AssigneeUnavailable,

    SprintNotFound,

    SprintOnDifferentBoard
}
