namespace Mootable.Application.Features.MootTables.Queries.GetMootTable;

public sealed record GetMootTableResponse(
    Guid Id,
    Guid ServerId,
    string ServerName,
    string Name,
    string? Topic,
    int Position,
    string Type,
    bool IsArchived,
    Guid? CategoryId,
    string? CategoryName,
    int MessageCount,
    int RabbitHoleCount,
    DateTime CreatedAt
);
