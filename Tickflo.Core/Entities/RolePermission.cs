namespace Tickflo.Core.Entities;

public class RolePermission
{
    public int RoleId { get; set; }
    public string Section { get; set; } = string.Empty; // e.g., "tickets", "contacts", etc.
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
    public bool CanCreate { get; set; }
    // For tickets section only: view scope control ("all", "mine", "team")
    public string? TicketViewScope { get; set; }
}
