using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class TeamsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IUserWorkspaceRoleRepository _uwr;
    private readonly ITeamRepository _teams;
    private readonly ITeamMemberRepository _members;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<Team> Teams { get; private set; } = new();
    public Dictionary<int,int> MemberCounts { get; private set; } = new();

    public TeamsModel(IWorkspaceRepository workspaces, IUserWorkspaceRoleRepository uwr, ITeamRepository teams, ITeamMemberRepository members)
    {
        _workspaces = workspaces;
        _uwr = uwr;
        _teams = teams;
        _members = members;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaces.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _uwr.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        Teams = await _teams.ListForWorkspaceAsync(Workspace.Id);
        MemberCounts = new Dictionary<int,int>();
        foreach (var t in Teams)
        {
            var ms = await _members.ListMembersAsync(t.Id);
            MemberCounts[t.Id] = ms.Count;
        }
        return Page();
    }
}
