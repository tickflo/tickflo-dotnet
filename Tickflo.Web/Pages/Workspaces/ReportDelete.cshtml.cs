namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;

using Tickflo.Core.Services.Views;

[Authorize]
public class ReportDeleteModel(
    IWorkspaceRepository workspaceRepo,
    IReportRepository reporyRepository,
    IReportRunRepository reportRunRepo,
    IWorkspaceReportDeleteViewService deleteViewService,
    IWebHostEnvironment env) : WorkspacePageModel
{
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepo;
    private readonly IReportRepository reporyRepository = reporyRepository;
    private readonly IReportRunRepository _reportRunRepo = reportRunRepo;
    private readonly IWorkspaceReportDeleteViewService _deleteViewService = deleteViewService;
    private readonly IWebHostEnvironment _env = env;

    public async Task<IActionResult> OnPostAsync(string slug, int reportId)
    {
        var ws = await this.workspaceRepository.FindBySlugAsync(slug);
        if (ws == null)
        {
            return this.NotFound();
        }

        var uid = this.TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0)
        {
            return this.Forbid();
        }

        var data = await this._deleteViewService.BuildAsync(ws.Id, uid);
        if (this.EnsurePermissionOrForbid(data.CanEditReports) is IActionResult permCheck)
        {
            return permCheck;
        }

        try
        {
            var runs = await this._reportRunRepo.ListForReportAsync(ws.Id, reportId, take: 500000);
            foreach (var rr in runs)
            {
                if (!string.IsNullOrWhiteSpace(rr.FilePath))
                {
                    var full = Path.Combine(this._env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), rr.FilePath);
                    if (System.IO.File.Exists(full))
                    {
                        System.IO.File.Delete(full);
                    }
                }
            }
        }
        catch { /* ignore cleanup errors */ }

        await this._reportRunRepo.DeleteForReportAsync(ws.Id, reportId);
        var ok = await this.reporyRepository.DeleteAsync(ws.Id, reportId);
        if (ok)
        {
            this.TempData["Success"] = "Report deleted.";
        }
        else
        {
            this.SetSuccessMessage("Report not found.");
        }
        return this.RedirectToPage("/Workspaces/Reports", new { slug });
    }

}

