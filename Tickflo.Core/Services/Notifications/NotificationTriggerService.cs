namespace Tickflo.Core.Services.Notifications;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Implementation of notification trigger service.
/// Coordinates notification dispatch for all business events.
/// </summary>
public class NotificationTriggerService(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    ITeamRepository teamRepository,
    IWorkspaceRepository workspaceRepository,
    IContactRepository contactRepository) : INotificationTriggerService
{
    private readonly INotificationRepository notificationRepository = notificationRepository;
    private readonly IUserRepository userRepository = userRepository;
    private readonly ITeamRepository teamRepository = teamRepository;
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly IContactRepository contactRepository = contactRepository;

    public async Task NotifyTicketCreatedAsync(
        int workspaceId,
        Ticket ticket,
        int createdByUserId)
    {
        var notifications = new List<Notification>();
        var creator = await this.userRepository.FindByIdAsync(createdByUserId);
        var creatorName = creator?.Name ?? creator?.Email ?? "Someone";

        var workspace = await this.workspaceRepository.FindByIdAsync(workspaceId);
        var ticketData = System.Text.Json.JsonSerializer.Serialize(new { ticketId = ticket.Id, workspaceSlug = workspace?.Slug });

        // Notify assigned user
        if (ticket.AssignedUserId.HasValue && ticket.AssignedUserId.Value != createdByUserId)
        {
            notifications.Add(new Notification
            {
                UserId = ticket.AssignedUserId.Value,
                WorkspaceId = workspaceId,
                Type = "ticket_assigned",
                DeliveryMethod = "in_app",
                Priority = "normal",
                Subject = "New Ticket Assigned",
                Body = $"{creatorName} assigned you ticket #{ticket.Id}: {ticket.Subject}",
                Data = ticketData,
                Status = "sent",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdByUserId
            });
        }

        // Notify team members if assigned to team
        if (ticket.AssignedTeamId.HasValue)
        {
            var team = await this.teamRepository.FindByIdAsync(ticket.AssignedTeamId.Value);
            if (team != null)
            {
                // Team notification - would need ITeamMemberRepository for this
                // For now, just queue the team notification
                notifications.Add(new Notification
                {
                    WorkspaceId = workspaceId,
                    Type = "ticket_created_team",
                    DeliveryMethod = "in_app",
                    Priority = "normal",
                    Subject = "New Ticket for Team",
                    Body = $"{creatorName} created ticket #{ticket.Id} for team {team.Name}: {ticket.Subject}",
                    Status = "sent",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdByUserId
                });
            }
        }

        // Add all notifications to queue
        foreach (var notif in notifications)
        {
            await this.notificationRepository.AddAsync(notif);
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
        var changer = await this.userRepository.FindByIdAsync(changedByUserId);
        var changerName = changer?.Name ?? changer?.Email ?? "Someone";

        var workspace = await this.workspaceRepository.FindByIdAsync(workspaceId);
        var ticketData = System.Text.Json.JsonSerializer.Serialize(new { ticketId = ticket.Id, workspaceSlug = workspace?.Slug });

        // Notify previously assigned user (unassigned)
        if (previousUserId.HasValue && previousUserId != ticket.AssignedUserId)
        {
            notifications.Add(new Notification
            {
                UserId = previousUserId.Value,
                WorkspaceId = workspaceId,
                Type = "ticket_unassigned",
                DeliveryMethod = "in_app",
                Priority = "normal",
                Subject = "Ticket Unassigned",
                Body = $"{changerName} unassigned you from ticket #{ticket.Id}: {ticket.Subject}",
                Data = ticketData,
                Status = "sent",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = changedByUserId
            });
        }

        // Notify newly assigned user
        if (ticket.AssignedUserId.HasValue && ticket.AssignedUserId.Value != changedByUserId)
        {
            notifications.Add(new Notification
            {
                UserId = ticket.AssignedUserId.Value,
                WorkspaceId = workspaceId,
                Type = "ticket_assigned",
                DeliveryMethod = "in_app",
                Priority = "normal",
                Subject = "Ticket Assigned",
                Body = $"{changerName} assigned you ticket #{ticket.Id}: {ticket.Subject}",
                Data = ticketData,
                Status = "sent",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = changedByUserId
            });
        }

        foreach (var notif in notifications)
        {
            await this.notificationRepository.AddAsync(notif);
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
        var changer = await this.userRepository.FindByIdAsync(changedByUserId);
        var changerName = changer?.Name ?? changer?.Email ?? "Someone";

        var workspace = await this.workspaceRepository.FindByIdAsync(workspaceId);
        var ticketData = System.Text.Json.JsonSerializer.Serialize(new { ticketId = ticket.Id, workspaceSlug = workspace?.Slug });

        // Notify assigned user/team about status change
        if (ticket.AssignedUserId.HasValue && ticket.AssignedUserId.Value != changedByUserId)
        {
            notifications.Add(new Notification
            {
                UserId = ticket.AssignedUserId.Value,
                WorkspaceId = workspaceId,
                Type = "ticket_status_changed",
                DeliveryMethod = "in_app",
                Priority = "normal",
                Subject = "Ticket Status Changed",
                Body = $"{changerName} changed ticket #{ticket.Id} status from {previousStatus} to {newStatus}: {ticket.Subject}",
                Data = ticketData,
                Status = "sent",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = changedByUserId
            });
        }

        if (ticket.AssignedTeamId.HasValue)
        {
            var team = await this.teamRepository.FindByIdAsync(ticket.AssignedTeamId.Value);
            if (team != null)
            {
                // Team notification queued
                notifications.Add(new Notification
                {
                    WorkspaceId = workspaceId,
                    Type = "ticket_status_changed_team",
                    DeliveryMethod = "in_app",
                    Priority = "normal",
                    Subject = "Ticket Status Changed",
                    Body = $"{changerName} changed ticket #{ticket.Id} status from {previousStatus} to {newStatus} for team {team.Name}: {ticket.Subject}",
                    Status = "sent",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = changedByUserId
                });
            }
        }

        foreach (var notif in notifications)
        {
            await this.notificationRepository.AddAsync(notif);
        }
    }

    public async Task NotifyTicketCommentAddedAsync(
        int workspaceId,
        Ticket ticket,
        int commentedByUserId,
        bool isVisibleToClient)
    {
        var notifications = new List<Notification>();
        var commenter = await this.userRepository.FindByIdAsync(commentedByUserId);
        var commenterName = commenter?.Name ?? commenter?.Email ?? "Someone";

        var workspace = await this.workspaceRepository.FindByIdAsync(workspaceId);
        var ticketData = System.Text.Json.JsonSerializer.Serialize(new { ticketId = ticket.Id, workspaceSlug = workspace?.Slug });

        // Collect recipients: assigned user and contact's assigned user (if any)
        var recipientIds = new List<int?> { ticket.AssignedUserId };

        if (ticket.ContactId.HasValue)
        {
            var contact = await this.contactRepository.FindAsync(workspaceId, ticket.ContactId.Value);
            if (contact?.AssignedUserId.HasValue == true)
            {
                recipientIds.Add(contact.AssignedUserId);
            }
        }

        foreach (var recipientId in recipientIds)
        {
            if (!recipientId.HasValue)
            {
                continue;
            }

            // Skip notifying the user who made the comment
            if (recipientId.Value == commentedByUserId)
            {
                continue;
            }

            // Skip notifying contact-assigned user if comment is internal-only (not visible to client)
            if (!isVisibleToClient && ticket.ContactId.HasValue)
            {
                var contact = await this.contactRepository.FindAsync(workspaceId, ticket.ContactId.Value);
                if (contact?.AssignedUserId == recipientId.Value)
                {
                    continue;
                }
            }

            // In-app notification (immediate)
            notifications.Add(new Notification
            {
                UserId = recipientId.Value,
                WorkspaceId = workspaceId,
                Type = "ticket_comment",
                DeliveryMethod = "in_app",
                Priority = "normal",
                Subject = "New Comment on Ticket",
                Body = $"{commenterName} added a comment to ticket #{ticket.Id}: {ticket.Subject}",
                Data = ticketData,
                Status = "sent",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = commentedByUserId
            });

            // Email notification (queued for batch send)
            notifications.Add(new Notification
            {
                UserId = recipientId.Value,
                WorkspaceId = workspaceId,
                Type = "ticket_comment",
                DeliveryMethod = "email",
                Priority = "normal",
                Subject = "New Comment on Ticket",
                Body = $"{commenterName} added a comment to ticket #{ticket.Id}: {ticket.Subject}",
                Data = ticketData,
                Status = "pending", // queued for batch email sender
                CreatedAt = DateTime.UtcNow,
                CreatedBy = commentedByUserId
            });
        }

        foreach (var notif in notifications)
        {
            await this.notificationRepository.AddAsync(notif);
        }
    }

    public async Task NotifyUserAddedToWorkspaceAsync(
        int workspaceId,
        int userId,
        int addedByUserId)
    {
        var adder = await this.userRepository.FindByIdAsync(addedByUserId);
        var adderName = adder?.Name ?? adder?.Email ?? "Someone";

        // Notify the invited user
        var notification = new Notification
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            Type = "workspace_invitation",
            DeliveryMethod = "in_app",
            Priority = "normal",
            Subject = "Added to Workspace",
            Body = $"{adderName} added you to a workspace",
            Status = "sent",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = addedByUserId
        };

        await this.notificationRepository.AddAsync(notification);
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

        await this.notificationRepository.AddAsync(notification);
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
            await this.notificationRepository.AddAsync(notif);
        }
    }

    public async Task SendTransactionalEmailAsync(
        string email,
        string subject,
        string message) =>
        // Transactional emails are user-level, not workspace-scoped
        // These would typically be handled by email service directly
        // This is a placeholder for the interface contract
        await Task.CompletedTask;

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

        await this.notificationRepository.AddAsync(notification);
    }
}
