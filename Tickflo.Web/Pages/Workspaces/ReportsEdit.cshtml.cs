namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Views;

[Authorize]
public class ReportsEditModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepository, IReportCommandService reportCommandService, IReportDefinitionValidator defValidator, IWorkspaceReportsEditViewService reportsEditViewService) : WorkspacePageModel
{
    #region Constants
    private const int NewReportId = 0;
    private const string DefaultReportSource = "tickets";
    private const string DefaultFieldsCsv = "Id,Subject,Status,CreatedAt";
    private const string DefaultScheduleType = "none";
    private const string ReportCreatedSuccessfully = "Report '{0}' created successfully.";
    private const string ReportUpdatedSuccessfully = "Report '{0}' updated successfully.";
    #endregion

    private readonly IWorkspaceRepository workspaceRepository = workspaceRepo;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IReportCommandService _reportCommandService = reportCommandService;
    private readonly IReportDefinitionValidator _defValidator = defValidator;
    private readonly IWorkspaceReportsEditViewService _reportsEditViewService = reportsEditViewService;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    [BindProperty]
    public int ReportId { get; set; }
    [BindProperty]
    public string Name { get; set; } = string.Empty;
    [BindProperty]
    public bool Ready { get; set; }
    public bool CanViewReports { get; private set; }
    public bool CanEditReports { get; private set; }
    public bool CanCreateReports { get; private set; }

    [BindProperty]
    public string Source { get; set; } = "tickets";
    [BindProperty]
    public string FieldsCsv { get; set; } = string.Empty; // comma-separated
    [BindProperty]
    public string? FiltersJson { get; set; }

    [BindProperty]
    public bool ScheduleEnabled { get; set; }
    [BindProperty]
    public string ScheduleType { get; set; } = "none"; // none|daily|weekly|monthly
    [BindProperty]
    public string? ScheduleTime { get; set; } // HH:mm
    [BindProperty]
    public int? ScheduleDayOfWeek { get; set; }
    [BindProperty]
    public int? ScheduleDayOfMonth { get; set; }

    public IReadOnlyDictionary<string, string[]> Sources { get; private set; } = new Dictionary<string, string[]>();

    public async Task<IActionResult> OnGetAsync(string slug, int reportId = 0)
    {
        this.WorkspaceSlug = slug;
        var workspaceLoadResult = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (workspaceLoadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)workspaceLoadResult;
        this.Workspace = workspace;
        var workspaceId = this.Workspace!.Id;
        var data = await this._reportsEditViewService.BuildAsync(workspaceId, uid, reportId);
        this.CanViewReports = data.CanViewReports;
        this.CanEditReports = data.CanEditReports;
        this.CanCreateReports = data.CanCreateReports;
        this.Sources = data.Sources;
        if (this.EnsurePermissionOrForbid(this.CanViewReports) is IActionResult permCheck)
        {
            return permCheck;
        }

        if (reportId > NewReportId)
        {
            if (this.LoadExistingReportData(data) is IActionResult result)
            {
                return result;
            }
        }
        else
        {
            this.InitializeNewReportForm();
        }
        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        var loadResult = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (loadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)loadResult;
        this.Workspace = workspace;
        var workspaceId = workspace!.Id;
        var data = await this._reportsEditViewService.BuildAsync(workspaceId, uid, this.ReportId);

        if (!this.ValidateReportPermissions(data))
        {
            return this.Forbid();
        }

        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        var nameTrim = this.Name?.Trim() ?? string.Empty;
        var defJson = this._defValidator.BuildJson(this.Source, this.FieldsCsv, this.FiltersJson);
        var schedTime = this.ParseScheduleTime();

        if (this.ReportId == NewReportId)
        {
            await this.CreateReportAsync(workspaceId, nameTrim, defJson, schedTime);
        }
        else
        {
            if (await this.UpdateReportAsync(workspaceId, nameTrim, defJson, schedTime) is IActionResult result)
            {
                return result;
            }
        }

        return this.RedirectToReportsWithPreservedFilters(slug);
    }

    private IActionResult? LoadExistingReportData(WorkspaceReportsEditViewData data)
    {
        var rep = data.ExistingReport;
        if (this.EnsureEntityExistsOrNotFound(rep) is IActionResult result)
        {
            return result;
        }

        this.ReportId = rep!.Id;
        this.Name = rep.Name;
        this.Ready = rep.Ready;

        var def = this._defValidator.Parse(rep.DefinitionJson);
        this.Source = def.Source;
        this.FieldsCsv = string.Join(",", def.Fields);
        this.FiltersJson = def.FiltersJson;

        this.ScheduleEnabled = rep.ScheduleEnabled;
        this.ScheduleType = rep.ScheduleType ?? DefaultScheduleType;
        this.ScheduleTime = rep.ScheduleTime.HasValue ? new DateTime(rep.ScheduleTime.Value.Ticks).ToString("HH:mm") : null;
        this.ScheduleDayOfWeek = rep.ScheduleDayOfWeek;
        this.ScheduleDayOfMonth = rep.ScheduleDayOfMonth;

        return null;
    }

    private void InitializeNewReportForm()
    {
        this.ReportId = NewReportId;
        this.Name = string.Empty;
        this.Ready = false;
        this.Source = DefaultReportSource;
        this.FieldsCsv = DefaultFieldsCsv;
        this.FiltersJson = string.Empty;
        this.ScheduleEnabled = false;
        this.ScheduleType = DefaultScheduleType;
        this.ScheduleTime = null;
        this.ScheduleDayOfWeek = null;
        this.ScheduleDayOfMonth = null;
    }

    private bool ValidateReportPermissions(WorkspaceReportsEditViewData data) => (this.ReportId == NewReportId) ? data.CanCreateReports : data.CanEditReports;

    private TimeSpan? ParseScheduleTime()
    {
        if (!string.IsNullOrWhiteSpace(this.ScheduleTime) && TimeSpan.TryParse(this.ScheduleTime, out var ts))
        {
            return ts;
        }

        return null;
    }

    private async Task CreateReportAsync(int workspaceId, string name, string definitionJson, TimeSpan? scheduleTime)
    {
        await this._reportCommandService.CreateAsync(new Report
        {
            WorkspaceId = workspaceId,
            Name = name,
            Ready = this.Ready,
            DefinitionJson = definitionJson,
            ScheduleEnabled = this.ScheduleEnabled,
            ScheduleType = this.ScheduleType,
            ScheduleTime = scheduleTime,
            ScheduleDayOfWeek = this.ScheduleDayOfWeek,
            ScheduleDayOfMonth = this.ScheduleDayOfMonth
        });
        this.SetSuccessMessage(string.Format(ReportCreatedSuccessfully, this.Name));
    }

    private async Task<IActionResult?> UpdateReportAsync(int workspaceId, string name, string definitionJson, TimeSpan? scheduleTime)
    {
        var updated = await this._reportCommandService.UpdateAsync(new Report
        {
            Id = this.ReportId,
            WorkspaceId = workspaceId,
            Name = name,
            Ready = this.Ready,
            DefinitionJson = definitionJson,
            ScheduleEnabled = this.ScheduleEnabled,
            ScheduleType = this.ScheduleType,
            ScheduleTime = scheduleTime,
            ScheduleDayOfWeek = this.ScheduleDayOfWeek,
            ScheduleDayOfMonth = this.ScheduleDayOfMonth
        });

        if (this.EnsureEntityExistsOrNotFound(updated) is IActionResult result)
        {
            return result;
        }

        this.SetSuccessMessage(string.Format(ReportUpdatedSuccessfully, this.Name));
        return null;
    }

    private IActionResult RedirectToReportsWithPreservedFilters(string slug)
    {
        var queryQ = this.Request.Query["Query"].ToString();
        var pageQ = this.Request.Query["PageNumber"].ToString();
        return this.RedirectToPage("/Workspaces/Reports", new { slug, Query = queryQ, PageNumber = pageQ });
    }


}


