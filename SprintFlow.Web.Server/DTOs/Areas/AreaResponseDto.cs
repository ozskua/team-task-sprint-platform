using SprintFlow.Web.Server.DTOs.Boards;

namespace SprintFlow.Web.Server.DTOs.Areas;

/// <summary>
/// An Area as returned to a client. The creator is included as information only; it confers no
/// permission over the Area.
/// </summary>
/// <param name="AreaId">Identifies the Area.</param>
/// <param name="Name">Trimmed name as stored.</param>
/// <param name="CreatedByUserId">The user the server resolved from the token at creation time.</param>
/// <param name="CreatedByFullName">Creator's display name, so the client never has to guess it.</param>
/// <param name="CreatedDate">Stored without an offset, so the client renders it as-is.</param>
/// <param name="Boards">
/// The Area's boards. A newly created Area always carries the first one, so the client never has to
/// fetch it separately or assume it exists.
/// </param>
public record AreaResponseDto(
    int AreaId,
    string Name,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate,
    IReadOnlyList<BoardResponseDto> Boards);
