namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

using Tickflo.Core.Services.Workspace;
public class WorkspaceSettingsViewData
{
    public bool CanViewSettings { get; set; }
    public bool CanEditSettings { get; set; }
    public bool CanCreateSettings { get; set; }

    public IReadOnlyList<Entities.TicketStatus> Statuses { get; set; } = [];
    public IReadOnlyList<Entities.TicketPriority> Priorities { get; set; } = [];
    public IReadOnlyList<Entities.TicketType> Types { get; set; } = [];

    public bool NotificationsEnabled { get; set; } = true;
    public bool EmailIntegrationEnabled { get; set; } = true;
    public string EmailProvider { get; set; } = "smtp";
    public bool SmsIntegrationEnabled { get; set; }
    public string SmsProvider { get; set; } = "none";
    public bool PushIntegrationEnabled { get; set; }
    public string PushProvider { get; set; } = "none";
    public bool InAppNotificationsEnabled { get; set; } = true;
    public int BatchNotificationDelay { get; set; } = 30;
    public int DailySummaryHour { get; set; } = 9;
    public bool MentionNotificationsUrgent { get; set; } = true;
    public bool TicketAssignmentNotificationsHigh { get; set; } = true;
}

public interface IWorkspaceSettingsViewService
{
    public Task<WorkspaceSettingsViewData> BuildAsync(int workspaceId, int userId);
}


public class WorkspaceSettingsViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository,
    ITicketStatusRepository statusRepository,
    ITicketPriorityRepository priorityRepository,
    ITicketTypeRepository ticketTypeRepository,
    IWorkspaceSettingsService workspaceSettingsService) : IWorkspaceSettingsViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly ITicketStatusRepository statusRepository = statusRepository;
    private readonly ITicketPriorityRepository priorityRepository = priorityRepository;
    private readonly ITicketTypeRepository ticketTypeRepository = ticketTypeRepository;
    private readonly IWorkspaceSettingsService workspaceSettingsService = workspaceSettingsService;

    public async Task<WorkspaceSettingsViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceSettingsViewData();

        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        if (isAdmin)
        {
            data.CanViewSettings = data.CanEditSettings = data.CanCreateSettings = true;
        }
        else
        {
            var perms = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);
            if (perms.TryGetValue("settings", out var eff))
            {
                data.CanViewSettings = eff.CanView;
                data.CanEditSettings = eff.CanEdit;
                data.CanCreateSettings = eff.CanCreate;
            }
        }

        // Ensure defaults and load lists
        await this.workspaceSettingsService.EnsureDefaultsExistAsync(workspaceId);
        data.Statuses = await this.statusRepository.ListAsync(workspaceId);
        data.Priorities = await this.priorityRepository.ListAsync(workspaceId);
        data.Types = await this.ticketTypeRepository.ListAsync(workspaceId);

        // Notification defaults (placeholder until persisted storage exists)
        data.NotificationsEnabled = true;
        data.EmailIntegrationEnabled = true;
        data.EmailProvider = "smtp";
        data.SmsIntegrationEnabled = false;
        data.SmsProvider = "none";
        data.PushIntegrationEnabled = false;
        data.PushProvider = "none";
        data.InAppNotificationsEnabled = true;
        data.BatchNotificationDelay = 30;
        data.DailySummaryHour = 9;
        data.MentionNotificationsUrgent = true;
        data.TicketAssignmentNotificationsHigh = true;

        return data;
    }
}



