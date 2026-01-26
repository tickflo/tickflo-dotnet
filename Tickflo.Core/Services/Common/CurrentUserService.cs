namespace Tickflo.Core.Services.Common;

using System.Security.Claims;

/// <summary>
/// Implementation of ICurrentUserService for extracting current user from claims.
/// </summary>

/// <summary>
/// Service for extracting and managing current user information from claims.
/// Centralizes the pattern of extracting user ID from claims across the application.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Attempts to extract the current user ID from the given claims principal.
    /// </summary>
    /// <param name="principal">The claims principal from the HTTP request</param>
    /// <param name="userId">The extracted user ID if successful</param>
    /// <returns>True if user ID was successfully extracted, false otherwise</returns>
    public bool TryGetUserId(ClaimsPrincipal principal, out int userId);

    /// <summary>
    /// Gets the current user's ID from claims principal.
    /// </summary>
    /// <param name="principal">The claims principal from the HTTP request</param>
    /// <returns>The user ID, or null if not found</returns>
    public int? GetUserId(ClaimsPrincipal principal);

    /// <summary>
    /// Gets the current user's ID from claims principal, throwing if not found.
    /// </summary>
    /// <param name="principal">The claims principal from the HTTP request</param>
    /// <returns>The user ID</returns>
    /// <exception cref="InvalidOperationException">Thrown if user ID cannot be extracted</exception>
    public int GetUserIdOrThrow(ClaimsPrincipal principal);
}

public class CurrentUserService : ICurrentUserService
{
    public bool TryGetUserId(ClaimsPrincipal principal, out int userId)
    {
        var idValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idValue, out userId))
        {
            return true;
        }

        userId = default;
        return false;
    }

    public int? GetUserId(ClaimsPrincipal principal) => this.TryGetUserId(principal, out var userId) ? userId : null;

    public int GetUserIdOrThrow(ClaimsPrincipal principal)
    {
        if (!this.TryGetUserId(principal, out var userId))
        {
            throw new InvalidOperationException("Unable to extract user ID from claims.");
        }

        return userId;
    }
}


