namespace Tickflo.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class UserNotificationPreference
{
    public int UserId { get; set; }

    [Column("notification_type")]
    public string NotificationType { get; set; } = string.Empty;

    [Column("email_enabled")]
    public bool EmailEnabled { get; set; } = true;

    [Column("in_app_enabled")]
    public bool InAppEnabled { get; set; } = true;

    [Column("sms_enabled")]
    public bool SmsEnabled { get; set; } = false;

    [Column("push_enabled")]
    public bool PushEnabled { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
