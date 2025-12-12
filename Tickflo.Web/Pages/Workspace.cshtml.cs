using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Tickflo.Web.Pages;

public class WorkspaceModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Workspace? Workspace { get; set; }
    public bool IsMember { get; set; }
    public List<WorkspaceView> Workspaces { get; set; } = new();

    public WorkspaceModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepo, IHttpContextAccessor httpContextAccessor)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> OnGetAsync(string? slug)
    {
        if (string.IsNullOrEmpty(slug))
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

        var found = await _workspaceRepo.FindBySlugAsync(slug);
        if (found == null)
            return NotFound();

        Workspace = found;

        var curUser = _httpContextAccessor.HttpContext?.User;
        if (curUser?.Identity?.IsAuthenticated == true)
        {
            var idClaim = curUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var userId))
            {
                var memberships = await _userWorkspaceRepo.FindForUserAsync(userId);
                foreach (var m in memberships)
                {
                    var w = await _workspaceRepo.FindByIdAsync(m.WorkspaceId);
                    if (w == null) continue;
                    Workspaces.Add(new WorkspaceView
                    {
                        Id = w.Id,
                        Name = w.Name,
                        Slug = w.Slug,
                        Accepted = m.Accepted
                    });
                }

                IsMember = memberships.Any(m => m.WorkspaceId == found.Id && m.Accepted);
            }
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

