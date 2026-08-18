using SprintFlow.Web.Server.DTOs.Areas;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Interfaces.Services;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

public class AreaService : IAreaService
{
    private readonly IAreaRepository areaRepository;

    public AreaService(IAreaRepository areaRepository)
    {
        this.areaRepository = areaRepository;
    }

    public async Task<AreaResponseDto> CreateAsync(
        CreateAreaRequestDto request,
        int createdByUserId,
        CancellationToken cancellationToken)
    {
        var area = new Area
        {
            // Stored without surrounding whitespace, so "  Marketing  " and "Marketing" are the
            // same name. A name that is only whitespace never reaches here — [Required] rejects it.
            Name = request.Name.Trim(),
            CreatedByUserId = createdByUserId

            // CreatedDate is intentionally not set; the database default supplies it.
        };

        // An Area is created on its own. Boards are added afterwards by a user, who names them.
        var areaId = await areaRepository.AddAsync(area, cancellationToken);

        // Re-read so the response carries the creators' names and the generated dates from the
        // database rather than anything assembled here.
        var created = await areaRepository.GetByIdAsync(areaId, cancellationToken)
            ?? throw new InvalidOperationException($"Area {areaId} was saved but could not be read back.");

        return created.ToResponseDto();
    }

    public async Task<IReadOnlyList<AreaResponseDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var areas = await areaRepository.GetAllAsync(cancellationToken);

        return areas.Select(area => area.ToResponseDto()).ToList();
    }
}
