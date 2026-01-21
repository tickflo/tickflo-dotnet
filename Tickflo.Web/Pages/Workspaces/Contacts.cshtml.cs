namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Views;

[Authorize]
public class ContactsModel(
    IWorkspaceRepository workspaceRepo,
    IUserWorkspaceRepository userWorkspaceRepo,
    ICurrentUserService currentUserService,
    IWorkspaceContactsViewService viewService) : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo = workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo = userWorkspaceRepo;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IWorkspaceContactsViewService _viewService = viewService;

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

        var result = await this.LoadWorkspaceAndValidateUserMembershipAsync(this._workspaceRepo, this._userWorkspaceRepo, slug);
        if (result is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        this.Workspace = workspace;

        var viewData = await this._viewService.BuildAsync(this.Workspace!.Id, uid, this.Priority, this.Query);

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


