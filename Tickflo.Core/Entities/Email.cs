namespace Tickflo.Core.Entities;

public class Email
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public Dictionary<string, string> Vars { get; set; } = [];
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
