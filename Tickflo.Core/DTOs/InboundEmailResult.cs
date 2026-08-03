namespace Tickflo.Core.DTOs;

/// <summary>
/// Result of processing an inbound email through the pipeline.
/// </summary>
public class InboundEmailResult
{
    /// <summary>
    /// Whether processing succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The ID of the inbound email record.
    /// </summary>
    public int InboundEmailId { get; set; }

    /// <summary>
    /// The ID of the created ticket, if successful.
    /// </summary>
    public int? TicketId { get; set; }

    /// <summary>
    /// User-facing message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The final processing status.
    /// </summary>
    public string Status { get; set; } = "Pending";

    public static InboundEmailResult SuccessResult(int inboundEmailId, int ticketId)
        => new()
        {
            Success = true,
            InboundEmailId = inboundEmailId,
            TicketId = ticketId,
            Status = "Processed",
            Message = "Email processed successfully",
        };

    public static InboundEmailResult RejectedResult(int inboundEmailId, string reason)
        => new()
        {
            Success = false,
            InboundEmailId = inboundEmailId,
            Status = "Rejected",
            Message = reason,
        };

    public static InboundEmailResult FailedResult(int inboundEmailId, string error)
        => new()
        {
            Success = false,
            InboundEmailId = inboundEmailId,
            Status = "Failed",
            Message = error,
        };

    public static InboundEmailResult DuplicateResult(string reason)
        => new()
        {
            Success = false,
            Status = "Rejected",
            Message = reason,
        };
}
