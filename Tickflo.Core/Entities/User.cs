namespace Tickflo.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [Column("recoveryEmail")]
    public string? RecoveryEmail { get; set; } = string.Empty;
    public bool SystemAdmin { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? EmailConfirmationCode { get; set; } = string.Empty;
    public string? PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
