namespace SprintFlow.Web.Server.DTOs.Boards;

/// <summary>
/// A board as returned to a client, nested inside its Area. Ownership information only — it confers
/// no permission over the Board.
/// </summary>
/// <param name="BoardId">Identifies the Board.</param>
/// <param name="Name">Display name.</param>
/// <param name="AreaId">The Area this Board belongs to.</param>
/// <param name="CreatedByUserId">Inherited from the Area's creator when created automatically.</param>
/// <param name="CreatedByFullName">Creator's display name, so the client never has to guess it.</param>
/// <param name="CreatedDate">Stored without an offset, so the client renders it as-is.</param>
public record BoardResponseDto(
    int BoardId,
    string Name,
    int AreaId,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate);
