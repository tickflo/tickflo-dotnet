namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class WorkspaceLocationsEditViewData
{
    public bool CanViewLocations { get; set; }
    public bool CanEditLocations { get; set; }
    public bool CanCreateLocations { get; set; }
    public Location? ExistingLocation { get; set; }
    public List<int> SelectedContactIds { get; set; } = [];
    public List<User> MemberOptions { get; set; } = [];
    public List<Contact> ContactOptions { get; set; } = [];
}

public interface IWorkspaceLocationsEditViewService
{
    public Task<WorkspaceLocationsEditViewData> BuildAsync(int workspaceId, int userId, int locationId = 0);
}


public class WorkspaceLocationsEditViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository,
    ILocationRepository locationRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    IUserRepository userRepository,
    IContactRepository contactRepository) : IWorkspaceLocationsEditViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly ILocationRepository locationRepository = locationRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IUserRepository userRepository = userRepository;
    private readonly IContactRepository contactRepository = contactRepository;

    public async Task<WorkspaceLocationsEditViewData> BuildAsync(int workspaceId, int userId, int locationId = 0)
    {
        var data = new WorkspaceLocationsEditViewData();

        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);

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
        var memberships = await this.userWorkspaceRepository.FindForWorkspaceAsync(workspaceId);
        if (memberships != null)
        {
            foreach (var memberUserId in memberships.Select(m => m.UserId).Distinct())
            {
                var user = await this.userRepository.FindByIdAsync(memberUserId);
                if (user != null)
                {
                    data.MemberOptions.Add(user);
                }
            }
        }

        // Load all contacts
        var contacts = await this.contactRepository.ListAsync(workspaceId);
        data.ContactOptions = contacts != null ? [.. contacts] : [];

        if (locationId > 0)
        {
            data.ExistingLocation = await this.locationRepository.FindAsync(workspaceId, locationId);
            if (data.ExistingLocation != null)
            {
                var selected = await this.locationRepository.ListContactIdsAsync(workspaceId, locationId);
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
