using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Tasks;

// No UserId: the author is taken from the caller's token, so there is nothing here to spoof.
public class CreateTaskCommentRequestDto
{
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}
