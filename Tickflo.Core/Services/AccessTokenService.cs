using System.Security.Cryptography;
using System.Text;

namespace Tickflo.Core.Services;

/// <summary>
/// Service for generating secure access tokens for client portal.
/// </summary>
public interface IAccessTokenService
{
    /// <summary>
    /// Generates a cryptographically secure random token.
    /// </summary>
    string GenerateToken();

    /// <summary>
    /// Generates a token with a specific length.
    /// </summary>
    string GenerateToken(int length);
}

/// <summary>
/// Implementation of access token generation service.
/// </summary>
public class AccessTokenService : IAccessTokenService
{
    private const string ValidCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    /// <summary>
    /// Generates a 32-character secure random token.
    /// </summary>
    public string GenerateToken()
        => GenerateToken(32);

    /// <summary>
    /// Generates a secure random token with specified length.
    /// </summary>
    public string GenerateToken(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenData = new byte[length];
        rng.GetBytes(tokenData);

        var sb = new StringBuilder(length);
        foreach (var b in tokenData)
        {
            sb.Append(ValidCharacters[b % ValidCharacters.Length]);
        }

        return sb.ToString();
    }
}
