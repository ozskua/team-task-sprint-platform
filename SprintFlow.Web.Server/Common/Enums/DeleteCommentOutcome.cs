namespace SprintFlow.Web.Server.Common.Enums;

public enum DeleteCommentOutcome
{
    Deleted,
    NotFound,

    // Someone else wrote it. The one place in this project where being the author decides anything.
    NotAuthor
}
