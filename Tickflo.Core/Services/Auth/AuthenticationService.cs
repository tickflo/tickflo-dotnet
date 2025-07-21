using Tickflo.Core.Data;
using Tickflo.Core.Services.Auth;

namespace Tickflo.Core.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthenticationService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string email, string password)
    {
        var result = new AuthenticationResult();
        var user = await _userRepository.FindByEmailAsync(email);
        if (user == null || user.PasswordHash == null)
        {
            result.ErrorMessage = "Invalid username or password, please try again";
            return result;
        }

        bool isValid = _passwordHasher.Verify($"{email}{password}", user.PasswordHash);
        if (!isValid)
        {
            result.ErrorMessage = "Invalid username or password, please try again";
            return result;
        }

        result.Success = true;
        result.UserId = user.Id;
        return result;
    }
}