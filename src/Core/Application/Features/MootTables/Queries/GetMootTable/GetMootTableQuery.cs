using MediatR;
using Mootable.Application.Pipelines.Authorization;
using Mootable.Application.Pipelines.Caching;

namespace Mootable.Application.Features.MootTables.Queries.GetMootTable;

public sealed record GetMootTableQuery(
    Guid MootTableId
) : IRequest<GetMootTableResponse>, ISecuredRequest, ICachableRequest
{
    public string[] Roles => Array.Empty<string>();
    
    public string CacheKey => $"moot-table:{MootTableId}";
    public string? CacheGroupKey => null;
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}
