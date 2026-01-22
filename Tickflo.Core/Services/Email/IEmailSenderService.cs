namespace Tickflo.Core.Services.Email;

public interface IEmailSenderService
{
    public Task SendAsync(string toEmail, string subject, string htmlBody);
}
