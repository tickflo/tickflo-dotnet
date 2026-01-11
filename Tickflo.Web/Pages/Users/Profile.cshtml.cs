using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Users;

public class ProfileModel : PageModel
{
    private readonly IUserRepository _userRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    
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

    public ProfileModel(
        IUserRepository userRepo,
        ICurrentUserService currentUserService,
        INotificationPreferenceService notificationPreferenceService)
    {
        _userRepo = userRepo;
        _currentUserService = currentUserService;
        _notificationPreferenceService = notificationPreferenceService;
    }

    public async Task OnGetAsync()
    {
        if (!_currentUserService.TryGetUserId(User, out var uid)) return;

        var user = await _userRepo.FindByIdAsync(uid);
        if (user == null) return;

        UserId = user.Id.ToString();
        UserName = user.Name;
        Email = user.Email;

        // Get notification preferences - service handles defaults and initialization
        var prefs = await _notificationPreferenceService.GetUserPreferencesAsync(uid);
        var definitions = _notificationPreferenceService.GetNotificationTypeDefinitions();
        var prefsByType = prefs.ToDictionary(p => p.NotificationType, p => p);

        NotificationPreferences = new List<NotificationPreferenceItem>();
        foreach (var definition in definitions)
        {
            prefsByType.TryGetValue(definition.Type, out var pref);

            var item = new NotificationPreferenceItem
            {
                Type = definition.Type,
                Label = definition.Label,
                EmailEnabled = pref?.EmailEnabled ?? true,
                InAppEnabled = pref?.InAppEnabled ?? true,
                SmsEnabled = pref?.SmsEnabled ?? false,
                PushEnabled = pref?.PushEnabled ?? false
            };

            NotificationPreferences.Add(item);

            EmailPrefs[definition.Type] = item.EmailEnabled;
            InAppPrefs[definition.Type] = item.InAppEnabled;
            SmsPrefs[definition.Type] = item.SmsEnabled;
            PushPrefs[definition.Type] = item.PushEnabled;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!_currentUserService.TryGetUserId(User, out var uid))
        {
            return RedirectToPage();
        }

        // Update user profile
        var user = await _userRepo.FindByIdAsync(uid);
        if (user != null)
        {
            user.Name = UserName;
            user.Email = Email;
            await _userRepo.UpdateAsync(user);
        }

        // Collect and save preferences
        var definitions = _notificationPreferenceService.GetNotificationTypeDefinitions();
        var preferences = new List<UserNotificationPreference>();
        
        foreach (var definition in definitions)
        {
            var pref = new UserNotificationPreference
            {
                UserId = uid,
                NotificationType = definition.Type,
                EmailEnabled = EmailPrefs.ContainsKey(definition.Type) && EmailPrefs[definition.Type],
                InAppEnabled = InAppPrefs.ContainsKey(definition.Type) && InAppPrefs[definition.Type],
                SmsEnabled = SmsPrefs.ContainsKey(definition.Type) && SmsPrefs[definition.Type],
                PushEnabled = PushPrefs.ContainsKey(definition.Type) && PushPrefs[definition.Type],
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            preferences.Add(pref);
        }

        await _notificationPreferenceService.SavePreferencesAsync(uid, preferences);
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
