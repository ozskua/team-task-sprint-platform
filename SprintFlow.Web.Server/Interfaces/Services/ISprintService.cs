using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Sprints;

namespace SprintFlow.Web.Server.Interfaces.Services;

/// <summary>
/// Creating and reading sprints, and the rule that a board's sprints form one timeline.
/// </summary>
public interface ISprintService
{
    /// <summary>
    /// Creates a sprint on the board, provided it does not cover days another of the board's
    /// sprints already covers.
    /// </summary>
    Task<(CreateSprintOutcome Outcome, SprintResponseDto? Sprint)> CreateAsync(
        int boardId,
        CreateSprintRequestDto request,
        int createdByUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// The board's sprints in date order, or <c>null</c> when no such board exists. A board with no
    /// sprints yields an empty list, which is a normal state rather than a missing resource.
    /// </summary>
    Task<IReadOnlyList<SprintResponseDto>?> GetBoardSprintsAsync(
        int boardId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Every sprint in the system, ordered area → board → start day, for the system-wide overview.
    /// An empty list is a normal answer.
    /// </summary>
    /// <remarks>
    /// Read-only. Sprints are still owned by boards; this is a view across them, not a second place
    /// they can be created or changed.
    /// </remarks>
    Task<IReadOnlyList<SprintListItemResponseDto>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>One sprint, or <c>null</c> when no such sprint exists.</summary>
    Task<SprintDetailResponseDto?> GetAsync(int sprintId, CancellationToken cancellationToken);

    /// <summary>
    /// The tasks in one sprint, or <c>null</c> when no such sprint exists. An empty list is a
    /// normal answer — a sprint nobody has scheduled anything into yet.
    /// </summary>
    Task<IReadOnlyList<SprintTaskResponseDto>?> GetTasksAsync(
        int sprintId,
        CancellationToken cancellationToken);

    /// <summary>
    /// One sprint, but only if it belongs to the given board — <c>null</c> when it does not exist
    /// or sits on another board. The two are one answer on purpose: to a caller scoping a board by
    /// a sprint, "no such sprint" and "not this board's sprint" are the same refusal.
    /// </summary>
    Task<SprintResponseDto?> GetForBoardAsync(
        int boardId,
        int sprintId,
        CancellationToken cancellationToken);

    /// <summary>
    /// The board's sprint running today, or <c>null</c> when none is — which is a normal state, not
    /// an error: a board may have no sprints at all, or sit between two of them.
    /// </summary>
    /// <remarks>
    /// This is what the board page falls back to when the caller names no sprint, so the kanban
    /// shows the work actually in progress rather than everything the board has ever held.
    /// </remarks>
    Task<SprintResponseDto?> GetActiveForBoardAsync(int boardId, CancellationToken cancellationToken);
}
