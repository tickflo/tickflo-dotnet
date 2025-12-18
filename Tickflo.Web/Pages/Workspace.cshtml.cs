using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;

namespace Tickflo.Web.Pages;


public class WorkspaceModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly IUserRepository _userRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly ITicketPriorityRepository _priorityRepo;

    public Workspace? Workspace { get; set; }
    public bool IsMember { get; set; }
    public List<WorkspaceView> Workspaces { get; set; } = new();

    // Dashboard metrics
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

    // Priority counts
    public Dictionary<string, int> PriorityCounts { get; set; } = new();
    public List<TicketPriority> PriorityList { get; set; } = new();

    public WorkspaceModel(
        IWorkspaceRepository workspaceRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        IHttpContextAccessor httpContextAccessor,
        ITicketRepository ticketRepo,
        ITicketStatusRepository statusRepo,
        IUserRepository userRepo,
        ITicketPriorityRepository priorityRepo,
        ITeamRepository teamRepo)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _httpContextAccessor = httpContextAccessor;
        _ticketRepo = ticketRepo;
        _statusRepo = statusRepo;
        _userRepo = userRepo;
        _priorityRepo = priorityRepo;
        _teamRepo = teamRepo;
    }

    public async Task<IActionResult> OnGetAsync(string? slug, int? range, string? assignment)
    {
        RangeDays = NormalizeRange(range);
        AssignmentFilter = string.IsNullOrEmpty(assignment) ? "all" : assignment.ToLowerInvariant();
        if (string.IsNullOrEmpty(slug))
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return Challenge();

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Challenge();

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

        var curUser = _httpContextAccessor.HttpContext?.User;
        if (curUser?.Identity?.IsAuthenticated == true)
        {
            var idClaim = curUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var userId))
            {
                var memberships = await _userWorkspaceRepo.FindForUserAsync(userId);
                foreach (var m in memberships)
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

                IsMember = memberships.Any(m => m.WorkspaceId == found.Id && m.Accepted);
            }
        }

        if (Workspace != null && IsMember)
        {
            int? userId = null;
            var dashboardUser = _httpContextAccessor.HttpContext?.User;
            if (dashboardUser?.Identity?.IsAuthenticated == true)
            {
                var idClaim = dashboardUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out var uid))
                    userId = uid;
            }
            await LoadDashboardDataAsync(Workspace.Id, RangeDays, AssignmentFilter, userId);
        }

        return Page();
    }

    private int NormalizeRange(int? range)
    {
        return range is 7 or 30 or 90 ? range.Value : 90;
    }


    private async Task LoadDashboardDataAsync(int workspaceId, int rangeDays, string assignmentFilter, int? userId)
    {
        // Load all members and teams for assignment filter
        var acceptedUserIds = (await _userWorkspaceRepo.FindForWorkspaceAsync(workspaceId))
            .Where(m => m.Accepted)
            .Select(m => m.UserId)
            .Distinct()
            .ToList();
        WorkspaceMembers = new List<User>();
        foreach (var uid in acceptedUserIds)
        {
            var user = await _userRepo.FindByIdAsync(uid);
            if (user != null)
                WorkspaceMembers.Add(user);
        }
        WorkspaceTeams = await _teamRepo.ListForWorkspaceAsync(workspaceId);

        var tickets = await _ticketRepo.ListAsync(workspaceId);
        TotalTickets = tickets.Count;

        StatusList = (await _statusRepo.ListAsync(workspaceId)).ToList();
        var closedNames = new HashSet<string>(StatusList.Where(s => s.IsClosedState).Select(s => s.Name), System.StringComparer.OrdinalIgnoreCase);
        var statusColor = StatusList.GroupBy(s => s.Name, System.StringComparer.OrdinalIgnoreCase)
                  .ToDictionary(g => g.Key, g => g.First().Color, System.StringComparer.OrdinalIgnoreCase);

        ResolvedTickets = tickets.Count(t => closedNames.Contains(t.Status));
        OpenTickets = TotalTickets - ResolvedTickets;

        var memberships = await _userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
        ActiveMembers = memberships.Count(m => m.Accepted);

        // Get custom priorities for this workspace
        PriorityList = (await _priorityRepo.ListAsync(workspaceId)).ToList();
        // Priority counts (all tickets, not filtered, using custom priorities)
        PriorityCounts = PriorityList
            .ToDictionary(
                p => p.Name,
                p => tickets.Count(t => (t.Priority ?? "Normal") == p.Name)
            );
        // Add any tickets with priorities not in the custom list
        foreach (var t in tickets)
        {
            var p = string.IsNullOrWhiteSpace(t.Priority) ? "Normal" : t.Priority;
            if (!PriorityCounts.ContainsKey(p))
                PriorityCounts[p] = 1;
            else if (!PriorityList.Any(x => x.Name == p))
                PriorityCounts[p] += 1;
        }

        // Assignment filter logic
        IEnumerable<Tickflo.Core.Entities.Ticket> filtered = tickets;
        if (assignmentFilter == "unassigned")
        {
            filtered = filtered.Where(t => !t.AssignedUserId.HasValue);
        }
        else if (assignmentFilter == "me" && userId.HasValue)
        {
            filtered = filtered.Where(t => t.AssignedUserId == userId.Value);
        }
        else if (assignmentFilter == "others" && userId.HasValue)
        {
            filtered = filtered.Where(t => t.AssignedUserId.HasValue && t.AssignedUserId != userId.Value);
        }
        // else "all" (no filter)

        // Recent tickets list (latest 8)
        var recent = filtered
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .Take(8)
            .ToList();

        // Map assignee names in one pass
        var assigneeIds = recent.Where(t => t.AssignedUserId.HasValue).Select(t => t.AssignedUserId!.Value).Distinct().ToList();
        var assigneeNames = new Dictionary<int, string>();
        foreach (var uid in assigneeIds)
        {
            var u = await _userRepo.FindByIdAsync(uid);
            if (u != null) assigneeNames[uid] = u.Name;
        }

        RecentTickets = recent.Select(t => new TicketListItem
        {
            Id = t.Id,
            Subject = t.Subject,
            Type = t.Type,
            Status = t.Status,
            StatusColor = statusColor.TryGetValue(t.Status, out var c) ? c : "neutral",
            AssignedUserId = t.AssignedUserId,
            AssigneeName = t.AssignedUserId.HasValue && assigneeNames.TryGetValue(t.AssignedUserId.Value, out var n) ? n : null,
            UpdatedAt = t.UpdatedAt ?? t.CreatedAt
        }).ToList();

        // Top members by closed ticket count in selected range
        var cutoff = DateTime.UtcNow.AddDays(-rangeDays);
        var closedAssigned = tickets
            .Where(t => t.AssignedUserId.HasValue && closedNames.Contains(t.Status) && (t.UpdatedAt ?? t.CreatedAt) >= cutoff)
            .GroupBy(t => t.AssignedUserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        foreach (var item in closedAssigned)
        {
            var user = await _userRepo.FindByIdAsync(item.UserId);
            TopMembers.Add(new MemberStat
            {
                UserId = item.UserId,
                Name = user?.Name ?? $"User #{item.UserId}",
                ResolvedCount = item.Count
            });
        }

        // Average resolution time for closed tickets (selected range)
        var closedForAvg = tickets.Where(t => closedNames.Contains(t.Status) && (t.UpdatedAt ?? t.CreatedAt) >= cutoff && (t.UpdatedAt.HasValue)).ToList();
        if (closedForAvg.Count > 0)
        {
            var avgTicks = closedForAvg.Average(t => (t.UpdatedAt!.Value - t.CreatedAt).Ticks);
            AvgResolutionTime = TimeSpan.FromTicks(Convert.ToInt64(avgTicks));
            AvgResolutionLabel = FormatDuration(AvgResolutionTime.Value);
        }
        else
        {
            AvgResolutionTime = null;
            AvgResolutionLabel = "—";
        }
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

    // Close LoadDashboardDataAsync method
    }

    // Close LoadDashboardDataAsync method
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

