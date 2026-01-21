namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class WorkspaceLocationsEditViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePerms,
    ILocationRepository locationRepo,
    IUserWorkspaceRepository userWorkspaces,
    IUserRepository users,
    IContactRepository contacts) : IWorkspaceLocationsEditViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;
    private readonly ILocationRepository _locationRepo = locationRepo;
    private readonly IUserWorkspaceRepository _userWorkspaces = userWorkspaces;
    private readonly IUserRepository _users = users;
    private readonly IContactRepository _contacts = contacts;

    public async Task<WorkspaceLocationsEditViewData> BuildAsync(int workspaceId, int userId, int locationId = 0)
    {
        var data = new WorkspaceLocationsEditViewData();

        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await this._rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);

        if (isAdmin)
        {
            data.CanViewLocations = data.CanEditLocations = data.CanCreateLocations = true;
        }
        else if (eff.TryGetValue("locations", out var lp))
        {
            data.CanViewLocations = lp.CanView;
            data.CanEditLocations = lp.CanEdit;
            data.CanCreateLocations = lp.CanCreate;
        }

        // Load members for default assignee selection
        var memberships = await this._userWorkspaces.FindForWorkspaceAsync(workspaceId);
        if (memberships != null)
        {
            foreach (var m in memberships.Select(m => m.UserId).Distinct())
            {
                var u = await this._users.FindByIdAsync(m);
                if (u != null)
                {
                    data.MemberOptions.Add(u);
                }
            }
        }

        // Load all contacts
        var contacts = await this._contacts.ListAsync(workspaceId);
        data.ContactOptions = contacts != null ? [.. contacts] : [];

        if (locationId > 0)
        {
            data.ExistingLocation = await this._locationRepo.FindAsync(workspaceId, locationId);
            if (data.ExistingLocation != null)
            {
                var selected = await this._locationRepo.ListContactIdsAsync(workspaceId, locationId);
                data.SelectedContactIds = [.. selected];
            }
        }
        else
        {
            data.ExistingLocation = new Location { WorkspaceId = workspaceId, Active = true };
            data.SelectedContactIds = [];
        }

        return data;
    }
}


