namespace Tickflo.Core.Services.Notifications;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Services.Web;

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
    public Task NotifyTicketCreatedAsync(
        int workspaceId,
        Ticket ticket,
        int createdByUserId);

    /// <summary>
    /// Notify ticket assignee when assignment changes.
    /// Notifies previously assigned and newly assigned parties.
    /// </summary>
    public Task NotifyTicketAssignmentChangedAsync(
        int workspaceId,
        Ticket ticket,
        int? previousUserId,
        int? previousTeamId,
        int changedByUserId);

    /// <summary>
    /// Notify relevant parties when ticket status changes.
    /// Different notifications for different status transitions.
    /// </summary>
    public Task NotifyTicketStatusChangedAsync(
        int workspaceId,
        Ticket ticket,
        string previousStatus,
        string newStatus,
        int changedByUserId);

    /// <summary>
    /// Notify assignee and creator when a ticket is updated.
    /// </summary>
    public Task NotifyTicketUpdatedAsync(
        int workspaceId,
        Ticket ticket,
        int updatedByUserId,
        string changeSummary,
        IReadOnlyCollection<int>? excludedUserIds = null);

    /// <summary>
    /// Notify relevant parties when a comment is added to a ticket.
    /// Notifies assigned user if they didn't create the comment.
    /// </summary>
    public Task NotifyTicketCommentAddedAsync(
        int workspaceId,
        Ticket ticket,
        int commentedByUserId,
        bool isVisibleToClient);

    /// <summary>
    /// Notify user when they are added to a workspace.
    /// Sends invitation/welcome notification.
    /// </summary>
    public Task NotifyUserAddedToWorkspaceAsync(
        int workspaceId,
        int userId,
        int addedByUserId);

    /// <summary>
    /// Notify workspace admins when a critical action occurs.
    /// Used for auditable changes like user removal, role changes, etc.
    /// </summary>
    public Task NotifyAdminsAsync(
        int workspaceId,
        string subject,
        string message,
        Dictionary<string, string>? contextData = null);

    /// <summary>
    /// Send bulk notifications to a group of users efficiently.
    /// </summary>
    public Task NotifyUsersAsync(
        int workspaceId,
        List<int> userIds,
        string subject,
        string message,
        int triggeredByUserId);

    /// <summary>
    /// Send transactional email (password reset, email confirmation, etc).
    /// Not workspace-scoped as these are user-level notifications.
    /// </summary>
    public Task SendTransactionalEmailAsync(
        string email,
        string subject,
        string message);

    /// <summary>
    /// Notify user about workflow completion (report ready, batch job done, etc).
    /// </summary>
    public Task NotifyWorkflowCompletionAsync(
        int workspaceId,
        int userId,
        string workflowName,
        string message,
        Dictionary<string, string>? resultData = null);
}

public class NotificationTriggerService(
    TickfloDbContext dbContext,
    IEmailSendService emailSendService,
    TickfloConfig tickfloConfig,
    IRequestOriginService requestOriginService) : INotificationTriggerService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IEmailSendService emailSendService = emailSendService;
    private readonly TickfloConfig tickfloConfig = tickfloConfig;
    private readonly IRequestOriginService requestOriginService = requestOriginService;

    public async Task NotifyTicketCreatedAsync(
        int workspaceId,
        Ticket ticket,
        int createdByUserId)
    {
        var notifications = new List<Notification>();
        var creator = await this.dbContext.Users.FindAsync(createdByUserId);
        var creatorName = creator?.Name ?? creator?.Email ?? "Someone";

        var workspace = await this.dbContext.Workspaces.FindAsync(workspaceId);
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
                CreatedBy = createdByUserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Notify team members if assigned to team
        if (ticket.AssignedTeamId.HasValue)
        {
            var team = await this.dbContext.Teams.FindAsync(ticket.AssignedTeamId.Value);
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
                    CreatedBy = createdByUserId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (notifications.Count > 0)
        {
            this.dbContext.Notifications.AddRange(notifications);
            await this.dbContext.SaveChangesAsync();
        }

        await this.QueueTicketAssignmentEmailAsync(workspaceId, ticket, createdByUserId);
    }

    public async Task NotifyTicketAssignmentChangedAsync(
        int workspaceId,
        Ticket ticket,
        int? previousUserId,
        int? previousTeamId,
        int changedByUserId)
    {
        var notifications = new List<Notification>();
        var changer = await this.dbContext.Users.FindAsync(changedByUserId);
        var changerName = changer?.Name ?? changer?.Email ?? "Someone";

        var workspace = await this.dbContext.Workspaces.FindAsync(workspaceId);
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
                CreatedBy = changedByUserId,
                CreatedAt = DateTime.UtcNow
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
                CreatedBy = changedByUserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (notifications.Count > 0)
        {
            this.dbContext.Notifications.AddRange(notifications);
            await this.dbContext.SaveChangesAsync();
        }

        await this.QueueTicketAssignmentEmailAsync(workspaceId, ticket, changedByUserId);
    }

    public async Task NotifyTicketStatusChangedAsync(
        int workspaceId,
        Ticket ticket,
        string previousStatus,
        string newStatus,
        int changedByUserId)
    {
        var notifications = new List<Notification>();
        var changer = await this.dbContext.Users.FindAsync(changedByUserId);
        var changerName = changer?.Name ?? changer?.Email ?? "Someone";

        var workspace = await this.dbContext.Workspaces.FindAsync(workspaceId);
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
                CreatedBy = changedByUserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (ticket.AssignedTeamId.HasValue)
        {
            var team = await this.dbContext.Teams.FindAsync(ticket.AssignedTeamId.Value);
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
                    CreatedBy = changedByUserId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (notifications.Count > 0)
        {
            this.dbContext.Notifications.AddRange(notifications);
            await this.dbContext.SaveChangesAsync();
        }
    }

    public async Task NotifyTicketUpdatedAsync(
        int workspaceId,
        Ticket ticket,
        int updatedByUserId,
        string changeSummary,
        IReadOnlyCollection<int>? excludedUserIds = null)
    {
        if (string.IsNullOrWhiteSpace(changeSummary))
        {
            return;
        }

        var recipients = await this.GetTicketEmailRecipientsAsync(ticket, updatedByUserId, excludedUserIds);
        if (recipients.Count == 0)
        {
            return;
        }

        var actorName = await this.GetUserDisplayNameAsync(updatedByUserId);
        var workspace = await this.GetWorkspaceAsync(workspaceId);
        var ticketLink = this.BuildTicketLink(workspace?.Slug, ticket.Id);

        foreach (var recipient in recipients)
        {
            await this.emailSendService.AddToQueueAsync(
                recipient.Email,
                EmailTemplateType.TicketUpdated,
                BuildTicketEmailVariables(
                    recipient.Name,
                    actorName,
                    workspace?.Name,
                    ticket,
                    ticketLink,
                    changeSummary),
                updatedByUserId);
        }
    }

    public async Task NotifyTicketCommentAddedAsync(
        int workspaceId,
        Ticket ticket,
        int commentedByUserId,
        bool isVisibleToClient)
    {
        var notifications = new List<Notification>();
        var commenterName = await this.GetUserDisplayNameAsync(commentedByUserId);

        var workspace = await this.dbContext.Workspaces.FindAsync(workspaceId);
        var ticketData = System.Text.Json.JsonSerializer.Serialize(new { ticketId = ticket.Id, workspaceSlug = workspace?.Slug });
        var additionalRecipientIds = new List<int>();
        var contactAssignedUserId = await this.GetContactAssignedUserIdAsync(workspaceId, ticket.ContactId);
        if (isVisibleToClient && contactAssignedUserId.HasValue)
        {
            additionalRecipientIds.Add(contactAssignedUserId.Value);
        }

        var recipients = await this.GetTicketEmailRecipientsAsync(ticket, commentedByUserId, null, additionalRecipientIds);

        foreach (var recipient in recipients)
        {
            // In-app notification (immediate)
            notifications.Add(new Notification
            {
                UserId = recipient.Id,
                WorkspaceId = workspaceId,
                Type = "ticket_comment",
                DeliveryMethod = "in_app",
                Priority = "normal",
                Subject = "New Comment on Ticket",
                Body = $"{commenterName} added a comment to ticket #{ticket.Id}: {ticket.Subject}",
                Data = ticketData,
                Status = "sent",
                CreatedBy = commentedByUserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (notifications.Count > 0)
        {
            this.dbContext.Notifications.AddRange(notifications);
            await this.dbContext.SaveChangesAsync();
        }

        var commentSummary = isVisibleToClient
            ? "A new client-visible comment was added."
            : "A new internal comment was added.";
        var ticketLink = this.BuildTicketLink(workspace?.Slug, ticket.Id);

        foreach (var recipient in recipients)
        {
            await this.emailSendService.AddToQueueAsync(
                recipient.Email,
                EmailTemplateType.TicketComment,
                BuildTicketEmailVariables(
                    recipient.Name,
                    commenterName,
                    workspace?.Name,
                    ticket,
                    ticketLink,
                    commentSummary),
                commentedByUserId);
        }
    }

    private async Task QueueTicketAssignmentEmailAsync(int workspaceId, Ticket ticket, int actorUserId)
    {
        if (!ticket.AssignedUserId.HasValue)
        {
            return;
        }

        var assignedUser = await this.dbContext.Users.FindAsync(ticket.AssignedUserId.Value);
        if (assignedUser == null || string.IsNullOrWhiteSpace(assignedUser.Email))
        {
            return;
        }

        var actorName = await this.GetUserDisplayNameAsync(actorUserId);
        var workspace = await this.GetWorkspaceAsync(workspaceId);
        var ticketLink = this.BuildTicketLink(workspace?.Slug, ticket.Id);

        await this.emailSendService.AddToQueueAsync(
            assignedUser.Email,
            EmailTemplateType.TicketAssigned,
            BuildTicketEmailVariables(
                assignedUser.Name,
                actorName,
                workspace?.Name,
                ticket,
                ticketLink,
                "You have been assigned this ticket."),
            actorUserId);
    }

    private async Task<List<User>> GetTicketEmailRecipientsAsync(
        Ticket ticket,
        int actorUserId,
        IReadOnlyCollection<int>? excludedUserIds,
        IReadOnlyCollection<int>? additionalUserIds = null)
    {
        var recipientIds = new HashSet<int>();
        if (ticket.AssignedUserId.HasValue)
        {
            recipientIds.Add(ticket.AssignedUserId.Value);
        }

        var creatorUserId = await this.GetTicketCreatorUserIdAsync(ticket.Id);
        if (creatorUserId.HasValue)
        {
            recipientIds.Add(creatorUserId.Value);
        }

        if (additionalUserIds != null)
        {
            foreach (var additionalUserId in additionalUserIds)
            {
                recipientIds.Add(additionalUserId);
            }
        }

        recipientIds.Remove(actorUserId);

        if (excludedUserIds != null)
        {
            foreach (var excludedUserId in excludedUserIds)
            {
                recipientIds.Remove(excludedUserId);
            }
        }

        return await this.dbContext.Users
            .Where(user => recipientIds.Contains(user.Id) && !string.IsNullOrWhiteSpace(user.Email))
            .ToListAsync();
    }

    private async Task<int?> GetContactAssignedUserIdAsync(int workspaceId, int? contactId)
    {
        if (!contactId.HasValue)
        {
            return null;
        }

        return await this.dbContext.Contacts
            .Where(contact => contact.WorkspaceId == workspaceId && contact.Id == contactId.Value)
            .Select(contact => contact.AssignedUserId)
            .FirstOrDefaultAsync();
    }

    private async Task<int?> GetTicketCreatorUserIdAsync(int ticketId) =>
        await this.dbContext.TicketHistory
            .Where(history => history.TicketId == ticketId && history.Action == TicketHistoryAction.Created)
            .OrderBy(history => history.CreatedAt)
            .ThenBy(history => history.Id)
            .Select(history => (int?)history.CreatedByUserId)
            .FirstOrDefaultAsync();

    private async Task<string> GetUserDisplayNameAsync(int userId)
    {
        var user = await this.dbContext.Users.FindAsync(userId);
        return user?.Name ?? user?.Email ?? "Someone";
    }

    private async Task<Workspace?> GetWorkspaceAsync(int workspaceId) =>
        await this.dbContext.Workspaces.FindAsync(workspaceId);

    private string BuildTicketLink(string? workspaceSlug, int ticketId)
    {
        var baseUrl = this.requestOriginService.GetCurrentOrigin().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = this.tickfloConfig.BaseUrl.TrimEnd('/');
        }

        if (string.IsNullOrWhiteSpace(workspaceSlug))
        {
            return $"{baseUrl}/workspaces";
        }

        return $"{baseUrl}/workspaces/{workspaceSlug}/tickets/{ticketId}";
    }

    private static Dictionary<string, string> BuildTicketEmailVariables(
        string? recipientName,
        string actorName,
        string? workspaceName,
        Ticket ticket,
        string ticketLink,
        string changeSummary) =>
        new()
        {
            { "recipient_name", string.IsNullOrWhiteSpace(recipientName) ? "there" : recipientName },
            { "actor_name", actorName },
            { "workspace_name", workspaceName ?? "your workspace" },
            { "ticket_id", ticket.Id.ToString() },
            { "ticket_subject", ticket.Subject },
            { "ticket_link", ticketLink },
            { "change_summary", changeSummary }
        };

    public async Task NotifyUserAddedToWorkspaceAsync(
        int workspaceId,
        int userId,
        int addedByUserId)
    {
        var adder = await this.dbContext.Users.FindAsync(addedByUserId);
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
            CreatedBy = addedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        this.dbContext.Notifications.Add(notification);
        await this.dbContext.SaveChangesAsync();
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

        this.dbContext.Notifications.Add(notification);
        await this.dbContext.SaveChangesAsync();
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

        this.dbContext.Notifications.AddRange(notifications);
        await this.dbContext.SaveChangesAsync();
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

        this.dbContext.Notifications.Add(notification);
        await this.dbContext.SaveChangesAsync();
    }
}
