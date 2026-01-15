using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Config;
using Tickflo.Core.Services;
using System.Collections.Generic;
using System.Linq;

using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Views;
namespace Tickflo.Web.Pages;

[Authorize]
public class WorkspaceModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IWorkspaceDashboardViewService _dashboardViewService;
    private readonly ITeamMemberRepository _teamMembers;
    private readonly SettingsConfig _settingsConfig;
    private readonly ICurrentUserService _currentUserService;

    public Workspace? Workspace { get; set; }
    public bool IsMember { get; set; }
    public List<WorkspaceView> Workspaces { get; set; } = new();

    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ActiveMembers { get; set; }
    public TimeSpan? AvgResolutionTime { get; set; }
    public string AvgResolutionLabel { get; set; } = string.Empty;

    public List<TicketListItem> RecentTickets { get; set; } = new();
    public List<MemberStat> TopMembers { get; set; } = new();
    public int RangeDays { get; set; } = 90;
    public string AssignmentFilter { get; set; } = "all";
    public List<User> WorkspaceMembers { get; set; } = new();
    public List<Team> WorkspaceTeams { get; set; } = new();
    public List<TicketStatus> StatusList { get; set; } = new();
    public List<TicketType> TypeList { get; set; } = new();
    public bool CanViewDashboard { get; set; }
    public bool CanViewTickets { get; set; }
    public string TicketViewScope { get; set; } = string.Empty;

    public Dictionary<string, int> PriorityCounts { get; set; } = new();
    public List<TicketPriority> PriorityList { get; set; } = new();

    public List<ActivityPoint> ActivitySeries { get; set; } = new();

    public string DashboardTheme { get; set; } = "light";
    public string PrimaryColor { get; set; } = "primary";
    public string SuccessColor { get; set; } = "success";
    public string InfoColor { get; set; } = "info";
    public string WarningColor { get; set; } = "warning";
    public string ErrorColor { get; set; } = "error";
    
    public bool PrimaryIsHex { get; set; }
    public bool SuccessIsHex { get; set; }
    public bool InfoIsHex { get; set; }
    public bool WarningIsHex { get; set; }
    public bool ErrorIsHex { get; set; }

    public WorkspaceModel(
        IWorkspaceRepository workspaceRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        IWorkspaceDashboardViewService dashboardViewService,
        ITeamMemberRepository teamMembers,
        SettingsConfig settingsConfig,
        ICurrentUserService currentUserService)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _dashboardViewService = dashboardViewService;
        _teamMembers = teamMembers;
        _settingsConfig = settingsConfig;
        _currentUserService = currentUserService;

        InitializeTheme();
    }

    private void InitializeTheme()
    {
        DashboardTheme = _settingsConfig?.Theme?.Default ?? "light";
        PrimaryColor = "primary";
        SuccessColor = "success";
        InfoColor = "info";
        WarningColor = "warning";
        ErrorColor = "error";
    }

    public async Task<IActionResult> OnGetAsync(string? slug, int? range, string? assignment)
    {
        RangeDays = NormalizeRange(range);
        AssignmentFilter = string.IsNullOrEmpty(assignment) ? "all" : assignment.ToLowerInvariant();
        
        if (!_currentUserService.TryGetUserId(User, out var userId))
            return Challenge();

        if (string.IsNullOrEmpty(slug))
        {
            var memberships = await _userWorkspaceRepo.FindForUserAsync(userId);

            foreach (var m in memberships)
            {
                var ws = await _workspaceRepo.FindByIdAsync(m.WorkspaceId);
                if (ws == null) continue;
                Workspaces.Add(new WorkspaceView
                {
                    Id = ws.Id,
                    Name = ws.Name,
                    Slug = ws.Slug,
                    Accepted = m.Accepted
                });
            }

            return Page();
        }

        var found = await _workspaceRepo.FindBySlugAsync(slug);
        if (found == null)
            return NotFound();

        Workspace = found;

        var userMemberships = await _userWorkspaceRepo.FindForUserAsync(userId);
        foreach (var m in userMemberships)
        {
            var w = await _workspaceRepo.FindByIdAsync(m.WorkspaceId);
            if (w == null) continue;
            Workspaces.Add(new WorkspaceView
            {
                Id = w.Id,
                Name = w.Name,
                Slug = w.Slug,
                Accepted = m.Accepted
            });
        }

        IsMember = userMemberships.Any(m => m.WorkspaceId == found.Id && m.Accepted);

        if (Workspace != null && IsMember)
        {
            await LoadDashboardDataAsync(Workspace.Id, RangeDays, AssignmentFilter, userId);
            if (!CanViewDashboard)
            {
                return Forbid();
            }
        }

        return Page();
    }

    private int NormalizeRange(int? range)
    {
        return range is 7 or 30 or 90 ? range.Value : 90;
    }


    private async Task LoadDashboardDataAsync(int workspaceId, int rangeDays, string assignmentFilter, int userId)
    {
        // First pass: get scope to determine team IDs (use placeholder scope initially)
        var view = await _dashboardViewService.BuildAsync(workspaceId, userId, "all", new List<int>(), rangeDays, assignmentFilter);

        // Extract permissions from view
        CanViewDashboard = view.CanViewDashboard;
        CanViewTickets = view.CanViewTickets;
        TicketViewScope = view.TicketViewScope;

        // If scope is team, rebuild with actual team IDs
        if (view.TicketViewScope == "team")
        {
            var myTeams = await _teamMembers.ListTeamsForUserAsync(workspaceId, userId);
            var teamIds = myTeams.Select(t => t.Id).ToList();
            view = await _dashboardViewService.BuildAsync(workspaceId, userId, view.TicketViewScope, teamIds, rangeDays, assignmentFilter);
        }
        else if (view.TicketViewScope != "all")
        {
            // Re-fetch with correct scope if not "all"
            view = await _dashboardViewService.BuildAsync(workspaceId, userId, view.TicketViewScope, new List<int>(), rangeDays, assignmentFilter);
        }

        TotalTickets = view.TotalTickets;
        OpenTickets = view.OpenTickets;
        ResolvedTickets = view.ResolvedTickets;
        ActiveMembers = view.ActiveMembers;

        StatusList = view.StatusList.ToList();
        TypeList = view.TypeList.ToList();
        PriorityList = view.PriorityList.ToList();
        PriorityCounts = view.PriorityCounts.ToDictionary(k => k.Key, v => v.Value);

        PrimaryColor = view.PrimaryColor;
        PrimaryIsHex = view.PrimaryIsHex;
        SuccessColor = view.SuccessColor;
        SuccessIsHex = view.SuccessIsHex;

        WorkspaceMembers = view.WorkspaceMembers.ToList();
        WorkspaceTeams = view.WorkspaceTeams.ToList();

        ActivitySeries = view.ActivitySeries.Select(a => new ActivityPoint { Label = a.Label, Created = a.Created, Closed = a.Closed }).ToList();
        TopMembers = view.TopMembers.Select(m => new MemberStat { UserId = m.UserId, Name = m.Name, ResolvedCount = m.ResolvedCount }).ToList();

        AvgResolutionLabel = view.AvgResolutionLabel;
        AvgResolutionTime = null;

        RecentTickets = view.RecentTickets.Select(t => new TicketListItem
        {
            Id = t.Id,
            Subject = t.Subject,
            Type = t.Type,
            Status = t.Status,
            StatusColor = t.StatusColor,
            TypeColor = t.TypeColor,
            AssignedUserId = t.AssignedUserId,
            AssigneeName = t.AssigneeName,
            UpdatedAt = t.UpdatedAt
        }).ToList();
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}d {ts.Hours}h";
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}m";
        return $"{ts.Seconds}s";

    }
    
    public static string HexToRgba(string hex, double opacity)
    {
        if (string.IsNullOrWhiteSpace(hex) || !hex.StartsWith("#"))
            return hex;
        
        hex = hex.TrimStart('#');
        if (hex.Length == 3)
        {
            hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
        }
        
        if (hex.Length != 6)
            return hex;
        
        try
        {
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return $"rgba({r}, {g}, {b}, {opacity})";
        }
        catch
        {
            return hex;
        }
    }

}

// Close WorkspaceModel class
public class WorkspaceView
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool Accepted { get; set; }
}

public class TicketListItem
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "neutral";
    public string TypeColor { get; set; } = "neutral";
    public int? AssignedUserId { get; set; }
    public string? AssigneeName { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class MemberStat
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ResolvedCount { get; set; }
}

public class ActivityPoint
{
    public string Label { get; set; } = string.Empty;
    public int Created { get; set; }
    public int Closed { get; set; }
}


