using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class TeamsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceTeamsViewService _viewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<Team> Teams { get; private set; } = new();
    public Dictionary<int, int> MemberCounts { get; private set; } = new();
    public bool CanCreateTeams { get; private set; }
    public bool CanEditTeams { get; private set; }

    public TeamsModel(
        IWorkspaceRepository workspaces,
        ICurrentUserService currentUserService,
        IWorkspaceTeamsViewService viewService)
    {
        _workspaces = workspaces;
        _currentUserService = currentUserService;
        _viewService = viewService;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaces.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();

        if (!_currentUserService.TryGetUserId(User, out var uid)) return Forbid();

        var viewData = await _viewService.BuildAsync(Workspace.Id, uid);
        
        if (!viewData.CanViewTeams) return Forbid();

        Teams = viewData.Teams;
        MemberCounts = viewData.MemberCounts;
        CanCreateTeams = viewData.CanCreateTeams;
        CanEditTeams = viewData.CanEditTeams;

        return Page();
    }
}
