namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

using Tickflo.Core.Services.Workspace;

public class WorkspaceSettingsViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermRepo,
    ITicketStatusRepository statusRepo,
    ITicketPriorityRepository priorityRepo,
    ITicketTypeRepository typeRepo,
    IWorkspaceSettingsService settingsService) : IWorkspaceSettingsViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePermRepo = rolePermRepo;
    private readonly ITicketStatusRepository _statusRepo = statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo = priorityRepo;
    private readonly ITicketTypeRepository _typeRepo = typeRepo;
    private readonly IWorkspaceSettingsService _settingsService = settingsService;

    public async Task<WorkspaceSettingsViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceSettingsViewData();

        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        if (isAdmin)
        {
            data.CanViewSettings = data.CanEditSettings = data.CanCreateSettings = true;
        }
        else
        {
            var perms = await this._rolePermRepo.GetEffectivePermissionsForUserAsync(workspaceId, userId);
            if (perms.TryGetValue("settings", out var eff))
            {
                data.CanViewSettings = eff.CanView;
                data.CanEditSettings = eff.CanEdit;
                data.CanCreateSettings = eff.CanCreate;
            }
        }

        // Ensure defaults and load lists
        await this._settingsService.EnsureDefaultsExistAsync(workspaceId);
        data.Statuses = await this._statusRepo.ListAsync(workspaceId);
        data.Priorities = await this._priorityRepo.ListAsync(workspaceId);
        data.Types = await this._typeRepo.ListAsync(workspaceId);

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



