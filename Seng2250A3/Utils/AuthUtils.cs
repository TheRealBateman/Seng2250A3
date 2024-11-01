using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Seng2250A3.Enums;

namespace Seng2250A3.Utils;

public class AuthUtils
{
    public static readonly Dictionary<string, (SecurityLevel? SecurityLevel, DateTime Expiry)> Tokens = new();

    public static bool IsTokenValid(string token)
    {
        return Tokens.TryGetValue(token, out var entry) && entry.Expiry > DateTime.UtcNow;
    }

    public static string GenerateToken(SecurityLevel? securityLevel)
    {
        var token = Guid.NewGuid().ToString();
        Tokens[token] = (securityLevel, DateTime.UtcNow.AddMinutes(15));
        return token;
    }

    public static string GenerateRandomPassword()
    {
        const int passwordLength = 12;
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        using var rng = new RNGCryptoServiceProvider();
        var randomBytes = new byte[passwordLength];
        rng.GetBytes(randomBytes);
        return new string(randomBytes.Select(b => validChars[b % validChars.Length]).ToArray());
    }

    public static string HashPassword(string password, out string salt)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(128 / 8);
        salt = Convert.ToBase64String(saltBytes);
        string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: saltBytes,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));
        return $"{salt}:{hashedPassword}";
    }

    public static bool VerifyPassword(string inputPassword, string storedPassword)
    {
        var passwordParts = storedPassword.Split(':');
        if (passwordParts.Length != 2) return false;
        var saltBytes = Convert.FromBase64String(passwordParts[0]);
        var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: inputPassword,
            salt: saltBytes,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));
        return hash == passwordParts[1];
    }
}