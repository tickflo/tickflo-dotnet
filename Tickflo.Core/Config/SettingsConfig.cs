namespace Tickflo.Core.Config;

public class SettingsConfig
{
    public ThemeSettings Theme { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public DisplaySettings Display { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public PrivacySettings Privacy { get; set; } = new();
}

public class ThemeSettings
{
    public string Default { get; set; } = "light";
    public List<string> AvailableOptions { get; set; } = new();
}

public class NotificationSettings
{
    public bool EmailOnTicketAssigned { get; set; } = true;
    public bool EmailOnTicketCommented { get; set; } = true;
    public bool EmailOnTicketUpdated { get; set; } = true;
    public bool EmailOnTeamInvite { get; set; } = true;
    public string DigestFrequency { get; set; } = "daily";
    public List<string> AvailableFrequencies { get; set; } = new();
}

public class DisplaySettings
{
    public int ItemsPerPage { get; set; } = 25;
    public string DateFormat { get; set; } = "YYYY-MM-DD";
    public string TimeFormat { get; set; } = "HH:mm";
    public List<int> AvailablePageSizes { get; set; } = new();
}

public class SecuritySettings
{
    public bool TwoFactorEnabled { get; set; }
    public int? SessionTimeoutOverrideMinutes { get; set; }
    public int LastPasswordChangeRequiredDays { get; set; } = 90;
}

public class PrivacySettings
{
    public bool ShowOnlineStatus { get; set; } = true;
    public string AllowProfileVisibility { get; set; } = "workspace";
}
