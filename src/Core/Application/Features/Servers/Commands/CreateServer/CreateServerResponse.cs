namespace Mootable.Application.Features.Servers.Commands.CreateServer;

public sealed record CreateServerResponse(
    Guid ServerId,
    string Name,
    string? Description,
    string InviteCode,
    bool IsPublic,
    DateTime CreatedAt
);
