namespace Tickflo.Core.Services.Email;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Validates Mailgun webhook HMAC-SHA256 signatures.
/// Mailgun computes signature = HMAC-SHA256(signing_key, timestamp + token).
/// </summary>
public interface IInboundEmailHMACValidator
{
    /// <summary>
    /// Validates a Mailgun webhook signature.
    /// </summary>
    /// <param name="timestamp">Mailgun signature timestamp from the webhook payload</param>
    /// <param name="token">Mailgun signature token from the webhook payload</param>
    /// <param name="signature">Mailgun signature value to verify against</param>
    /// <param name="signingKey">The Mailgun webhook signing key</param>
    /// <returns>True if the computed HMAC matches the provided signature</returns>
    public bool Validate(string timestamp, string token, string signature, string signingKey);
}

/// <inheritdoc />
public class InboundEmailHMACValidator : IInboundEmailHMACValidator
{
    /// <inheritdoc />
    public bool Validate(string timestamp, string token, string signature, string signingKey)
    {
        if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(signingKey))
        {
            return false;
        }

        var data = timestamp + token;
        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        var computedHash = HMACSHA256.HashData(keyBytes, dataBytes);
        var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }
}
