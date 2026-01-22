namespace Tickflo.Web.Pages;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;

public class NotificationTicketData
{
    public int ticketId { get; set; }
    public string? workspaceSlug { get; set; }
}

[Authorize]
public class NotificationsModel(
    INotificationRepository notificationRepo,
    ICurrentUserService currentUserService) : PageModel
{
    private readonly INotificationRepository _notificationRepo = notificationRepo;
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

        this.Notifications = await this._notificationRepo.ListForUserAsync(userId);
        return this.Page();
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
    {
        if (!this.currentUserService.TryGetUserId(this.User, out var userId))
        {
            return this.Forbid();
        }

        var notification = await this._notificationRepo.FindByIdAsync(id);
        if (notification == null || notification.UserId != userId)
        {
            return this.NotFound();
        }

        await this._notificationRepo.MarkAsReadAsync(id);
        return this.RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllAsReadAsync()
    {
        if (!this.currentUserService.TryGetUserId(this.User, out var userId))
        {
            return this.Forbid();
        }

        var notifications = await this._notificationRepo.ListForUserAsync(userId, unreadOnly: true);
        foreach (var notification in notifications)
        {
            await this._notificationRepo.MarkAsReadAsync(notification.Id);
        }

        return this.RedirectToPage();
    }
}
