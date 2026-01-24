namespace Tickflo.Core.Services.Authentication;

public interface IAuthenticationService
{
    public Task<AuthenticationResult> AuthenticateAsync(string email, string password);
    public Task<AuthenticationResult> SignupAsync(string name, string email, string recoveryEmail, string workspaceName, string password);
    public Task<AuthenticationResult> SignupInviteeAsync(string name, string email, string recoveryEmail, string password);
    public Task ResendEmailConfirmationAsync(int userId);
}


