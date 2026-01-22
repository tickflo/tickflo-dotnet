namespace Tickflo.Core.Services.Views;

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


