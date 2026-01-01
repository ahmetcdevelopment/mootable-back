using MediatR;

namespace Mootable.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string Token,
    string IpAddress
) : IRequest<RefreshTokenResponse>;
