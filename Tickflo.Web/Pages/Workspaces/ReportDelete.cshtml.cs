using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;

namespace Tickflo.Web.Pages.Workspaces;

public class ReportDeleteModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IReportRepository _reportRepo;
    private readonly IReportRunRepository _reportRunRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly IWebHostEnvironment _env;

    public ReportDeleteModel(IWorkspaceRepository workspaceRepo, IReportRepository reportRepo, IReportRunRepository reportRunRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IRolePermissionRepository rolePerms, IWebHostEnvironment env)
    {
        _workspaceRepo = workspaceRepo;
        _reportRepo = reportRepo;
        _reportRunRepo = reportRunRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
        _env = env;
    }

    public async Task<IActionResult> OnPostAsync(string slug, int reportId)
    {
        var ws = await _workspaceRepo.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        var uidStr = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, ws.Id);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(ws.Id, uid);
        var allowed = isAdmin || (eff.TryGetValue("reports", out var rp) && rp.CanEdit);
        if (!allowed) return Forbid();

        // Attempt legacy file cleanup for older runs (best-effort)
        try
        {
            var runs = await _reportRunRepo.ListForReportAsync(ws.Id, reportId, take: 500000);
            foreach (var rr in runs)
            {
                if (!string.IsNullOrWhiteSpace(rr.FilePath))
                {
                    var full = Path.Combine(_env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), rr.FilePath);
                    if (System.IO.File.Exists(full))
                    {
                        System.IO.File.Delete(full);
                    }
                }
            }
        }
        catch { /* ignore cleanup errors */ }

        // Delete runs then report
        await _reportRunRepo.DeleteForReportAsync(ws.Id, reportId);
        var ok = await _reportRepo.DeleteAsync(ws.Id, reportId);
        if (ok)
        {
            TempData["Success"] = "Report deleted.";
        }
        else
        {
            TempData["Success"] = "Report not found.";
        }
        return RedirectToPage("/Workspaces/Reports", new { slug });
    }
}
