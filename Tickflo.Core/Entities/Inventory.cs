namespace Tickflo.Core.Entities;

public class Inventory
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public int? LocationId { get; set; }
    public int? MinStock { get; set; }
    public decimal Cost { get; set; }
    public decimal? Price { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public string Status { get; set; } = "active";
    public string? Supplier { get; set; }
    public DateTime? LastRestockAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
