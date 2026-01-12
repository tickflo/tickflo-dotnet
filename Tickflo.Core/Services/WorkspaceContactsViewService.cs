using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Core.Services;

public class WorkspaceContactsViewService : IWorkspaceContactsViewService
{
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IContactListingService _listingService;

    public WorkspaceContactsViewService(
        IWorkspaceAccessService workspaceAccessService,
        IContactListingService listingService)
    {
        _workspaceAccessService = workspaceAccessService;
        _listingService = listingService;
    }

    public async Task<WorkspaceContactsViewData> BuildAsync(int workspaceId, int userId, string? priorityFilter = null, string? searchQuery = null)
    {
        var data = new WorkspaceContactsViewData();

        // Get permissions
        var permissions = await _workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        if (permissions.TryGetValue("contacts", out var contactPermissions))
        {
            data.CanCreateContacts = contactPermissions.CanCreate;
            data.CanEditContacts = contactPermissions.CanEdit;
        }

        // Load contacts with filtering
        var (contacts, priorities) = await _listingService.GetListAsync(workspaceId, priorityFilter, searchQuery);
        data.Contacts = contacts.ToList();
        data.Priorities = priorities.ToList();
        data.PriorityColorByName = priorities.ToDictionary(
            p => p.Name,
            p => string.IsNullOrWhiteSpace(p.Color) ? "neutral" : p.Color);

        return data;
    }
}
