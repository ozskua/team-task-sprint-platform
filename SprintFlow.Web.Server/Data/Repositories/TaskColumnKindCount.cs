using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.Data.Repositories;

public sealed record TaskColumnKindCount(SystemColumnKind? SystemKind, int Count);
