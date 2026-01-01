using MediatR;
using Mootable.Application.Pipelines.Authorization;
using Mootable.Application.Pipelines.Caching;

namespace Mootable.Application.Features.MootTables.Commands.CreateMootTable;

public sealed record CreateMootTableCommand(
    Guid ServerId,
    string Name,
    string? Topic,
    Guid? CategoryId
) : IRequest<CreateMootTableResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public string[] Roles => Array.Empty<string>();
    
    public string[]? CacheKeysToRemove => null;
    public string[]? CacheGroupKeysToRemove => new[] { $"server:{ServerId}:moot-tables" };
}
