using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Boards;

public class CreateBoardRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
