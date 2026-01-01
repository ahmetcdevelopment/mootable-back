using MediatR;
using Mootable.Application.Pipelines.Logging;
using Mootable.Application.Pipelines.Transaction;

namespace Mootable.Application.Features.Auth.Commands.Register;

/// <summary>
/// Register işlemi için Command.
/// ITransactionalRequest: User + UserRole birlikte oluşturulmalı, partial state olmamalı.
/// ILoggableRequest: Yeni kullanıcı kaydı audit için loglanmalı.
/// </summary>
public sealed record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string? DisplayName,
    string IpAddress
) : IRequest<RegisterResponse>, ILoggableRequest, ITransactionalRequest;
