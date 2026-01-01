using Mootable.Domain.Common;

namespace Mootable.Domain.Events;

public sealed class UserRegisteredEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public string Username { get; }
    public string Email { get; }
    
    public UserRegisteredEvent(Guid userId, string username, string email)
    {
        UserId = userId;
        Username = username;
        Email = email;
    }
}
