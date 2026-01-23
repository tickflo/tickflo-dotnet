namespace Tickflo.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

public class Email
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    
    [Column("vars")]
    public string VarsJson { get; set; } = "{}";
    
    [NotMapped]
    public Dictionary<string, string> Vars
    {
        get => string.IsNullOrWhiteSpace(this.VarsJson) 
            ? [] 
            : JsonSerializer.Deserialize<Dictionary<string, string>>(this.VarsJson) ?? [];
        set => this.VarsJson = JsonSerializer.Serialize(value);
    }
    
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
