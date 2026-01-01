using Mootable.Domain.Entities;

namespace Mootable.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    RefreshToken GenerateRefreshToken(string ipAddress);
    bool ValidateAccessToken(string token);
}
