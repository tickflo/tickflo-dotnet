using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Views;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportsEditModel : WorkspacePageModel
{
    #region Constants
    private const int NewReportId = 0;
    private const string DefaultReportSource = "tickets";
    private const string DefaultFieldsCsv = "Id,Subject,Status,CreatedAt";
    private const string DefaultScheduleType = "none";
    private const string ReportCreatedSuccessfully = "Report '{0}' created successfully.";
    private const string ReportUpdatedSuccessfully = "Report '{0}' updated successfully.";
    #endregion

    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IReportCommandService _reportCommandService;
    private readonly IReportDefinitionValidator _defValidator;
    private readonly IWorkspaceReportsEditViewService _reportsEditViewService;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    [BindProperty]
    public int ReportId { get; set; }
    [BindProperty]
    public string Name { get; set; } = string.Empty;
    [BindProperty]
    public bool Ready { get; set; }

    public ReportsEditModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepo, IReportCommandService reportCommandService, IReportDefinitionValidator defValidator, IWorkspaceReportsEditViewService reportsEditViewService)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _reportCommandService = reportCommandService;
        _defValidator = defValidator;
        _reportsEditViewService = reportsEditViewService;
    }
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
        WorkspaceSlug = slug;
        var workspaceLoadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (workspaceLoadResult is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)workspaceLoadResult;
        Workspace = workspace;
        var workspaceId = Workspace!.Id;
        var data = await _reportsEditViewService.BuildAsync(workspaceId, uid, reportId);
        CanViewReports = data.CanViewReports;
        CanEditReports = data.CanEditReports;
        CanCreateReports = data.CanCreateReports;
        Sources = data.Sources;
        if (EnsurePermissionOrForbid(CanViewReports) is IActionResult permCheck) return permCheck;

        if (reportId > NewReportId)
        {
            if (LoadExistingReportData(data) is IActionResult result)
                return result;
        }
        else
        {
            InitializeNewReportForm();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        WorkspaceSlug = slug;
        
        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;
        var workspaceId = workspace!.Id;
        var data = await _reportsEditViewService.BuildAsync(workspaceId, uid, ReportId);
        
        if (!ValidateReportPermissions(data)) return Forbid();
        if (!ModelState.IsValid) return Page();

        var nameTrim = Name?.Trim() ?? string.Empty;
        var defJson = _defValidator.BuildJson(Source, FieldsCsv, FiltersJson);
        var schedTime = ParseScheduleTime();

        if (ReportId == NewReportId)
            await CreateReportAsync(workspaceId, nameTrim, defJson, schedTime);
        else
        {
            if (await UpdateReportAsync(workspaceId, nameTrim, defJson, schedTime) is IActionResult result)
                return result;
        }
        
        return RedirectToReportsWithPreservedFilters(slug);
    }

    private IActionResult? LoadExistingReportData(WorkspaceReportsEditViewData data)
    {
        var rep = data.ExistingReport;
        if (EnsureEntityExistsOrNotFound(rep) is IActionResult result)
            return result;
        
        ReportId = rep!.Id;
        Name = rep.Name;
        Ready = rep.Ready;
        
        var def = _defValidator.Parse(rep.DefinitionJson);
        Source = def.Source;
        FieldsCsv = string.Join(",", def.Fields);
        FiltersJson = def.FiltersJson;
        
        ScheduleEnabled = rep.ScheduleEnabled;
        ScheduleType = rep.ScheduleType ?? DefaultScheduleType;
        ScheduleTime = rep.ScheduleTime.HasValue ? new DateTime(rep.ScheduleTime.Value.Ticks).ToString("HH:mm") : null;
        ScheduleDayOfWeek = rep.ScheduleDayOfWeek;
        ScheduleDayOfMonth = rep.ScheduleDayOfMonth;
        
        return null;
    }

    private void InitializeNewReportForm()
    {
        ReportId = NewReportId;
        Name = string.Empty;
        Ready = false;
        Source = DefaultReportSource;
        FieldsCsv = DefaultFieldsCsv;
        FiltersJson = string.Empty;
        ScheduleEnabled = false;
        ScheduleType = DefaultScheduleType;
        ScheduleTime = null;
        ScheduleDayOfWeek = null;
        ScheduleDayOfMonth = null;
    }

    private bool ValidateReportPermissions(WorkspaceReportsEditViewData data)
    {
        return (ReportId == NewReportId) ? data.CanCreateReports : data.CanEditReports;
    }

    private TimeSpan? ParseScheduleTime()
    {
        if (!string.IsNullOrWhiteSpace(ScheduleTime) && TimeSpan.TryParse(ScheduleTime, out var ts))
            return ts;
        return null;
    }

    private async Task CreateReportAsync(int workspaceId, string name, string definitionJson, TimeSpan? scheduleTime)
    {
        await _reportCommandService.CreateAsync(new Report
        {
            WorkspaceId = workspaceId,
            Name = name,
            Ready = Ready,
            DefinitionJson = definitionJson,
            ScheduleEnabled = ScheduleEnabled,
            ScheduleType = ScheduleType,
            ScheduleTime = scheduleTime,
            ScheduleDayOfWeek = ScheduleDayOfWeek,
            ScheduleDayOfMonth = ScheduleDayOfMonth
        });
        SetSuccessMessage(string.Format(ReportCreatedSuccessfully, Name));
    }

    private async Task<IActionResult?> UpdateReportAsync(int workspaceId, string name, string definitionJson, TimeSpan? scheduleTime)
    {
        var updated = await _reportCommandService.UpdateAsync(new Report
        {
            Id = ReportId,
            WorkspaceId = workspaceId,
            Name = name,
            Ready = Ready,
            DefinitionJson = definitionJson,
            ScheduleEnabled = ScheduleEnabled,
            ScheduleType = ScheduleType,
            ScheduleTime = scheduleTime,
            ScheduleDayOfWeek = ScheduleDayOfWeek,
            ScheduleDayOfMonth = ScheduleDayOfMonth
        });
        
        if (EnsureEntityExistsOrNotFound(updated) is IActionResult result)
            return result;
        
        SetSuccessMessage(string.Format(ReportUpdatedSuccessfully, Name));
        return null;
    }

    private IActionResult RedirectToReportsWithPreservedFilters(string slug)
    {
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return RedirectToPage("/Workspaces/Reports", new { slug, Query = queryQ, PageNumber = pageQ });
    }


}


