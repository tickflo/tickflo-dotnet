using System.Security.Claims;

namespace Tickflo.Core.Services;

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

    public int? GetUserId(ClaimsPrincipal principal)
    {
        return TryGetUserId(principal, out var userId) ? userId : null;
    }

    public int GetUserIdOrThrow(ClaimsPrincipal principal)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            throw new InvalidOperationException("Unable to extract user ID from claims.");
        }

        return userId;
    }
}
