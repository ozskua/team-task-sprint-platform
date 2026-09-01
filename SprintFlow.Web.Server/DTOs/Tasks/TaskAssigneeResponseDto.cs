namespace SprintFlow.Web.Server.DTOs.Tasks;

public record TaskAssigneeResponseDto(
    int UserId,
    string FullName);
