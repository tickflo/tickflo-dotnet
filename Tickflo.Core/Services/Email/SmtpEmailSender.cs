using System.Net;
using System.Net.Mail;
using Tickflo.Core.Config;

namespace Tickflo.Core.Services.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly TickfloConfig _config;

    public SmtpEmailSender(TickfloConfig config)
    {
        _config = config;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var cfg = _config.EMAIL;
        using var client = new SmtpClient(cfg.SMTP_HOST, cfg.SMTP_PORT)
        {
            EnableSsl = cfg.SMTP_SSL,
            Credentials = new NetworkCredential(cfg.SMTP_USERNAME, cfg.SMTP_PASSWORD)
        };
        using var msg = new MailMessage
        {
            From = new MailAddress(cfg.FROM_ADDRESS, cfg.FROM_NAME),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        msg.To.Add(new MailAddress(toEmail));
        await client.SendMailAsync(msg);
    }
}
