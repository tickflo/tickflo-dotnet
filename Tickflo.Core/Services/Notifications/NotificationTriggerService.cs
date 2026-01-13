using Tickflo.Core.Entities;
using Tickflo.Core.Data;

namespace Tickflo.Core.Services.Notifications;

/// <summary>
/// Implementation of notification trigger service.
/// Coordinates notification dispatch for all business events.
/// </summary>
public class NotificationTriggerService : INotificationTriggerService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly IUserRepository _userRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;

    public NotificationTriggerService(
        INotificationRepository notificationRepo,
        IUserRepository userRepo,
        ITeamRepository teamRepo,
        ILocationRepository locationRepo,
        IUserWorkspaceRepository userWorkspaceRepo)
    {
        _notificationRepo = notificationRepo;
        _userRepo = userRepo;
        _teamRepo = teamRepo;
        _locationRepo = locationRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
    }

    public async Task NotifyTicketCreatedAsync(
        int workspaceId,
        Ticket ticket,
        int createdByUserId)
    {
        var notifications = new List<Notification>();

        // Notify assigned user
        if (ticket.AssignedUserId.HasValue)
        {
            notifications.Add(new Notification
            {
                UserId = ticket.AssignedUserId.Value,
                WorkspaceId = workspaceId,
                Type = "ticket_created",
                DeliveryMethod = "email",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Notify team members if assigned to team
        if (ticket.AssignedTeamId.HasValue)
        {
            var team = await _teamRepo.FindByIdAsync(ticket.AssignedTeamId.Value);
            if (team != null)
            {
                // Team notification - would need ITeamMemberRepository for this
                // For now, just queue the team notification
                notifications.Add(new Notification
                {
                    WorkspaceId = workspaceId,
                    Type = "ticket_created_team",
                    DeliveryMethod = "email",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Add all notifications to queue
        foreach (var notif in notifications)
        {
            await _notificationRepo.AddAsync(notif);
        }
    }

    public async Task NotifyTicketAssignmentChangedAsync(
        int workspaceId,
        Ticket ticket,
        int? previousUserId,
        int? previousTeamId,
        int changedByUserId)
    {
        var notifications = new List<Notification>();

        // Notify previously assigned user (unassigned)
        if (previousUserId.HasValue && previousUserId != ticket.AssignedUserId)
        {
            notifications.Add(new Notification
            {
                UserId = previousUserId.Value,
                WorkspaceId = workspaceId,
                Type = "ticket_unassigned",
                DeliveryMethod = "email",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Notify newly assigned user
        if (ticket.AssignedUserId.HasValue)
        {
            notifications.Add(new Notification
            {
                UserId = ticket.AssignedUserId.Value,
                WorkspaceId = workspaceId,
                Type = "ticket_assigned",
                DeliveryMethod = "email",
                CreatedAt = DateTime.UtcNow
            });
        }

        foreach (var notif in notifications)
        {
            await _notificationRepo.AddAsync(notif);
        }
    }

    public async Task NotifyTicketStatusChangedAsync(
        int workspaceId,
        Ticket ticket,
        string previousStatus,
        string newStatus,
        int changedByUserId)
    {
        var notifications = new List<Notification>();

        // Notify assigned user/team about status change
        if (ticket.AssignedUserId.HasValue)
        {
            notifications.Add(new Notification
            {
                UserId = ticket.AssignedUserId.Value,
                WorkspaceId = workspaceId,
                Type = "ticket_status_changed",
                DeliveryMethod = "email",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (ticket.AssignedTeamId.HasValue)
        {
            var team = await _teamRepo.FindByIdAsync(ticket.AssignedTeamId.Value);
            if (team != null)
            {
                // Team notification queued
                notifications.Add(new Notification
                {
                    WorkspaceId = workspaceId,
                    Type = "ticket_status_changed_team",
                    DeliveryMethod = "email",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        foreach (var notif in notifications)
        {
            await _notificationRepo.AddAsync(notif);
        }
    }

    public async Task NotifyUserAddedToWorkspaceAsync(
        int workspaceId,
        int userId,
        int addedByUserId)
    {
        // Notify the invited user
        var notification = new Notification
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            Type = "workspace_invitation",
            DeliveryMethod = "email",
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepo.AddAsync(notification);
    }

    public async Task NotifyAdminsAsync(
        int workspaceId,
        string subject,
        string message,
        Dictionary<string, string>? contextData = null)
    {
        // Find all admin users in workspace
        // This would require a query to find admins - for now, store as audit log
        var notification = new Notification
        {
            WorkspaceId = workspaceId,
            Type = "admin_alert",
            DeliveryMethod = "email",
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepo.AddAsync(notification);
    }

    public async Task NotifyUsersAsync(
        int workspaceId,
        List<int> userIds,
        string subject,
        string message,
        int triggeredByUserId)
    {
        var notifications = new List<Notification>();

        foreach (var userId in userIds)
        {
            notifications.Add(new Notification
            {
                UserId = userId,
                WorkspaceId = workspaceId,
                Type = "bulk_notification",
                DeliveryMethod = "email",
                CreatedAt = DateTime.UtcNow
            });
        }

        foreach (var notif in notifications)
        {
            await _notificationRepo.AddAsync(notif);
        }
    }

    public async Task SendTransactionalEmailAsync(
        string email,
        string subject,
        string message)
    {
        // Transactional emails are user-level, not workspace-scoped
        // These would typically be handled by email service directly
        // This is a placeholder for the interface contract
        await Task.CompletedTask;
    }

    public async Task NotifyWorkflowCompletionAsync(
        int workspaceId,
        int userId,
        string workflowName,
        string message,
        Dictionary<string, string>? resultData = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            Type = "workflow_completed",
            DeliveryMethod = "email",
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepo.AddAsync(notification);
    }
}
