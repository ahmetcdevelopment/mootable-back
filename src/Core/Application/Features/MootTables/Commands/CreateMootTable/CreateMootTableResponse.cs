namespace Mootable.Application.Features.MootTables.Commands.CreateMootTable;

public sealed record CreateMootTableResponse(
    Guid MootTableId,
    Guid ServerId,
    string Name,
    string? Topic,
    int Position,
    DateTime CreatedAt
);
