using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class EffectiveSectionPermission
{
    public string Section { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
    public bool CanCreate { get; set; }
    public string? TicketViewScope { get; set; } // null for non-ticket sections
}

public interface IRolePermissionRepository
{
    Task<List<EffectiveSectionPermission>> ListByRoleAsync(int roleId);
    Task UpsertAsync(int roleId, IEnumerable<EffectiveSectionPermission> permissions, int? actorUserId = null);
    Task<Dictionary<string, EffectiveSectionPermission>> GetEffectivePermissionsForUserAsync(int workspaceId, int userId);
    Task<string> GetTicketViewScopeForUserAsync(int workspaceId, int userId, bool isAdmin);
}
