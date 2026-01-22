namespace Tickflo.Web.Pages;

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;

public class NotificationTicketData
{
    [JsonPropertyName("ticketId")]
    public int TicketId { get; set; }
    [JsonPropertyName("workspaceSlug")]
    public string? WorkspaceSlug { get; set; }
}

[Authorize]
public class NotificationsModel(
    INotificationRepository notificationRepository,
    ICurrentUserService currentUserService) : PageModel
{
    private readonly INotificationRepository notificationRepository = notificationRepository;
    private readonly ICurrentUserService currentUserService = currentUserService;

    public List<Notification> Notifications { get; set; } = [];

    public NotificationTicketData? GetTicketData(Notification notification)
    {
        if (string.IsNullOrEmpty(notification.Data))
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<NotificationTicketData>(notification.Data);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!this.currentUserService.TryGetUserId(this.User, out var userId))
        {
            return this.Forbid();
        }

        this.Notifications = await this.notificationRepository.ListForUserAsync(userId);
        return this.Page();
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
    {
        if (!this.currentUserService.TryGetUserId(this.User, out var userId))
        {
            return this.Forbid();
        }

        var notification = await this.notificationRepository.FindByIdAsync(id);
        if (notification == null || notification.UserId != userId)
        {
            return this.NotFound();
        }

        await this.notificationRepository.MarkAsReadAsync(id);
        return this.RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllAsReadAsync()
    {
        if (!this.currentUserService.TryGetUserId(this.User, out var userId))
        {
            return this.Forbid();
        }

        var notifications = await this.notificationRepository.ListForUserAsync(userId, unreadOnly: true);
        foreach (var notification in notifications)
        {
            await this.notificationRepository.MarkAsReadAsync(notification.Id);
        }

        return this.RedirectToPage();
    }
}
