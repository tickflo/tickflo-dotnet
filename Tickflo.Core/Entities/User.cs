namespace Tickflo.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using Tickflo.Core.Utils;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [Column("recoveryEmail")]
    public string? RecoveryEmail { get; set; }
    public bool SystemAdmin { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? EmailConfirmationCode { get; set; }
    public string? PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public User(string name, string email, string? recoveryEmail, string passwordHash)
    {
        this.Name = name;
        this.Email = email;
        this.RecoveryEmail = recoveryEmail;
        this.PasswordHash = passwordHash;
        this.EmailConfirmationCode = SecureTokenGenerator.GenerateToken(16);
    }

    internal User()
    {
    }
}
