namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class WorkspaceContactsEditViewData
{
    public bool CanViewContacts { get; set; }
    public bool CanEditContacts { get; set; }
    public bool CanCreateContacts { get; set; }
    public Contact? ExistingContact { get; set; }
    public List<TicketPriority> Priorities { get; set; } = [];
}

public interface IWorkspaceContactsEditViewService
{
    public Task<WorkspaceContactsEditViewData> BuildAsync(int workspaceId, int userId, int contactId = 0);
}


public class WorkspaceContactsEditViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository,
    IContactRepository contactRepository,
    ITicketPriorityRepository priorityRepository) : IWorkspaceContactsEditViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly IContactRepository contactRepository = contactRepository;
    private readonly ITicketPriorityRepository priorityRepository = priorityRepository;

    public async Task<WorkspaceContactsEditViewData> BuildAsync(int workspaceId, int userId, int contactId = 0)
    {
        var data = new WorkspaceContactsEditViewData();

        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);

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

        var priorities = await this.priorityRepository.ListAsync(workspaceId);
        data.Priorities = priorities != null ? [.. priorities] : [];

        if (contactId > 0)
        {
            data.ExistingContact = await this.contactRepository.FindAsync(workspaceId, contactId);
        }

        return data;
    }
}


