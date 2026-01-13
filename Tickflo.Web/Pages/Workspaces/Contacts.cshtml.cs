using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Views;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ContactsModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceContactsViewService _viewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public IReadOnlyList<Contact> Contacts { get; private set; } = Array.Empty<Contact>();
    public IReadOnlyList<TicketPriority> Priorities { get; private set; } = Array.Empty<TicketPriority>();
    public Dictionary<string, string> PriorityColorByName { get; private set; } = new();
    public bool CanCreateContacts { get; private set; }
    public bool CanEditContacts { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Priority { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    public ContactsModel(
        IWorkspaceRepository workspaceRepo,
        ICurrentUserService currentUserService,
        IWorkspaceContactsViewService viewService)
    {
        _workspaceRepo = workspaceRepo;
        _currentUserService = currentUserService;
        _viewService = viewService;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        
        var result = await LoadWorkspaceAndUserOrExitAsync(_workspaceRepo, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        Workspace = workspace;

        var viewData = await _viewService.BuildAsync(Workspace.Id, uid, Priority, Query);
        
        if (EnsurePermissionOrForbid(viewData.CanCreateContacts || viewData.CanEditContacts) is IActionResult permCheck) return permCheck;

        Contacts = viewData.Contacts;
        Priorities = viewData.Priorities;
        PriorityColorByName = viewData.PriorityColorByName;
        CanCreateContacts = viewData.CanCreateContacts;
        CanEditContacts = viewData.CanEditContacts;

        return Page();
    }
}


