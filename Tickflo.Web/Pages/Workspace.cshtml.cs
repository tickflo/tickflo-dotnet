namespace Tickflo.Web.Pages;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class WorkspaceModel : PageModel
{
    private readonly IWorkspaceRepository workspaceRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository;
    private readonly IUserRepository userRepository;
    private readonly IWorkspaceDashboardViewService workspaceDashboardViewService;
    private readonly ITeamMemberRepository teamMemberRepository;
    private readonly SettingsConfig settingsConfig;
    private readonly ICurrentUserService currentUserService;
    private readonly IWorkspaceCreationService workspaceCreationService;

    public Workspace? Workspace { get; set; }
    public bool IsMember { get; set; }
    public List<WorkspaceView> Workspaces { get; set; } = [];

    [BindProperty]
    public string NewWorkspaceName { get; set; } = string.Empty;

    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ActiveMembers { get; set; }
    public string AvgResolutionLabel { get; set; } = string.Empty;

    public List<TicketListItem> RecentTickets { get; set; } = [];
    public List<MemberStat> TopMembers { get; set; } = [];
    public int RangeDays { get; set; } = 90;
    public string AssignmentFilter { get; set; } = "all";
    public List<User> WorkspaceMembers { get; set; } = [];
    public List<Team> WorkspaceTeams { get; set; } = [];
    public List<TicketStatus> StatusList { get; set; } = [];
    public List<TicketType> TypeList { get; set; } = [];
    public bool CanViewDashboard { get; set; }
    public bool CanViewTickets { get; set; }
    public string TicketViewScope { get; set; } = string.Empty;
    public bool ShowEmailConfirmationPrompt { get; set; }

    public Dictionary<string, int> PriorityCounts { get; set; } = [];
    public List<TicketPriority> PriorityList { get; set; } = [];

    public List<ActivityPoint> ActivitySeries { get; set; } = [];

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
        IWorkspaceRepository workspaceRepository,
        IUserWorkspaceRepository userWorkspaceRepository,
        IUserRepository users,
        IWorkspaceDashboardViewService dashboardViewService,
        ITeamMemberRepository teamMembers,
        SettingsConfig settingsConfig,
        ICurrentUserService currentUserService,
        IWorkspaceCreationService workspaceCreationService)
    {
        this.workspaceRepository = workspaceRepository;
        this.userWorkspaceRepository = userWorkspaceRepository;
        this.userRepository = users;
        this.workspaceDashboardViewService = dashboardViewService;
        this.teamMemberRepository = teamMembers;
        this.settingsConfig = settingsConfig;
        this.currentUserService = currentUserService;
        this.workspaceCreationService = workspaceCreationService;

        this.InitializeTheme();
    }

    private void InitializeTheme()
    {
        this.DashboardTheme = this.settingsConfig?.Theme?.Default ?? "light";
        this.PrimaryColor = "primary";
        this.SuccessColor = "success";
        this.InfoColor = "info";
        this.WarningColor = "warning";
        this.ErrorColor = "error";
    }

    public async Task<IActionResult> OnGetAsync(string? slug, int? range, string? assignment)
    {
        this.RangeDays = NormalizeRange(range);
        this.AssignmentFilter = NormalizeAssignment(assignment);

        var authContext = await this.GetAuthenticatedUserAsync();
        if (authContext is null)
        {
            return this.Challenge();
        }

        var (userId, user) = authContext.Value;
        this.ShowEmailConfirmationPrompt = !user.EmailConfirmed;

        if (string.IsNullOrEmpty(slug))
        {
            await this.LoadUserWorkspacesAsync(userId, null);
            return this.Page();
        }

        var found = await this.workspaceRepository.FindBySlugAsync(slug);
        if (found == null)
        {
            return this.NotFound();
        }

        this.Workspace = found;

        var userMemberships = await this.userWorkspaceRepository.FindForUserAsync(userId);
        await this.LoadUserWorkspacesAsync(userId, userMemberships);

        this.IsMember = userMemberships.Any(m => m.WorkspaceId == found.Id && m.Accepted);

        if (this.Workspace != null && this.IsMember)
        {
            await this.LoadDashboardDataAsync(this.Workspace.Id, this.RangeDays, this.AssignmentFilter, userId);
            if (!this.CanViewDashboard)
            {
                return this.Forbid();
            }
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var authContext = await this.GetAuthenticatedUserAsync();
        if (authContext is null)
        {
            return this.Challenge();
        }

        var (userId, user) = authContext.Value;
        this.ShowEmailConfirmationPrompt = !user.EmailConfirmed;

        var trimmedName = this.NewWorkspaceName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            this.ModelState.AddModelError(nameof(this.NewWorkspaceName), "Workspace name is required");
            await this.LoadUserWorkspacesAsync(userId, null);
            return this.Page();
        }

        try
        {
            var workspace = await this.workspaceCreationService.CreateWorkspaceAsync(
                new WorkspaceCreationRequest { Name = trimmedName },
                userId);

            return this.Redirect($"/workspaces/{workspace.Slug}");
        }
        catch (InvalidOperationException ex)
        {
            this.ModelState.AddModelError(nameof(this.NewWorkspaceName), ex.Message);
        }

        await this.LoadUserWorkspacesAsync(userId, null);
        return this.Page();
    }

    private static int NormalizeRange(int? range) => range is 7 or 30 or 90 ? range.Value : 90;

    private static string NormalizeAssignment(string? assignment)
    {
        if (string.IsNullOrWhiteSpace(assignment))
        {
            return "all";
        }

        return assignment.ToLowerInvariant();
    }

    private async Task<(int UserId, User User)?> GetAuthenticatedUserAsync()
    {
        if (!this.currentUserService.TryGetUserId(this.User, out var userId))
        {
            return null;
        }

        var user = await this.userRepository.FindByIdAsync(userId);
        return user == null ? null : (userId, user);
    }

    private async Task LoadUserWorkspacesAsync(int userId, List<UserWorkspace>? memberships)
    {
        this.Workspaces.Clear();
        var membershipList = memberships ?? await this.userWorkspaceRepository.FindForUserAsync(userId);

        foreach (var membership in membershipList)
        {
            var workspace = await this.workspaceRepository.FindByIdAsync(membership.WorkspaceId);
            if (workspace == null)
            {
                continue;
            }

            this.Workspaces.Add(new WorkspaceView
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Slug = workspace.Slug,
                Accepted = membership.Accepted
            });
        }
    }


    // TODO: Wtf is this even doing? Why on earth would this stupid service be called 3 times and have its own results passed back into it?
    private async Task LoadDashboardDataAsync(int workspaceId, int rangeDays, string assignmentFilter, int userId)
    {
        // First pass: get scope to determine team IDs (use placeholder scope initially)
        var view = await this.workspaceDashboardViewService.BuildAsync(workspaceId, userId, "all", [], rangeDays, assignmentFilter);

        // Extract permissions from view
        this.CanViewDashboard = view.CanViewDashboard;
        this.CanViewTickets = view.CanViewTickets;
        this.TicketViewScope = view.TicketViewScope;

        // If scope is team, rebuild with actual team IDs
        if (view.TicketViewScope == "team")
        {
            var myTeams = await this.teamMemberRepository.ListTeamsForUserAsync(workspaceId, userId);
            var teamIds = myTeams.Select(t => t.Id).ToList();
            view = await this.workspaceDashboardViewService.BuildAsync(workspaceId, userId, view.TicketViewScope, teamIds, rangeDays, assignmentFilter);
        }
        else if (view.TicketViewScope != "all")
        {
            // Re-fetch with correct scope if not "all"
            view = await this.workspaceDashboardViewService.BuildAsync(workspaceId, userId, view.TicketViewScope, [], rangeDays, assignmentFilter);
        }

        this.TotalTickets = view.TotalTickets;
        this.OpenTickets = view.OpenTickets;
        this.ResolvedTickets = view.ResolvedTickets;
        this.ActiveMembers = view.ActiveMembers;

        this.StatusList = [.. view.StatusList];
        this.TypeList = [.. view.TypeList];
        this.PriorityList = [.. view.PriorityList];
        this.PriorityCounts = view.PriorityCounts.ToDictionary(k => k.Key, v => v.Value);

        this.PrimaryColor = view.PrimaryColor;
        this.PrimaryIsHex = view.PrimaryIsHex;
        this.SuccessColor = view.SuccessColor;
        this.SuccessIsHex = view.SuccessIsHex;

        this.WorkspaceMembers = [.. view.WorkspaceMembers];
        this.WorkspaceTeams = [.. view.WorkspaceTeams];

        this.ActivitySeries = [.. view.ActivitySeries.Select(a => new ActivityPoint { Label = a.Label, Created = a.Created, Closed = a.Closed })];
        this.TopMembers = [.. view.TopMembers.Select(member => new MemberStat { UserId = member.UserId, Name = member.Name, ResolvedCount = member.ResolvedCount })];

        this.AvgResolutionLabel = view.AvgResolutionLabel;

        this.RecentTickets = [.. view.RecentTickets.Select(ticket => new TicketListItem
        {
            Id = ticket.Id,
            Subject = ticket.Subject,
            Type = ticket.Type,
            Status = ticket.Status,
            StatusColor = ticket.StatusColor,
            TypeColor = ticket.TypeColor,
            AssignedUserId = ticket.AssignedUserId,
            AssigneeName = ticket.AssigneeName,
            UpdatedAt = ticket.UpdatedAt
        })];
    }

    public static string HexToRgba(string hex, double opacity)
    {
        if (string.IsNullOrWhiteSpace(hex) || !hex.StartsWith('#'))
        {
            return hex;
        }

        hex = hex.TrimStart('#');
        if (hex.Length == 3)
        {
            hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
        }

        if (hex.Length != 6)
        {
            return hex;
        }

        try
        {
            var red = Convert.ToInt32(hex[..2], 16);
            var green = Convert.ToInt32(hex.Substring(2, 2), 16);
            var blue = Convert.ToInt32(hex.Substring(4, 2), 16);
            return $"rgba({red}, {green}, {blue}, {opacity})";
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

