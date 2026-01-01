using MediatR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Features.Servers.Rules;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;

namespace Mootable.Application.Features.MootTables.Commands.CreateMootTable;

public sealed class CreateMootTableCommandHandler : IRequestHandler<CreateMootTableCommand, CreateMootTableResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ServerBusinessRules _serverRules;

    public CreateMootTableCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ServerBusinessRules serverRules)
    {
        _context = context;
        _currentUser = currentUser;
        _serverRules = serverRules;
    }

    public async Task<CreateMootTableResponse> Handle(CreateMootTableCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;

        var server = await _context.Servers
            .FirstOrDefaultAsync(s => s.Id == request.ServerId && !s.IsDeleted, cancellationToken);
        _serverRules.ServerMustExist(server);

        var member = await _context.ServerMembers
            .Include(m => m.Roles)
                .ThenInclude(r => r.ServerRole)
            .FirstOrDefaultAsync(m => m.ServerId == request.ServerId && m.UserId == userId && !m.IsDeleted, cancellationToken);
        _serverRules.UserMustBeMember(member);
        _serverRules.UserMustHavePermission(member!, ServerPermissions.CreateMootTables);

        var maxPosition = await _context.MootTables
            .Where(mt => mt.ServerId == request.ServerId && !mt.IsDeleted)
            .MaxAsync(mt => (int?)mt.Position, cancellationToken) ?? -1;

        var mootTable = new MootTable
        {
            Id = Guid.NewGuid(),
            ServerId = request.ServerId,
            Name = request.Name,
            Topic = request.Topic,
            CategoryId = request.CategoryId,
            Position = maxPosition + 1,
            Type = MootTableType.Text,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        _context.MootTables.Add(mootTable);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateMootTableResponse(
            MootTableId: mootTable.Id,
            ServerId: mootTable.ServerId,
            Name: mootTable.Name,
            Topic: mootTable.Topic,
            Position: mootTable.Position,
            CreatedAt: mootTable.CreatedAt
        );
    }
}
