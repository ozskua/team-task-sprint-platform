using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Data;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Repositories;

public class TaskParticipantRepository : ITaskParticipantRepository
{
    private readonly SprintFlowDbContext context;

    public TaskParticipantRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<User>> GetBoardMembersAsync(
        int boardId,
        CancellationToken cancellationToken)
        => await context.TaskParticipants
            .AsNoTracking()
            // Joined on the task's board. A task moving between columns does not touch BoardId, so
            // no column operation can add or remove a member.
            .Where(p => p.Task.BoardId == boardId)
            .Select(p => p.User)
            // Distinct because one user may be assigned several tasks on the same board; they are
            // one member, not several.
            .Distinct()
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<User>>> GetMembersByBoardAsync(
        CancellationToken cancellationToken)
    {
        var rows = await context.TaskParticipants
            .AsNoTracking()
            .Select(p => new { p.Task.BoardId, p.User })
            .Distinct()
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.BoardId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<User>)group
                    .Select(row => row.User)
                    .OrderBy(user => user.FirstName)
                    .ThenBy(user => user.LastName)
                    .ToList());
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<User>>> GetMembersByBoardsAsync(
        IReadOnlyCollection<int> boardIds,
        CancellationToken cancellationToken)
    {
        if (boardIds.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<User>>();
        }

        var rows = await context.TaskParticipants
            .AsNoTracking()
            .Where(p => boardIds.Contains(p.Task.BoardId))
            .Select(p => new { p.Task.BoardId, p.User })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.BoardId)
            .ToDictionary(
                group => group.Key,
                // One row per assigned task, so someone holding three tasks on this board arrives
                // three times. Distinct on the user id collapses them into the one member they are.
                // Done here, before anything is mapped, so no caller can render a duplicate.
                group => (IReadOnlyList<User>)group
                    .DistinctBy(row => row.User.UserId)
                    .Select(row => row.User)
                    .OrderBy(user => user.FirstName)
                    .ThenBy(user => user.LastName)
                    .ToList());
    }

    public Task<List<TaskParticipant>> GetForTaskAsync(int taskId, CancellationToken cancellationToken)
        => context.TaskParticipants
            .Where(p => p.TaskId == taskId)
            .ToListAsync(cancellationToken);

    public void Add(TaskParticipant participant) => context.TaskParticipants.Add(participant);

    public void RemoveRange(IEnumerable<TaskParticipant> participants)
        => context.TaskParticipants.RemoveRange(participants);
}
