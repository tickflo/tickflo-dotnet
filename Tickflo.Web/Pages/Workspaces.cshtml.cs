using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Tickflo.Web.Pages;

public class WorkspacesModel : PageModel
{
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public List<WorkspaceView> Workspaces { get; set; } = new();

    public WorkspacesModel(IUserWorkspaceRepository userWorkspaceRepo, IWorkspaceRepository workspaceRepo, IHttpContextAccessor httpContextAccessor)
    {
        _userWorkspaceRepo = userWorkspaceRepo;
        _workspaceRepo = workspaceRepo;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return Challenge();

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Challenge();

        var memberships = await _userWorkspaceRepo.FindForUserAsync(userId);

        foreach (var m in memberships)
        {
            var ws = await _workspaceRepo.FindByIdAsync(m.WorkspaceId);
            if (ws == null) continue;
            Workspaces.Add(new WorkspaceView
            {
                Id = ws.Id,
                Name = ws.Name,
                Slug = ws.Slug,
                Accepted = m.Accepted
            });
        }

        return Page();
    }
}

public class WorkspaceView
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool Accepted { get; set; }
}
