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

    public WorkspaceModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepo, IHttpContextAccessor httpContextAccessor)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrEmpty(slug))
            return RedirectToPage("/Workspaces");

        var ws = await _workspaceRepo.FindBySlugAsync(slug);
        if (ws == null)
            return NotFound();

        Workspace = ws;

        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var userId))
            {
                var memberships = await _userWorkspaceRepo.FindForUserAsync(userId);
                IsMember = memberships.Any(m => m.WorkspaceId == ws.Id && m.Accepted);
            }
        }

        return Page();
    }
}
