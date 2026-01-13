using Microsoft.Extensions.Logging;

namespace Tickflo.Core.Services.Email;

public class DebugEmailSender(ILogger<DebugEmailSender> logger) : IEmailSender
{
    private readonly ILogger<DebugEmailSender> _logger = logger;

    public Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        _logger.LogInformation("[Email] To: {To}\nSubject: {Subject}\nBody:\n{Body}", toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }
}
