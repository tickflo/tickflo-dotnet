namespace Tickflo.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

[Table("permissions")]
#pragma warning disable CA1711
public class Permission
#pragma warning restore CA1711
{
    public int Id { get; set; }
    public string Resource { get; set; } = string.Empty; // e.g., "tickets", "contacts", "tickets_scope"
    public string Action { get; set; } = string.Empty;   // e.g., "view", "edit", "create" or "all"|"mine"|"team" for tickets_scope
}
