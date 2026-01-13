using System.Security.Claims;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Common;

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
    bool TryGetUserId(ClaimsPrincipal principal, out int userId);

    /// <summary>
    /// Gets the current user's ID from claims principal.
    /// </summary>
    /// <param name="principal">The claims principal from the HTTP request</param>
    /// <returns>The user ID, or null if not found</returns>
    int? GetUserId(ClaimsPrincipal principal);

    /// <summary>
    /// Gets the current user's ID from claims principal, throwing if not found.
    /// </summary>
    /// <param name="principal">The claims principal from the HTTP request</param>
    /// <returns>The user ID</returns>
    /// <exception cref="InvalidOperationException">Thrown if user ID cannot be extracted</exception>
    int GetUserIdOrThrow(ClaimsPrincipal principal);
}


