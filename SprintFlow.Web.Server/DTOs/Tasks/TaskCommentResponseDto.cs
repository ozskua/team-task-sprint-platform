namespace SprintFlow.Web.Server.DTOs.Tasks;

public record TaskCommentResponseDto(
    int TaskCommentId,
    int UserId,
    string UserFullName,
    string Content,
    DateTime CreatedDate,
    IReadOnlyList<TaskAttachmentResponseDto> Attachments);
