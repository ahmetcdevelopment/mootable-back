using MediatR;
using Mootable.Application.Pipelines.Logging;

namespace Mootable.Application.Features.Auth.Commands.Login;

/// <summary>
/// Login işlemi için Command.
/// ILoggableRequest: Başarılı/başarısız tüm login attempt'leri loglanmalı (security audit).
/// ISecuredRequest YOK: Login public endpoint, authentication gerektirmez.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password,
    string IpAddress
) : IRequest<LoginResponse>, ILoggableRequest
{
    bool ILoggableRequest.LogResponse => false;
}
