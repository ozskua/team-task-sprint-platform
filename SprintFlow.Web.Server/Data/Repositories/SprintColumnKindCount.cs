using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.Data.Repositories;

public sealed record SprintColumnKindCount(int SprintId, SystemColumnKind? SystemKind, int Count);
