using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Views;

public class WorkspaceSettingsViewData
{
    public bool CanViewSettings { get; set; }
    public bool CanEditSettings { get; set; }
    public bool CanCreateSettings { get; set; }

    public IReadOnlyList<Tickflo.Core.Entities.TicketStatus> Statuses { get; set; } = Array.Empty<Tickflo.Core.Entities.TicketStatus>();
    public IReadOnlyList<Tickflo.Core.Entities.TicketPriority> Priorities { get; set; } = Array.Empty<Tickflo.Core.Entities.TicketPriority>();
    public IReadOnlyList<Tickflo.Core.Entities.TicketType> Types { get; set; } = Array.Empty<Tickflo.Core.Entities.TicketType>();

    public bool NotificationsEnabled { get; set; } = true;
    public bool EmailIntegrationEnabled { get; set; } = true;
    public string EmailProvider { get; set; } = "smtp";
    public bool SmsIntegrationEnabled { get; set; } = false;
    public string SmsProvider { get; set; } = "none";
    public bool PushIntegrationEnabled { get; set; } = false;
    public string PushProvider { get; set; } = "none";
    public bool InAppNotificationsEnabled { get; set; } = true;
    public int BatchNotificationDelay { get; set; } = 30;
    public int DailySummaryHour { get; set; } = 9;
    public bool MentionNotificationsUrgent { get; set; } = true;
    public bool TicketAssignmentNotificationsHigh { get; set; } = true;
}

public interface IWorkspaceSettingsViewService
{
    Task<WorkspaceSettingsViewData> BuildAsync(int workspaceId, int userId);
}


