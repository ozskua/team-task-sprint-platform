using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Users;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AuthService authService;
    private readonly ProfilePhotoService profilePhotoService;

    public UsersController(AuthService authService, ProfilePhotoService profilePhotoService)
    {
        this.authService = authService;
        this.profilePhotoService = profilePhotoService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await authService.GetUsersAsync(cancellationToken));
    }

    [HttpPut("me/photo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadMyPhoto(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return FileMissing();
        }

        await using var content = file.OpenReadStream();

        var outcome = await profilePhotoService.UploadAsync(
            User.GetAuthenticatedUserId(),
            file.FileName,
            file.Length,
            file.ContentType,
            content,
            cancellationToken);

        return outcome switch
        {
            UploadProfilePhotoOutcome.Uploaded => NoContent(),

            UploadProfilePhotoOutcome.FileMissing => FileMissing(),

            UploadProfilePhotoOutcome.FileTooLarge => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "File is too large",
                detail: $"A profile photo may be at most {ProfilePhotoService.MaxSizeBytes / (1024 * 1024)} MB."),

            UploadProfilePhotoOutcome.FileTypeNotAllowed => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "File type is not allowed",
                detail: "A profile photo must be a PNG, JPEG or WebP image."),

            _ => throw new InvalidOperationException($"Unhandled upload-profile-photo outcome {outcome}.")
        };
    }

    [HttpDelete("me/photo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveMyPhoto(CancellationToken cancellationToken)
    {
        await profilePhotoService.RemoveAsync(User.GetAuthenticatedUserId(), cancellationToken);

        return NoContent();
    }

    [HttpGet("{userId:int}/photo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPhoto(int userId, CancellationToken cancellationToken)
    {
        var photo = await profilePhotoService.GetForDownloadAsync(userId, cancellationToken);

        if (photo is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Profile photo not found",
                detail: "This user has no profile photo.");
        }

        var (key, contentType) = photo.Value;

        Response.ContentType = contentType;
        Response.Headers.ContentDisposition = "inline";
        Response.Headers.CacheControl = "private, no-store";

        await profilePhotoService.CopyToAsync(key, Response.Body, cancellationToken);

        return new EmptyResult();
    }

    private ObjectResult FileMissing()
        => Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "No file was sent",
            detail: "Send the photo as multipart form data under the field name 'file'.");
}
