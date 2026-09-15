using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Areas;

public class CreateAreaRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
