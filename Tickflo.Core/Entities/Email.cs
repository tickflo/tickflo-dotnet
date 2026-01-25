namespace Tickflo.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class Email
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    [Column(TypeName = "json")]
    public Dictionary<string, string>? Vars { get; set; } = null;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
