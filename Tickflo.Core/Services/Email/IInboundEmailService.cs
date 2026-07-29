namespace Tickflo.Core.Services.Email;

using Tickflo.Core.DTOs;

/// <summary>
/// Processes inbound emails from the Mailgun webhook through the full pipeline:
/// validate → parse → deduplicate → route → resolve contact → download attachments →
/// create ticket → send confirmation.
/// </summary>
public interface IInboundEmailService
{
    /// <summary>
    /// Processes an inbound email payload.
    /// </summary>
    /// <param name="payload">Parsed Mailgun webhook payload</param>
    /// <param name="attachmentStreams">
    /// Streams for attachments included in the webhook request.
    /// Key is the attachment filename; value is a tuple of (stream, content-type, size).
    /// Null if there are no attachments.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of processing</returns>
    public Task<InboundEmailResult> ProcessAsync(
        InboundEmailPayload payload,
        Dictionary<string, (Stream Stream, string ContentType, long Size)>? attachmentStreams,
        CancellationToken cancellationToken = default);
}
