namespace Tickflo.Core.Entities;

public class EmailTemplate
{
    public int Id { get; set; }
    public int? WorkspaceId { get; set; }
    public int TemplateTypeId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
