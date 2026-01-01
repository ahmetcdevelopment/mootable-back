namespace Mootable.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
}
