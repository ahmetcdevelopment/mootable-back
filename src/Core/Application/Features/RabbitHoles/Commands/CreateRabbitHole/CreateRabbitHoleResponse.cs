namespace Mootable.Application.Features.RabbitHoles.Commands.CreateRabbitHole;

public sealed record CreateRabbitHoleResponse(
    Guid RabbitHoleId,
    Guid MootTableId,
    string Title,
    Guid StarterMessageId,
    string StarterMessageContent,
    DateTime CreatedAt
);
