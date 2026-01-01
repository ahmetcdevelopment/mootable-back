using MediatR;
using Mootable.Application.Pipelines.Authorization;
using Mootable.Application.Pipelines.Caching;
using Mootable.Application.Pipelines.Transaction;

namespace Mootable.Application.Features.RabbitHoles.Commands.CreateRabbitHole;

/// <summary>
/// RabbitHole (tavşan deliği) oluşturma komutu.
/// 
/// Bir mesajdan thread açma işlemi.
/// Matrix metaforu: "Follow the white rabbit" - ana tartışmadan dallanma.
/// </summary>
public sealed record CreateRabbitHoleCommand(
    Guid MootTableId,
    Guid StarterMessageId,
    string Title
) : IRequest<CreateRabbitHoleResponse>, 
    ISecuredRequest, 
    ITransactionalRequest,
    ICacheRemoverRequest
{
    public string[] Roles => Array.Empty<string>();
    
    public string[]? CacheKeysToRemove => new[] { $"message:{StarterMessageId}" };
    public string[]? CacheGroupKeysToRemove => new[] { $"moot-table:{MootTableId}:rabbit-holes" };
}
