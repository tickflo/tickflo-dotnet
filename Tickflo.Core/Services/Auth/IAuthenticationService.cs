using Tickflo.Core.Services.Auth;

namespace Tickflo.Core.Services.Auth;

public interface IAuthenticationService
{
    public Task<AuthenticationResult> AuthenticateAsync(string email, string password);
    public Task<AuthenticationResult> SignupAsync(string name, string email, string recoveryEmail, string workspaceName, string password);
}