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
    public int SessionTimeoutMinutes { get; set; }
    public UserConfig User { get; set; } = new();
    public ContactConfig Contact { get; set; } = new();
    public LocationConfig Location { get; set; } = new();
    public RoleConfig Role { get; set; } = new();
    public WorkspaceConfig Workspace { get; set; } = new();
    public EmailConfig Email { get; set; } = new();
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
    public string FromAddress { get; set; } = "no-reply@tickflo.local";
    public string FromName { get; set; } = "Tickflo";
}
