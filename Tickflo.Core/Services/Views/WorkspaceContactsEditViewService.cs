namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

public class WorkspaceContactsEditViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePerms,
    IContactRepository contactRepo,
    ITicketPriorityRepository priorityRepo) : IWorkspaceContactsEditViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;
    private readonly IContactRepository _contactRepo = contactRepo;
    private readonly ITicketPriorityRepository _priorityRepo = priorityRepo;

    public async Task<WorkspaceContactsEditViewData> BuildAsync(int workspaceId, int userId, int contactId = 0)
    {
        var data = new WorkspaceContactsEditViewData();

        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await this._rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);

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

        var priorities = await this._priorityRepo.ListAsync(workspaceId);
        data.Priorities = priorities != null ? [.. priorities] : [];

        if (contactId > 0)
        {
            data.ExistingContact = await this._contactRepo.FindAsync(workspaceId, contactId);
        }

        return data;
    }
}


