namespace Tickflo.Core.Services.Email;

public class EmailSenderService : IEmailSenderService
{
    public Task SendAsync(string toEmail, string subject, string htmlBody) => throw new NotImplementedException();
}
