using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.Data.Repositories;

// How many tasks one sprint holds behind one column kind. TaskColumnKindCount answers the same
// question for the whole system; this one keeps the sprint, because the report needs a breakdown
// per sprint rather than a single total.
public sealed record SprintColumnKindCount(int SprintId, SystemColumnKind? SystemKind, int Count);
