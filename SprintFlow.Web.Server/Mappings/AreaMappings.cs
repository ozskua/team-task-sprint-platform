using SprintFlow.Web.Server.DTOs.Areas;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

/// <summary>
/// Hand-written entity to DTO mapping, kept in one place so what leaves the server is visible.
/// </summary>
public static class AreaMappings
{
    /// <summary>
    /// Requires <c>Include(a =&gt; a.CreatedByUser)</c> and the boards with their creators loaded.
    /// </summary>
    public static AreaResponseDto ToResponseDto(this Area area)
        => new(
            area.AreaId,
            area.Name,
            area.CreatedByUserId,
            area.CreatedByUser.FullName,
            area.CreatedDate,
            area.Boards
                .OrderBy(board => board.BoardId) // Oldest first, so the automatic board leads.
                .Select(board => board.ToResponseDto())
                .ToList());
}
