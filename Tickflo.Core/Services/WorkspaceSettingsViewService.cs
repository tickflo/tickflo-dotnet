using Tickflo.Core.Data;

namespace Tickflo.Core.Services;

public class WorkspaceSettingsViewService : IWorkspaceSettingsViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePermRepo;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ITicketTypeRepository _typeRepo;
    private readonly IWorkspaceSettingsService _settingsService;

    public WorkspaceSettingsViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePermRepo,
        ITicketStatusRepository statusRepo,
        ITicketPriorityRepository priorityRepo,
        ITicketTypeRepository typeRepo,
        IWorkspaceSettingsService settingsService)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePermRepo = rolePermRepo;
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
        _typeRepo = typeRepo;
        _settingsService = settingsService;
    }

    public async Task<WorkspaceSettingsViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceSettingsViewData();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        if (isAdmin)
        {
            data.CanViewSettings = data.CanEditSettings = data.CanCreateSettings = true;
        }
        else
        {
            var perms = await _rolePermRepo.GetEffectivePermissionsForUserAsync(workspaceId, userId);
            if (perms.TryGetValue("settings", out var eff))
            {
                data.CanViewSettings = eff.CanView;
                data.CanEditSettings = eff.CanEdit;
                data.CanCreateSettings = eff.CanCreate;
            }
        }

        // Ensure defaults and load lists
        await _settingsService.EnsureDefaultsExistAsync(workspaceId);
        data.Statuses = await _statusRepo.ListAsync(workspaceId);
        data.Priorities = await _priorityRepo.ListAsync(workspaceId);
        data.Types = await _typeRepo.ListAsync(workspaceId);

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
