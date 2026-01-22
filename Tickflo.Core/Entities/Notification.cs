namespace Tickflo.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class Notification
{
    public int Id { get; set; }

    [Column("workspace_id")]
    public int? WorkspaceId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("type")]
    public string Type { get; set; } = string.Empty; // email, sms, push, in_app

    [Column("delivery_method")]
    public string DeliveryMethod { get; set; } = "email"; // email, sms, push, in_app, batch

    [Column("priority")]
    public string Priority { get; set; } = "normal"; // low, normal, high, urgent

    [Column("subject")]
    public string Subject { get; set; } = string.Empty;

    [Column("body")]
    public string Body { get; set; } = string.Empty;

    [Column("data")]
    public string? Data { get; set; } // JSON metadata

    [Column("status")]
    public string Status { get; set; } = "pending"; // pending, sent, failed, cancelled

    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    [Column("failed_at")]
    public DateTime? FailedAt { get; set; }

    [Column("failure_reason")]
    public string? FailureReason { get; set; }

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("scheduled_for")]
    public DateTime? ScheduledFor { get; set; }

    [Column("batch_id")]
    public string? BatchId { get; set; } // For grouping notifications to send together
}
