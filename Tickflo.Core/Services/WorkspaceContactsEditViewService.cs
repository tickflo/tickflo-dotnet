using Tickflo.Core.Data;

namespace Tickflo.Core.Services;

public class WorkspaceContactsEditViewService : IWorkspaceContactsEditViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly IContactRepository _contactRepo;
    private readonly ITicketPriorityRepository _priorityRepo;

    public WorkspaceContactsEditViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms,
        IContactRepository contactRepo,
        ITicketPriorityRepository priorityRepo)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
        _contactRepo = contactRepo;
        _priorityRepo = priorityRepo;
    }

    public async Task<WorkspaceContactsEditViewData> BuildAsync(int workspaceId, int userId, int contactId = 0)
    {
        var data = new WorkspaceContactsEditViewData();
        
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);

        if (isAdmin)
        {
            data.CanViewContacts = data.CanEditContacts = data.CanCreateContacts = true;
        }
        else if (eff.TryGetValue("contacts", out var cp))
        {
            data.CanViewContacts = cp.CanView;
            data.CanEditContacts = cp.CanEdit;
            data.CanCreateContacts = cp.CanCreate;
        }

        var priorities = await _priorityRepo.ListAsync(workspaceId);
        data.Priorities = priorities != null ? priorities.ToList() : new();

        if (contactId > 0)
        {
            data.ExistingContact = await _contactRepo.FindAsync(workspaceId, contactId);
        }

        return data;
    }
}
