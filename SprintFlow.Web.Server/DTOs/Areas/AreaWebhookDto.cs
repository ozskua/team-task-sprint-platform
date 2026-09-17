using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Areas;

public class AreaWebhookDto
{
    [MaxLength(2048)]
    public string? WebhookUrl { get; set; }
}
