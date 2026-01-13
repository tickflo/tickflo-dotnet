using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Views;

public class WorkspaceLocationsEditViewService : IWorkspaceLocationsEditViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly ILocationRepository _locationRepo;
    private readonly IUserWorkspaceRepository _userWorkspaces;
    private readonly IUserRepository _users;
    private readonly IContactRepository _contacts;

    public WorkspaceLocationsEditViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms,
        ILocationRepository locationRepo,
        IUserWorkspaceRepository userWorkspaces,
        IUserRepository users,
        IContactRepository contacts)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
        _locationRepo = locationRepo;
        _userWorkspaces = userWorkspaces;
        _users = users;
        _contacts = contacts;
    }

    public async Task<WorkspaceLocationsEditViewData> BuildAsync(int workspaceId, int userId, int locationId = 0)
    {
        var data = new WorkspaceLocationsEditViewData();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);

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
        var memberships = await _userWorkspaces.FindForWorkspaceAsync(workspaceId);
        if (memberships != null)
        {
            foreach (var m in memberships.Select(m => m.UserId).Distinct())
            {
                var u = await _users.FindByIdAsync(m);
                if (u != null) data.MemberOptions.Add(u);
            }
        }

        // Load all contacts
        var contacts = await _contacts.ListAsync(workspaceId);
        data.ContactOptions = contacts != null ? contacts.ToList() : new();

        if (locationId > 0)
        {
            data.ExistingLocation = await _locationRepo.FindAsync(workspaceId, locationId);
            if (data.ExistingLocation != null)
            {
                var selected = await _locationRepo.ListContactIdsAsync(workspaceId, locationId);
                data.SelectedContactIds = selected.ToList();
            }
        }
        else
        {
            data.ExistingLocation = new Location { WorkspaceId = workspaceId, Active = true };
            data.SelectedContactIds = new();
        }

        return data;
    }
}


