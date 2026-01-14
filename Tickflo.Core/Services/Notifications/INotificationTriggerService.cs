using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Notifications;

/// <summary>
/// Behavior-focused service for triggering and managing notifications based on business events.
/// Acts as a centralized hub for all notification needs across the application.
/// </summary>
public interface INotificationTriggerService
{
    /// <summary>
    /// Notify relevant parties when a ticket is created.
    /// Notifies contact (if has account), assigned user/team, location owner.
    /// </summary>
    Task NotifyTicketCreatedAsync(
        int workspaceId,
        Ticket ticket,
        int createdByUserId);

    /// <summary>
    /// Notify ticket assignee when assignment changes.
    /// Notifies previously assigned and newly assigned parties.
    /// </summary>
    Task NotifyTicketAssignmentChangedAsync(
        int workspaceId,
        Ticket ticket,
        int? previousUserId,
        int? previousTeamId,
        int changedByUserId);

    /// <summary>
    /// Notify relevant parties when ticket status changes.
    /// Different notifications for different status transitions.
    /// </summary>
    Task NotifyTicketStatusChangedAsync(
        int workspaceId,
        Ticket ticket,
        string previousStatus,
        string newStatus,
        int changedByUserId);

    /// <summary>
    /// Notify relevant parties when a comment is added to a ticket.
    /// Notifies assigned user if they didn't create the comment.
    /// </summary>
    Task NotifyTicketCommentAddedAsync(
        int workspaceId,
        Ticket ticket,
        int commentedByUserId,
        bool isVisibleToClient);

    /// <summary>
    /// Notify user when they are added to a workspace.
    /// Sends invitation/welcome notification.
    /// </summary>
    Task NotifyUserAddedToWorkspaceAsync(
        int workspaceId,
        int userId,
        int addedByUserId);

    /// <summary>
    /// Notify workspace admins when a critical action occurs.
    /// Used for auditable changes like user removal, role changes, etc.
    /// </summary>
    Task NotifyAdminsAsync(
        int workspaceId,
        string subject,
        string message,
        Dictionary<string, string>? contextData = null);

    /// <summary>
    /// Send bulk notifications to a group of users efficiently.
    /// </summary>
    Task NotifyUsersAsync(
        int workspaceId,
        List<int> userIds,
        string subject,
        string message,
        int triggeredByUserId);

    /// <summary>
    /// Send transactional email (password reset, email confirmation, etc).
    /// Not workspace-scoped as these are user-level notifications.
    /// </summary>
    Task SendTransactionalEmailAsync(
        string email,
        string subject,
        string message);

    /// <summary>
    /// Notify user about workflow completion (report ready, batch job done, etc).
    /// </summary>
    Task NotifyWorkflowCompletionAsync(
        int workspaceId,
        int userId,
        string workflowName,
        string message,
        Dictionary<string, string>? resultData = null);
}
