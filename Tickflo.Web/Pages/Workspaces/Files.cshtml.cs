using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class FilesModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceFilesViewService _filesViewService;

    public FilesModel(
        IWorkspaceRepository workspaceRepository,
        IWorkspaceFilesViewService filesViewService)
    {
        _workspaceRepository = workspaceRepository;
        _filesViewService = filesViewService;
    }

    public Workspace? Workspace { get; set; }
    public int WorkspaceId { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var uid = TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0) return Forbid();

        var ws = await _workspaceRepository.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        var data = await _filesViewService.BuildAsync(ws.Id, uid);
        if (!data.CanViewFiles) return Forbid();

        Workspace = ws;
        WorkspaceId = ws.Id;
        return Page();
    }

    private bool TryGetUserId(out int userId)
    {
        var idValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idValue, out userId))
        {
            return true;
        }
        userId = default;
        return false;
    }
}
