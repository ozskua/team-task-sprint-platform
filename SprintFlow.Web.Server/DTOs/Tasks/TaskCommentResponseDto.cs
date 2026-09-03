namespace SprintFlow.Web.Server.DTOs.Tasks;

// UserId is here so the client can tell which comments are its own and offer to delete them. The
// server checks it again on delete; this is only what makes the control appear.
public record TaskCommentResponseDto(
    int TaskCommentId,
    int UserId,
    string UserFullName,
    string Content,
    DateTime CreatedDate,
    IReadOnlyList<TaskAttachmentResponseDto> Attachments);
