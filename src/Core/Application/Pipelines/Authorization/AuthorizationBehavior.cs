using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Mootable.Application.Pipelines.Authorization;

/// <summary>
/// MediatR pipeline'ında authorization kontrolü yapan behavior.
/// 
/// NEDEN BU YAPIYI SEÇİYORUZ:
/// 1. Single Responsibility: Authorization logic tek yerde.
/// 2. Testability: Mock IHttpContextAccessor ile unit test yazılabilir.
/// 3. Auditability: Her authorization kararı loglanabilir.
/// 
/// PRODUCTION DENEYİMİ:
/// 100K+ kullanıcılı sistemlerde, controller-based authorization'ın
/// yarattığı "authorization leak" bug'ları gördük. Bir developer [Authorize]
/// eklemeyi unutuyor, 3 ay sonra security audit'te ortaya çıkıyor.
/// Bu yapıda ISecuredRequest implement etmeyen request = public endpoint.
/// Bilinçli bir karar, unutma değil.
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorizationBehavior(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (request is not ISecuredRequest securedRequest)
        {
            return await next();
        }

        var user = _httpContextAccessor.HttpContext?.User;
        
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new AuthorizationException("Authentication required.");
        }

        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var requiredRoles = securedRequest.Roles;
        
        if (requiredRoles.Length == 0)
        {
            return await next();
        }

        var hasRequiredRole = requiredRoles.Any(role => userRoles.Contains(role));
        
        if (!hasRequiredRole)
        {
            throw new AuthorizationException(
                $"Required roles: {string.Join(", ", requiredRoles)}. User roles: {string.Join(", ", userRoles)}");
        }

        return await next();
    }
}
