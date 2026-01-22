namespace Tickflo.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

[Table("role_permissions")]
public class RolePermissionLink
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
