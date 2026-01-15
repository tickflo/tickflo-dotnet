using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Views;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportDeleteModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IReportRepository _reportRepo;
    private readonly IReportRunRepository _reportRunRepo;
    private readonly IWorkspaceReportDeleteViewService _deleteViewService;
    private readonly IWebHostEnvironment _env;

    public ReportDeleteModel(
        IWorkspaceRepository workspaceRepo,
        IReportRepository reportRepo,
        IReportRunRepository reportRunRepo,
        IWorkspaceReportDeleteViewService deleteViewService,
        IWebHostEnvironment env)
    {
        _workspaceRepo = workspaceRepo;
        _reportRepo = reportRepo;
        _reportRunRepo = reportRunRepo;
        _deleteViewService = deleteViewService;
        _env = env;
    }

    public async Task<IActionResult> OnPostAsync(string slug, int reportId)
    {
        var ws = await _workspaceRepo.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        var uid = TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0) return Forbid();
        var data = await _deleteViewService.BuildAsync(ws.Id, uid);
        if (EnsurePermissionOrForbid(data.CanEditReports) is IActionResult permCheck) return permCheck;

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

        await _reportRunRepo.DeleteForReportAsync(ws.Id, reportId);
        var ok = await _reportRepo.DeleteAsync(ws.Id, reportId);
        if (ok)
        {
            TempData["Success"] = "Report deleted.";
        }
        else
        {
            SetSuccessMessage("Report not found.");
        }
        return RedirectToPage("/Workspaces/Reports", new { slug });
    }

}

