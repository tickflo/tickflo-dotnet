namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Contacts;
using Tickflo.Core.Services.Workspace;

public class WorkspaceContactsViewService(
    IWorkspaceAccessService workspaceAccessService,
    IContactListingService listingService) : IWorkspaceContactsViewService
{
    private readonly IWorkspaceAccessService _workspaceAccessService = workspaceAccessService;
    private readonly IContactListingService _listingService = listingService;

    public async Task<WorkspaceContactsViewData> BuildAsync(int workspaceId, int userId, string? priorityFilter = null, string? searchQuery = null)
    {
        var data = new WorkspaceContactsViewData();

        // Get permissions
        var permissions = await this._workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        if (permissions.TryGetValue("contacts", out var contactPermissions))
        {
            data.CanCreateContacts = contactPermissions.CanCreate;
            data.CanEditContacts = contactPermissions.CanEdit;
        }

        // Load contacts with filtering
        var (contacts, priorities) = await this._listingService.GetListAsync(workspaceId, priorityFilter, searchQuery);
        data.Contacts = [.. contacts];
        data.Priorities = [.. priorities];
        data.PriorityColorByName = priorities.ToDictionary(
            p => p.Name,
            p => string.IsNullOrWhiteSpace(p.Color) ? "neutral" : p.Color);

        return data;
    }
}



