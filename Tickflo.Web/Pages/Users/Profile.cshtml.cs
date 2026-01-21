namespace Tickflo.Web.Pages.Users;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using Tickflo.Core.Services.Common;

public class ProfileModel(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    INotificationPreferenceService notificationPreferenceService) : PageModel
{
    private readonly IUserRepository userRepository = userRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly INotificationPreferenceService _notificationPreferenceService = notificationPreferenceService;

    [BindProperty]
    public string UserId { get; set; } = "";
    [BindProperty]
    public string UserName { get; set; } = "";
    [BindProperty]
    public string Email { get; set; } = "";

    public List<NotificationPreferenceItem> NotificationPreferences { get; set; } = [];

    [BindProperty]
    public Dictionary<string, bool> EmailPrefs { get; set; } = [];
    [BindProperty]
    public Dictionary<string, bool> InAppPrefs { get; set; } = [];
    [BindProperty]
    public Dictionary<string, bool> SmsPrefs { get; set; } = [];
    [BindProperty]
    public Dictionary<string, bool> PushPrefs { get; set; } = [];

    public async Task OnGetAsync()
    {
        if (!this._currentUserService.TryGetUserId(this.User, out var uid))
        {
            return;
        }

        var user = await this.userRepository.FindByIdAsync(uid);
        if (user == null)
        {
            return;
        }

        this.UserId = user.Id.ToString();
        this.UserName = user.Name;
        this.Email = user.Email;

        // Get notification preferences - service handles defaults and initialization
        var prefs = await this._notificationPreferenceService.GetUserPreferencesAsync(uid);
        var definitions = this._notificationPreferenceService.GetNotificationTypeDefinitions();
        var prefsByType = prefs.ToDictionary(p => p.NotificationType, p => p);

        this.NotificationPreferences = [];
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

            this.NotificationPreferences.Add(item);

            this.EmailPrefs[definition.Type] = item.EmailEnabled;
            this.InAppPrefs[definition.Type] = item.InAppEnabled;
            this.SmsPrefs[definition.Type] = item.SmsEnabled;
            this.PushPrefs[definition.Type] = item.PushEnabled;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!this._currentUserService.TryGetUserId(this.User, out var uid))
        {
            return this.RedirectToPage();
        }

        // Update user profile
        var user = await this.userRepository.FindByIdAsync(uid);
        if (user != null)
        {
            user.Name = this.UserName;
            user.Email = this.Email;
            await this.userRepository.UpdateAsync(user);
        }

        // Collect and save preferences
        var definitions = this._notificationPreferenceService.GetNotificationTypeDefinitions();
        var preferences = new List<UserNotificationPreference>();

        foreach (var definition in definitions)
        {
            var pref = new UserNotificationPreference
            {
                UserId = uid,
                NotificationType = definition.Type,
                EmailEnabled = this.EmailPrefs.ContainsKey(definition.Type) && this.EmailPrefs[definition.Type],
                InAppEnabled = this.InAppPrefs.ContainsKey(definition.Type) && this.InAppPrefs[definition.Type],
                SmsEnabled = this.SmsPrefs.ContainsKey(definition.Type) && this.SmsPrefs[definition.Type],
                PushEnabled = this.PushPrefs.ContainsKey(definition.Type) && this.PushPrefs[definition.Type],
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            preferences.Add(pref);
        }

        await this._notificationPreferenceService.SavePreferencesAsync(uid, preferences);
        return this.RedirectToPage();
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

