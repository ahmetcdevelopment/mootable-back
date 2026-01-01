using MediatR;
using Mootable.Application.Pipelines.Authorization;
using Mootable.Application.Pipelines.Caching;
using Mootable.Application.Pipelines.Logging;
using Mootable.Application.Pipelines.Transaction;

namespace Mootable.Application.Features.Servers.Commands.CreateServer;

/// <summary>
/// Server oluşturma komutu.
/// 
/// ISecuredRequest: Authenticated user gerekli.
/// ITransactionalRequest: Server + Owner Member + Default Roles atomik oluşturulmalı.
/// ICacheRemoverRequest: User'ın server listesi cache'i invalidate edilmeli.
/// </summary>
public sealed record CreateServerCommand(
    string Name,
    string? Description,
    bool IsPublic
) : IRequest<CreateServerResponse>, 
    ISecuredRequest, 
    ITransactionalRequest, 
    ILoggableRequest,
    ICacheRemoverRequest
{
    public string[] Roles => Array.Empty<string>();
    
    public string[]? CacheKeysToRemove => null;
    public string[]? CacheGroupKeysToRemove => new[] { "user-servers" };
}
