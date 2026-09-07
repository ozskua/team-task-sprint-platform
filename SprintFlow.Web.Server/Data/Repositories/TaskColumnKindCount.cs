using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.Data.Repositories;

// How many tasks sit behind one column kind, counted by the database rather than in memory.
// SystemKind is null for every custom column - Review, Testing, anything a board added - which is
// what the dashboard reports as Open.
public sealed record TaskColumnKindCount(SystemColumnKind? SystemKind, int Count);
