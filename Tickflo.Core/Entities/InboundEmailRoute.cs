namespace Tickflo.Core.Entities;

/// <summary>
/// Maps an inbound email address (e.g. support@tickflo.co) to a workspace.
/// Only emails matching a route are accepted; unmatched emails are rejected.
/// </summary>
public class InboundEmailRoute
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int? CreatedByUserId { get; set; }

    /// <summary>
    /// The local part of the email address (before the @).
    /// Example: "support" for support@tickflo.co.
    /// </summary>
    public string LocalPart { get; set; } = string.Empty;

    /// <summary>
    /// Display label for this route in the admin UI (e.g. "Support Inbox").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Optional default ticket type name assigned to tickets created via this route.
    /// If null, the workspace's default type is used.
    /// </summary>
    public string? DefaultTicketType { get; set; }

    /// <summary>
    /// Optional default ticket priority name assigned to tickets created via this route.
    /// If null, the workspace's default priority is used.
    /// </summary>
    public string? DefaultTicketPriority { get; set; }

    /// <summary>
    /// Optional location ID to assign tickets created via this route.
    /// If null, tickets are created without a location.
    /// </summary>
    public int? DefaultLocationId { get; set; }

    /// <summary>
    /// Whether this route is active. Inactive routes reject incoming emails.
    /// </summary>
    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
}
