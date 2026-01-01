namespace Mootable.Application.Features.Servers.Constants;

public static class ServerMessages
{
    public const string ServerNotFound = "Server not found.";
    public const string NotServerMember = "You are not a member of this server.";
    public const string NotServerOwner = "Only the server owner can perform this action.";
    public const string InsufficientPermissions = "You don't have permission to perform this action.";
    public const string InvalidInviteCode = "Invalid or expired invite code.";
    public const string AlreadyMember = "You are already a member of this server.";
    public const string CannotLeaveOwnServer = "Server owner cannot leave. Transfer ownership or delete the server.";
    public const string MaxServersReached = "You have reached the maximum number of servers.";
    
    public static class Validation
    {
        public const string NameRequired = "Server name is required.";
        public const string NameMinLength = "Server name must be at least 2 characters.";
        public const string NameMaxLength = "Server name cannot exceed 100 characters.";
        public const string DescriptionMaxLength = "Description cannot exceed 1000 characters.";
    }
}
