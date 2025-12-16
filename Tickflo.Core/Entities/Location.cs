namespace Tickflo.Core.Entities;

public class Location
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
}
