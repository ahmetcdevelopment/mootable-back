using MediatR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Features.MootTables.Rules;
using Mootable.Application.Features.Servers.Rules;
using Mootable.Application.Interfaces;

namespace Mootable.Application.Features.MootTables.Queries.GetMootTable;

public sealed class GetMootTableQueryHandler : IRequestHandler<GetMootTableQuery, GetMootTableResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly MootTableBusinessRules _mootTableRules;
    private readonly ServerBusinessRules _serverRules;

    public GetMootTableQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        MootTableBusinessRules mootTableRules,
        ServerBusinessRules serverRules)
    {
        _context = context;
        _currentUser = currentUser;
        _mootTableRules = mootTableRules;
        _serverRules = serverRules;
    }

    public async Task<GetMootTableResponse> Handle(GetMootTableQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;

        var mootTable = await _context.MootTables
            .Include(mt => mt.Server)
            .Include(mt => mt.Category)
            .FirstOrDefaultAsync(mt => mt.Id == request.MootTableId && !mt.IsDeleted, cancellationToken);

        _mootTableRules.MootTableMustExist(mootTable);

        var member = await _context.ServerMembers
            .FirstOrDefaultAsync(m => m.ServerId == mootTable!.ServerId && m.UserId == userId && !m.IsDeleted, cancellationToken);
        _serverRules.UserMustBeMember(member);

        var messageCount = await _context.Messages
            .CountAsync(m => m.MootTableId == request.MootTableId && !m.IsDeleted, cancellationToken);

        // Rabbit holes are topic-specific, not tied to MootTables
        var rabbitHoleCount = 0; // await _context.RabbitHoles
            

        return new GetMootTableResponse(
            Id: mootTable!.Id,
            ServerId: mootTable.ServerId,
            ServerName: mootTable.Server.Name,
            Name: mootTable.Name,
            Topic: mootTable.Topic,
            Position: mootTable.Position,
            Type: mootTable.Type.ToString(),
            IsArchived: mootTable.IsArchived,
            CategoryId: mootTable.CategoryId,
            CategoryName: mootTable.Category?.Name,
            MessageCount: messageCount,
            RabbitHoleCount: rabbitHoleCount,
            CreatedAt: mootTable.CreatedAt
        );
    }
}
