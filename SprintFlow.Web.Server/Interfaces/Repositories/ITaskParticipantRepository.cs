using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Interfaces.Repositories;

/// <summary>
/// Reads and writes task assignment — and, because membership is derived from it, this is also
/// where a board's team members come from.
/// </summary>
public interface ITaskParticipantRepository
{
    /// <summary>
    /// The members of a board's team: everyone assigned to at least one task on that board.
    /// </summary>
    /// <remarks>
    /// The query filters on the task's board, not its column, which is why moving a task between
    /// columns can never change who is a member. Nothing is stored — this is the definition of
    /// membership, evaluated on each read.
    /// </remarks>
    Task<IReadOnlyList<User>> GetBoardMembersAsync(int boardId, CancellationToken cancellationToken);

    /// <summary>
    /// Every team, with the board it belongs to and the members derived from assignment. One query
    /// rather than one per team, so the Teams page does not fan out.
    /// </summary>
    Task<IReadOnlyDictionary<int, IReadOnlyList<User>>> GetMembersByBoardAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The same derivation as <see cref="GetMembersByBoardAsync"/>, narrowed to the boards asked for.
    /// Each member appears once per board, however many tasks they hold there.
    /// </summary>
    /// <remarks>
    /// Boards with no assignment at all are simply absent from the dictionary rather than present
    /// with an empty list, so the caller decides what "no members" should look like.
    /// </remarks>
    Task<IReadOnlyDictionary<int, IReadOnlyList<User>>> GetMembersByBoardsAsync(
        IReadOnlyCollection<int> boardIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// The task's assignment rows, tracked. Zero or one in practice — the unique index on
    /// <c>TaskId</c> guarantees it — but returned as a list so replacing works the same either way.
    /// </summary>
    Task<List<TaskParticipant>> GetForTaskAsync(int taskId, CancellationToken cancellationToken);

    void Add(TaskParticipant participant);

    void RemoveRange(IEnumerable<TaskParticipant> participants);
}
