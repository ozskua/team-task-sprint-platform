namespace SprintFlow.Web.Server.Options;

// Only the location is configurable: it is the one thing that differs from machine to machine. The
// size limit and the accepted file types are policy and live in AttachmentStorage.
public class AttachmentOptions
{
    public const string SectionName = "Attachments";

    public string RootPath { get; set; } = string.Empty;
}
