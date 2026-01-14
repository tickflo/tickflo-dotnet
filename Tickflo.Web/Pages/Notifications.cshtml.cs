using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;

namespace Tickflo.Web.Pages;

public class NotificationTicketData
{
    public int ticketId { get; set; }
    public string? workspaceSlug { get; set; }
}

[Authorize]
public class NotificationsModel : PageModel
{
    private readonly INotificationRepository _notificationRepo;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsModel(
        INotificationRepository notificationRepo,
        ICurrentUserService currentUserService)
    {
        _notificationRepo = notificationRepo;
        _currentUserService = currentUserService;
    }

    public List<Notification> Notifications { get; set; } = new();

    public NotificationTicketData? GetTicketData(Notification notification)
    {
        if (string.IsNullOrEmpty(notification.Data))
            return null;

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
        if (!_currentUserService.TryGetUserId(User, out var userId))
        {
            return Forbid();
        }

        Notifications = await _notificationRepo.ListForUserAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
    {
        if (!_currentUserService.TryGetUserId(User, out var userId))
        {
            return Forbid();
        }

        var notification = await _notificationRepo.FindByIdAsync(id);
        if (notification == null || notification.UserId != userId)
        {
            return NotFound();
        }

        await _notificationRepo.MarkAsReadAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllAsReadAsync()
    {
        if (!_currentUserService.TryGetUserId(User, out var userId))
        {
            return Forbid();
        }

        var notifications = await _notificationRepo.ListForUserAsync(userId, unreadOnly: true);
        foreach (var notification in notifications)
        {
            await _notificationRepo.MarkAsReadAsync(notification.Id);
        }

        return RedirectToPage();
    }
}
