namespace Tickflo.Core.Config;

public class TickfloConfig
{
    public string POSTGRES_USER { get; set; } = string.Empty;
    public string POSTGRES_PASSWORD { get; set; } = string.Empty;
    public string POSTGRES_DB { get; set; } = string.Empty;
    public string POSTGRES_HOST { get; set; } = string.Empty;
    public string S3_ENDPOINT { get; set; } = string.Empty;
    public string S3_ACCESS_KEY { get; set; } = string.Empty;
    public string S3_SECRET_KEY { get; set; } = string.Empty;
    public string S3_BUCKET { get; set; } = string.Empty;
    public string S3_REGION { get; set; } = string.Empty;
    public int SESSION_TIMEOUT_MINUTES { get; set; }
    public UserConfig USER { get; set; } = new();
    public ContactConfig CONTACT { get; set; } = new();
    public LocationConfig LOCATION { get; set; } = new();
    public RoleConfig ROLE { get; set; } = new();
    public WorkspaceConfig WORKSPACE { get; set; } = new();
    public PortalConfig PORTAL { get; set; } = new();
}

public class UserConfig
{
    public int MIN_NAME_LENGTH { get; set; }
    public int MAX_NAME_LENGTH { get; set; }
    public int CHANGE_EMAIL_CONFIRM_TIMEOUT_MINUTES { get; set; }
    public int CHANGE_EMAIL_UNDO_TIMEOUT_MINUTES { get; set; }
}

public class LocationConfig
{
    public int MIN_NAME_LENGTH { get; set; }
    public int MAX_NAME_LENGTH { get; set; }
}

public class ContactConfig
{
    public int MIN_NAME_LENGTH { get; set; }
    public int MAX_NAME_LENGTH { get; set; }
}

public class WorkspaceConfig
{
    public int MIN_NAME_LENGTH { get; set; }
    public int MAX_NAME_LENGTH { get; set; }
    public int MAX_SLUG_LENGTH { get; set; }
}

public class PortalConfig
{
    public int MIN_NAME_LENGTH { get; set; }
    public int MAX_NAME_LENGTH { get; set; }
    public int MAX_SLUG_LENGTH { get; set; }
    public int MIN_SECTION_TITLE_LENGTH { get; set; }
    public int MAX_SECTION_TITLE_LENGTH { get; set; }
    public int MIN_QUESTION_LABEL_LENGTH { get; set; }
    public int MAX_QUESTION_LABEL_LENGTH { get; set; }
}

public class RoleConfig
{
    public int MIN_NAME_LENGTH { get; set; }
    public int MAX_NAME_LENGTH { get; set; }
}