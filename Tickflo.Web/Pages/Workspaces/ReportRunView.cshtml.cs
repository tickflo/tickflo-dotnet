namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class ReportRunViewModel(IWorkspaceService workspaceService, IWorkspaceReportRunViewService workspaceReportRunViewService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IWorkspaceReportRunViewService workspaceReportRunViewService = workspaceReportRunViewService;

    [BindProperty(SupportsGet = true)]
    public string WorkspaceSlug { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public int ReportId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int RunId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Take { get; set; } = 500; // default display limit
    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1; // 1-based page index, hides PageModel.Page()

    public Workspace? Workspace { get; set; }
    public Report? Report { get; set; }
    public ReportRun? Run { get; set; }

    public List<string> Headers { get; set; } = [];
    public List<List<string>> Rows { get; set; } = [];

    public int DisplayLimit { get; set; }
    public int TotalRows { get; set; }
    public int TotalPages { get; set; }
    public int FromRow { get; set; }
    public int ToRow { get; set; }
    public bool HasContent { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug, int reportId, int runId, int? take, int? page)
    {
        this.WorkspaceSlug = slug;
        this.ReportId = reportId;
        this.RunId = runId;
        if (take.HasValue && take.Value > 0)
        {
            this.Take = Math.Min(take.Value, 5000);
        }

        if (page.HasValue && page.Value > 0)
        {
            this.Page = page.Value;
        }

        this.DisplayLimit = this.Take;

        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        var uid = this.TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0)
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var data = await this.workspaceReportRunViewService.BuildAsync(this.Workspace.Id, uid, reportId, runId, this.Page, this.Take);
        if (this.EnsurePermissionOrForbid(data.CanViewReports) is IActionResult permCheck)
        {
            return permCheck;
        }

        if (data.Report == null || data.Run == null || data.PageData == null)
        {
            return this.NotFound();
        }

        this.Report = data.Report;
        this.Run = data.Run;
        var pageResult = data.PageData;
        this.Page = pageResult.Page;
        this.Take = pageResult.Take;
        this.DisplayLimit = pageResult.Take;
        this.TotalRows = pageResult.TotalRows;
        this.TotalPages = pageResult.TotalPages;
        this.FromRow = pageResult.FromRow;
        this.ToRow = pageResult.ToRow;
        this.HasContent = pageResult.HasContent;
        this.Headers = [.. pageResult.Headers];
        this.Rows = [.. pageResult.Rows.Select(r => r.ToList())];
        return this.Page();
    }
}

