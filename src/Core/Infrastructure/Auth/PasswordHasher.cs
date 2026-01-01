using System.Security.Cryptography;
using Mootable.Application.Interfaces;

namespace Mootable.Infrastructure.Auth;

/// <summary>
/// PBKDF2 tabanlı password hashing.
/// 
/// NEDEN PBKDF2:
/// - BCrypt/Argon2 için ek dependency gerekli
/// - .NET native PBKDF2 desteği var
/// - 100K iteration + 256-bit salt = brute force'a dayanıklı
/// 
/// PRODUCTION DENEYİMİ:
/// Iteration count çok düşük tutulursa (eski sistemlerde 1000 gibi),
/// GPU ile saniyede milyonlarca deneme yapılabilir.
/// 100K iteration, modern GPU'da bile yavaşlatıcı.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 32;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize
        );

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var storedHash = Convert.FromBase64String(parts[1]);

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize
        );

        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }
}
