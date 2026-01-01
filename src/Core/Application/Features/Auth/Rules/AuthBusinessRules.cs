using Mootable.Application.Features.Auth.Constants;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using Mootable.Domain.Exceptions;

namespace Mootable.Application.Features.Auth.Rules;

/// <summary>
/// Auth domain'i için business rules.
/// 
/// NEDEN AYRI CLASS:
/// 1. Handler'lar business logic ile şişmez
/// 2. Aynı rule birden fazla handler'da kullanılabilir
/// 3. Unit test yazması kolay
/// 4. Rule ihlali durumunda consistent exception handling
/// 
/// ANTI-PATTERN:
/// Handler içinde if-else ile validation yapmak.
/// 6 ay sonra aynı validation 5 farklı handler'da copy-paste edilmiş olur.
/// Bir tanesi güncellenir, diğerleri unutulur = inconsistent behavior.
/// </summary>
public sealed class AuthBusinessRules
{
    private readonly IPasswordHasher _passwordHasher;

    public AuthBusinessRules(IPasswordHasher passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public void UserMustExistForLogin(User? user)
    {
        if (user == null)
        {
            throw new BusinessRuleException("AUTH_001", AuthMessages.InvalidCredentials);
        }
    }

    public void PasswordMustBeCorrect(string password, string passwordHash)
    {
        if (!_passwordHasher.Verify(password, passwordHash))
        {
            throw new BusinessRuleException("AUTH_002", AuthMessages.InvalidCredentials);
        }
    }

    public void EmailMustBeUnique(bool exists)
    {
        if (exists)
        {
            throw new BusinessRuleException("AUTH_003", AuthMessages.EmailAlreadyExists);
        }
    }

    public void UsernameMustBeUnique(bool exists)
    {
        if (exists)
        {
            throw new BusinessRuleException("AUTH_004", AuthMessages.UsernameAlreadyExists);
        }
    }

    public void RefreshTokenMustBeValid(RefreshToken? token)
    {
        if (token == null || !token.IsActive)
        {
            throw new BusinessRuleException("AUTH_005", AuthMessages.InvalidRefreshToken);
        }
    }

    public void UserMustNotBeDeleted(User? user)
    {
        if (user == null || user.IsDeleted)
        {
            throw new BusinessRuleException("AUTH_006", AuthMessages.UserNotFound);
        }
    }
}
