namespace Tickflo.Core.Config;

public class TickfloConfig
{
    public string PostgresUser { get; set; } = string.Empty;
    public string PostgresPassword { get; set; } = string.Empty;
    public string PostresDatabase { get; set; } = string.Empty;
    public string PostgresHost { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://app.tickflo.co";
    public string S3EndPoint { get; set; } = string.Empty;
    public string S3AccessKey { get; set; } = string.Empty;
    public string S3SecretKey { get; set; } = string.Empty;
    public string S3Bucket { get; set; } = string.Empty;
    public string S3Region { get; set; } = string.Empty;
    public string MailgunApiKey { get; set; } = string.Empty;
    public string AppEnv { get; set; } = "Production";
    public int SessionTimeoutMinutes { get; set; }
    public string SessionCookieName { get; set; } = "tickflo_session";
    public int PasswordResetTokenMaxAgeSeconds { get; set; } = 60 * 60;
    public UserConfig User { get; set; } = new();
    public ContactConfig Contact { get; set; } = new();
    public LocationConfig Location { get; set; } = new();
    public RoleConfig Role { get; set; } = new();
    public WorkspaceConfig Workspace { get; set; } = new();
    public EmailConfig Email { get; set; } = new();
    public InboundEmailConfig InboundEmail { get; set; } = new();
}

public class UserConfig
{
    public int MinNameLength { get; set; }
    public int MaxNameLength { get; set; }
    public int ChangeEmailConfirmTimeoutMinutes { get; set; }
    public int ChangeEmailUndoTimeoutMinutes { get; set; }
}

public class LocationConfig
{
    public int MinNameLength { get; set; }
    public int MaxNameLength { get; set; }
}

public class ContactConfig
{
    public int MinNameLength { get; set; }
    public int MaxNameLength { get; set; }
}

public class WorkspaceConfig
{
    public int MinNameLength { get; set; }
    public int MaxNameLength { get; set; }
    public int MaxSlugLength { get; set; }
}

public class RoleConfig
{
    public int MinNameLength { get; set; }
    public int MaxNameLength { get; set; }
}

public class EmailConfig
{
    public string FromAddress { get; set; } = "no-reply@tickflo.co";
    public string FromName { get; set; } = "Tickflo";
    public int BatchSize { get; set; } = 100;
    public string MailgunDomain { get; set; } = "tickflo.co";
    public string MailgunApiBaseUrl { get; set; } = "https://api.mailgun.net/";
}

public class InboundEmailConfig
{
    /// <summary>
    /// Mailgun API key used to verify incoming webhook HMAC signatures.
    /// </summary>
    public string MailgunApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The email domain for inbound email (e.g. inbound.tickflo.co).
    /// Mailgun will route email for this domain to the webhook.
    /// Use a subdomain to avoid conflicting with existing email hosting.
    /// </summary>
    public string Domain { get; set; } = "inbound.tickflo.co";

    /// <summary>
    /// Secret used to validate Mailgun webhook HMAC signatures.
    /// This is stored in Mailgun's webhook settings.
    /// </summary>
    public string WebhookSigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Maximum attachment size in bytes (default 25 MB).
    /// </summary>
    public long MaxAttachmentSize { get; set; } = 25 * 1024 * 1024;

    /// <summary>
    /// Comma-separated allowed attachment MIME types.
    /// Empty means all types allowed (within size limit).
    /// </summary>
    public string AllowedMimeTypes { get; set; } = string.Empty;

    /// <summary>
    /// The inbox address that receives automated/error bounces.
    /// </summary>
    public string BounceAddress { get; set; } = "bounces@tickflo.co";
}
