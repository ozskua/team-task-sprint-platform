namespace SprintFlow.Web.Server.DTOs.Tasks;

// StoredName is deliberately absent: it is an internal filesystem detail and downloads are
// addressed by attachment id.
public record TaskAttachmentResponseDto(
    int TaskAttachmentId,
    string FileName,
    string ContentType,
    long SizeBytes,
    int UploadedByUserId,
    string UploadedByFullName,
    DateTime CreatedDate);
