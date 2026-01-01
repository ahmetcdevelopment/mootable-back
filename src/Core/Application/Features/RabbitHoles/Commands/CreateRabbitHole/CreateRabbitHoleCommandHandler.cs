using MediatR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Features.MootTables.Rules;
using Mootable.Application.Features.RabbitHoles.Rules;
using Mootable.Application.Features.Servers.Rules;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using Mootable.Domain.Events;

namespace Mootable.Application.Features.RabbitHoles.Commands.CreateRabbitHole;

public sealed class CreateRabbitHoleCommandHandler : IRequestHandler<CreateRabbitHoleCommand, CreateRabbitHoleResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly RabbitHoleBusinessRules _rabbitHoleRules;
    private readonly MootTableBusinessRules _mootTableRules;
    private readonly ServerBusinessRules _serverRules;

    public CreateRabbitHoleCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        RabbitHoleBusinessRules rabbitHoleRules,
        MootTableBusinessRules mootTableRules,
        ServerBusinessRules serverRules)
    {
        _context = context;
        _currentUser = currentUser;
        _rabbitHoleRules = rabbitHoleRules;
        _mootTableRules = mootTableRules;
        _serverRules = serverRules;
    }

    public async Task<CreateRabbitHoleResponse> Handle(CreateRabbitHoleCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;

        var mootTable = await _context.MootTables
            .Include(mt => mt.Server)
            .FirstOrDefaultAsync(mt => mt.Id == request.MootTableId && !mt.IsDeleted, cancellationToken);
        _mootTableRules.MootTableMustExist(mootTable);
        _mootTableRules.MootTableMustNotBeArchived(mootTable!);

        var member = await _context.ServerMembers
            .Include(m => m.Roles)
                .ThenInclude(r => r.ServerRole)
            .FirstOrDefaultAsync(m => m.ServerId == mootTable!.ServerId && m.UserId == userId && !m.IsDeleted, cancellationToken);
        _serverRules.UserMustBeMember(member);
        _serverRules.UserMustHavePermission(member!, ServerPermissions.CreateRabbitHoles);

        var starterMessage = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == request.StarterMessageId && m.MootTableId == request.MootTableId && !m.IsDeleted, cancellationToken);
        _rabbitHoleRules.MessageMustExist(starterMessage);

        var hasExistingRabbitHole = await _context.RabbitHoles
            .AnyAsync(rh => rh.StarterMessageId == request.StarterMessageId && !rh.IsDeleted, cancellationToken);
        _rabbitHoleRules.MessageMustNotHaveRabbitHole(hasExistingRabbitHole);

        var rabbitHole = new RabbitHole
        {
            Id = Guid.NewGuid(),
            MootTableId = request.MootTableId,
            StarterMessageId = request.StarterMessageId,
            Title = request.Title,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        starterMessage!.Type = MessageType.RabbitHoleStarter;
        
        rabbitHole.AddDomainEvent(new RabbitHoleCreatedEvent(
            rabbitHole.Id, 
            rabbitHole.MootTableId, 
            rabbitHole.StarterMessageId, 
            userId));

        _context.RabbitHoles.Add(rabbitHole);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateRabbitHoleResponse(
            RabbitHoleId: rabbitHole.Id,
            MootTableId: rabbitHole.MootTableId,
            Title: rabbitHole.Title,
            StarterMessageId: starterMessage.Id,
            StarterMessageContent: starterMessage.Content,
            CreatedAt: rabbitHole.CreatedAt
        );
    }
}
