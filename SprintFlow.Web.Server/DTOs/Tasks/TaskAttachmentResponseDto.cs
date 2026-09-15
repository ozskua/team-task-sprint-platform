namespace SprintFlow.Web.Server.DTOs.Tasks;

public record TaskAttachmentResponseDto(
    int TaskAttachmentId,
    string FileName,
    string ContentType,
    long SizeBytes,
    int UploadedByUserId,
    string UploadedByFullName,
    DateTime CreatedDate);
