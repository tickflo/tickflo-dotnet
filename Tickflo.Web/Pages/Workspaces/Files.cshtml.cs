using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class FilesModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IUserWorkspaceRepository _userWorkspaceRepository;

    public FilesModel(IWorkspaceRepository workspaceRepository, IUserWorkspaceRepository userWorkspaceRepository)
    {
        _workspaceRepository = workspaceRepository;
        _userWorkspaceRepository = userWorkspaceRepository;
    }

    public Workspace? Workspace { get; set; }
    public int WorkspaceId { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var uidStr = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();

        var ws = await _workspaceRepository.FindBySlugAsync(slug);
        if (ws == null) return NotFound();

        // Check if user has access to this workspace
        var userWorkspace = await _userWorkspaceRepository.FindAsync(uid, ws.Id);
        if (userWorkspace == null) return Forbid();

        Workspace = ws;
        WorkspaceId = ws.Id;
        return Page();
    }
}
