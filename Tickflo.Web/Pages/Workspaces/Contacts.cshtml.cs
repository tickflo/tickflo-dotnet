namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class ContactsModel(
    IWorkspaceService workspaceService,
    IWorkspaceContactsViewService workspaceContactsViewService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IWorkspaceContactsViewService workspaceContactsViewService = workspaceContactsViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public IReadOnlyList<Contact> Contacts { get; private set; } = [];
    public IReadOnlyList<TicketPriority> Priorities { get; private set; } = [];
    public Dictionary<string, string> PriorityColorByName { get; private set; } = [];
    public bool CanCreateContacts { get; private set; }
    public bool CanEditContacts { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Priority { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var viewData = await this.workspaceContactsViewService.BuildAsync(this.Workspace.Id, uid, this.Priority, this.Query);

        if (this.EnsurePermissionOrForbid(viewData.CanCreateContacts || viewData.CanEditContacts) is IActionResult permCheck)
        {
            return permCheck;
        }

        this.Contacts = viewData.Contacts;
        this.Priorities = viewData.Priorities;
        this.PriorityColorByName = viewData.PriorityColorByName;
        this.CanCreateContacts = viewData.CanCreateContacts;
        this.CanEditContacts = viewData.CanEditContacts;

        return this.Page();
    }
}


