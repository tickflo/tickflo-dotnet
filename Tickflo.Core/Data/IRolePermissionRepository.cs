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
    public Task<List<EffectiveSectionPermission>> ListByRoleAsync(int roleId);
    public Task UpsertAsync(int roleId, IEnumerable<EffectiveSectionPermission> permissions, int? actorUserId = null);
    public Task<Dictionary<string, EffectiveSectionPermission>> GetEffectivePermissionsForUserAsync(int workspaceId, int userId);
    public Task<string> GetTicketViewScopeForUserAsync(int workspaceId, int userId, bool isAdmin);
}
