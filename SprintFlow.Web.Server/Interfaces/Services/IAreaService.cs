using SprintFlow.Web.Server.DTOs.Areas;

namespace SprintFlow.Web.Server.Interfaces.Services;

public interface IAreaService
{
    /// <summary>
    /// Creates an Area owned by <paramref name="createdByUserId"/>.
    /// </summary>
    /// <param name="createdByUserId">
    /// Resolved by the controller from the caller's token. Passed in rather than read from
    /// <c>HttpContext</c> so this layer stays free of the request.
    /// </param>
    Task<AreaResponseDto> CreateAsync(
        CreateAreaRequestDto request,
        int createdByUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AreaResponseDto>> GetAllAsync(CancellationToken cancellationToken);
}
