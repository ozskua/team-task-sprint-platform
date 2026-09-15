using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Tasks;

public class CreateTaskCommentRequestDto
{
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}
