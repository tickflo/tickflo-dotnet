namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Views;

[Authorize]
public class ReportsModel(
    IWorkspaceRepository workspaceRepo,
    IUserWorkspaceRepository userWorkspaceRepository,
    ICurrentUserService currentUserService,
    IWorkspaceReportsViewService viewService) : WorkspacePageModel
{
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepo;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IWorkspaceReportsViewService _viewService = viewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<ReportSummary> Reports { get; private set; } = [];
    public bool CanCreateReports { get; private set; }
    public bool CanEditReports { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        var result = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (result is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        this.Workspace = workspace;

        var viewData = await this._viewService.BuildAsync(this.Workspace!.Id, uid);
        this.Reports = viewData.Reports;
        this.CanCreateReports = viewData.CanCreateReports;
        this.CanEditReports = viewData.CanEditReports;

        return this.Page();
    }
}

