using Tickflo.Core.Services.Auth;

namespace Tickflo.Core.Services;

public interface IAuthenticationService
{
    public Task<AuthenticationResult> AuthenticateAsync(string email, string password);
}