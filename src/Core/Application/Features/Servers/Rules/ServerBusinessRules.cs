using Mootable.Application.Features.Servers.Constants;
using Mootable.Domain.Entities;
using Mootable.Domain.Exceptions;

namespace Mootable.Application.Features.Servers.Rules;

public sealed class ServerBusinessRules
{
    public void ServerMustExist(Server? server)
    {
        if (server == null || server.IsDeleted)
        {
            throw new BusinessRuleException("SRV_001", ServerMessages.ServerNotFound);
        }
    }

    public void UserMustBeMember(ServerMember? member)
    {
        if (member == null || member.IsDeleted)
        {
            throw new BusinessRuleException("SRV_002", ServerMessages.NotServerMember);
        }
    }

    public void UserMustBeOwner(Server server, Guid userId)
    {
        if (server.OwnerId != userId)
        {
            throw new BusinessRuleException("SRV_003", ServerMessages.NotServerOwner);
        }
    }

    public void UserMustHavePermission(ServerMember member, ServerPermissions requiredPermission)
    {
        var userPermissions = member.Roles
            .Select(r => r.ServerRole.Permissions)
            .Aggregate(ServerPermissions.None, (current, p) => current | p);

        if (!userPermissions.HasFlag(ServerPermissions.Administrator) && 
            !userPermissions.HasFlag(requiredPermission))
        {
            throw new BusinessRuleException("SRV_004", ServerMessages.InsufficientPermissions);
        }
    }

    public void UserMustNotBeAlreadyMember(ServerMember? member)
    {
        if (member != null && !member.IsDeleted)
        {
            throw new BusinessRuleException("SRV_005", ServerMessages.AlreadyMember);
        }
    }

    public void InviteCodeMustBeValid(Server? server)
    {
        if (server == null || server.IsDeleted)
        {
            throw new BusinessRuleException("SRV_006", ServerMessages.InvalidInviteCode);
        }
    }

    public void OwnerCannotLeaveServer(Server server, Guid userId)
    {
        if (server.OwnerId == userId)
        {
            throw new BusinessRuleException("SRV_007", ServerMessages.CannotLeaveOwnServer);
        }
    }
}
