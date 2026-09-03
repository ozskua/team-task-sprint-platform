namespace SprintFlow.Web.Server.Common.Enums;

public enum AddAttachmentOutcome
{
    Added,
    TaskNotFound,
    FileMissing,
    FileTooLarge,
    FileTypeNotAllowed,
    CommentNotFound
}
