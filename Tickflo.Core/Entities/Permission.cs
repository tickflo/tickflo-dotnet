using System.ComponentModel.DataAnnotations.Schema;
namespace Tickflo.Core.Entities;

[Table("permissions")]
public class Permission
{
    public int Id { get; set; }
    public string Resource { get; set; } = string.Empty; // e.g., "tickets", "contacts", "tickets_scope"
    public string Action { get; set; } = string.Empty;   // e.g., "view", "edit", "create" or "all"|"mine"|"team" for tickets_scope
}
