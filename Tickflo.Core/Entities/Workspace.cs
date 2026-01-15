namespace Tickflo.Core.Entities;

public class Workspace
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    
    // Portal settings
    public bool PortalEnabled { get; set; } = false;
    public string? PortalAccessToken { get; set; } // Unique token for the workspace portal
}
