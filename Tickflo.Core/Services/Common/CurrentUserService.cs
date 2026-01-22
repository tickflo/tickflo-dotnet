namespace Tickflo.Core.Services.Common;

using System.Security.Claims;

/// <summary>
/// Implementation of ICurrentUserService for extracting current user from claims.
/// </summary>
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


