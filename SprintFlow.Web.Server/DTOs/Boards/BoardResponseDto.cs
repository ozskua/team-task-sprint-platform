namespace SprintFlow.Web.Server.DTOs.Boards;

public record BoardResponseDto(
    int BoardId,
    string Name,
    int AreaId,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate);
