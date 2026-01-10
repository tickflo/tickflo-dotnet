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

    private static readonly (string Type, string Label)[] NotificationTypes = new[]
    {
        ("workspace_invite", "Workspace Invitation"),
        ("ticket_assigned", "Ticket Assigned to You"),
        ("ticket_comment", "Comments on Your Tickets"),
        ("ticket_status_change", "Ticket Status Changes"),
        ("report_completed", "Report Completed"),
        ("mention", "Mentions in Comments"),
        ("ticket_summary", "Daily Ticket Summary"),
        ("password_reset", "Password Reset Confirmation"),
    };
    
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
        if (!TryGetUserId(out var uid)) return;

        var user = await _userRepo.FindByIdAsync(uid);
        if (user == null) return;

        UserId = user.Id.ToString();
        UserName = user.Name;
        Email = user.Email;

        var existingPrefs = await _notificationPrefs.GetPreferencesForUserAsync(uid);
        var prefsByType = existingPrefs.ToDictionary(p => p.NotificationType, p => p);

        NotificationPreferences = new List<NotificationPreferenceItem>(NotificationTypes.Length);
        foreach (var (type, label) in NotificationTypes)
        {
            prefsByType.TryGetValue(type, out var pref);

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

            EmailPrefs[type] = item.EmailEnabled;
            InAppPrefs[type] = item.InAppEnabled;
            SmsPrefs[type] = item.SmsEnabled;
            PushPrefs[type] = item.PushEnabled;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!TryGetUserId(out var uid))
        {
            return RedirectToPage();
        }

        var user = await _userRepo.FindByIdAsync(uid);
        if (user != null)
        {
            user.Name = UserName;
            user.Email = Email;
            await _userRepo.UpdateAsync(user);
        }

        var preferences = new List<UserNotificationPreference>(NotificationTypes.Length);
        foreach (var (type, _) in NotificationTypes)
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
        return RedirectToPage();
    }

    private bool TryGetUserId(out int userId)
    {
        var idValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idValue, out userId))
        {
            return true;
        }

        userId = default;
        return false;
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
