using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Users;

public class ProfileModel : PageModel
{
    private readonly IUserRepository _userRepo;
    private readonly IUserNotificationPreferenceRepository _notificationPrefs;
    
    [BindProperty]
    public string UserId { get; set; } = "";
    [BindProperty]
    public string UserName { get; set; } = "";
    [BindProperty]
    public string Email { get; set; } = "";
    
    public List<NotificationPreferenceItem> NotificationPreferences { get; set; } = new();
    
    [BindProperty]
    public Dictionary<string, bool> EmailPrefs { get; set; } = new();
    [BindProperty]
    public Dictionary<string, bool> InAppPrefs { get; set; } = new();
    [BindProperty]
    public Dictionary<string, bool> SmsPrefs { get; set; } = new();
    [BindProperty]
    public Dictionary<string, bool> PushPrefs { get; set; } = new();

    public ProfileModel(IUserRepository userRepo, IUserNotificationPreferenceRepository notificationPrefs)
    {
        _userRepo = userRepo;
        _notificationPrefs = notificationPrefs;
    }

    public async Task OnGetAsync()
    {
        var user = HttpContext.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null && int.TryParse(userId, out var uid))
        {
            var u = await _userRepo.FindByIdAsync(uid);
            if (u != null)
            {
                UserId = u.Id.ToString();
                UserName = u.Name;
                Email = u.Email;
            }

            // Load notification preferences
            var existingPrefs = await _notificationPrefs.GetPreferencesForUserAsync(uid);
            var prefsByType = existingPrefs.ToDictionary(p => p.NotificationType, p => p);

            // Define all notification types with friendly labels
            var notificationTypes = new Dictionary<string, string>
            {
                { "workspace_invite", "Workspace Invitation" },
                { "ticket_assigned", "Ticket Assigned to You" },
                { "ticket_comment", "Comments on Your Tickets" },
                { "ticket_status_change", "Ticket Status Changes" },
                { "report_completed", "Report Completed" },
                { "mention", "Mentions in Comments" },
                { "ticket_summary", "Daily Ticket Summary" },
                { "password_reset", "Password Reset Confirmation" }
            };

            // Build preference items with defaults
            NotificationPreferences = new List<NotificationPreferenceItem>();
            foreach (var (type, label) in notificationTypes)
            {
                UserNotificationPreference? pref = null;
                prefsByType.TryGetValue(type, out pref);

                var item = new NotificationPreferenceItem
                {
                    Type = type,
                    Label = label,
                    EmailEnabled = pref?.EmailEnabled ?? true,
                    InAppEnabled = pref?.InAppEnabled ?? true,
                    SmsEnabled = pref?.SmsEnabled ?? false,
                    PushEnabled = pref?.PushEnabled ?? false
                };

                NotificationPreferences.Add(item);

                // Populate dictionaries for binding
                EmailPrefs[type] = item.EmailEnabled;
                InAppPrefs[type] = item.InAppEnabled;
                SmsPrefs[type] = item.SmsEnabled;
                PushPrefs[type] = item.PushEnabled;
            }
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = HttpContext.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null && int.TryParse(userId, out var uid))
        {
            // Update user profile
            var u = await _userRepo.FindByIdAsync(uid);
            if (u != null)
            {
                u.Name = UserName;
                u.Email = Email;
                await _userRepo.UpdateAsync(u);
            }

            // Update notification preferences
            var preferences = new List<UserNotificationPreference>();
            var allTypes = new[] { "workspace_invite", "ticket_assigned", "ticket_comment", 
                                  "ticket_status_change", "report_completed", "mention", 
                                  "ticket_summary", "password_reset" };

            foreach (var type in allTypes)
            {
                var pref = new UserNotificationPreference
                {
                    UserId = uid,
                    NotificationType = type,
                    EmailEnabled = EmailPrefs.ContainsKey(type) && EmailPrefs[type],
                    InAppEnabled = InAppPrefs.ContainsKey(type) && InAppPrefs[type],
                    SmsEnabled = SmsPrefs.ContainsKey(type) && SmsPrefs[type],
                    PushEnabled = PushPrefs.ContainsKey(type) && PushPrefs[type],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                preferences.Add(pref);
            }

            await _notificationPrefs.SavePreferencesAsync(preferences);
        }
        return RedirectToPage();
    }
}

public class NotificationPreferenceItem
{
    public string Type { get; set; } = "";
    public string Label { get; set; } = "";
    public bool EmailEnabled { get; set; }
    public bool InAppEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool PushEnabled { get; set; }
}
