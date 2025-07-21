using Tickflo.Core.Data;
using Tickflo.Core.Services.Auth;

namespace Tickflo.Core.Services;

public class AuthenticationService(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenRepository tokenRepository) : IAuthenticationService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ITokenRepository _tokenRepository = tokenRepository;

    public async Task<AuthenticationResult> AuthenticateAsync(string email, string password)
    {
        var result = new AuthenticationResult();
        var user = await _userRepository.FindByEmailAsync(email);
        if (user == null || user.PasswordHash == null)
        {
            result.ErrorMessage = "Invalid username or password, please try again";
            return result;
        }

        var isValid = _passwordHasher.Verify($"{email}{password}", user.PasswordHash);
        if (!isValid)
        {
            result.ErrorMessage = "Invalid username or password, please try again";
            return result;
        }

        var token = await _tokenRepository.CreateForUserIdAsync(user.Id);

        result.Success = true;
        result.UserId = user.Id;
        result.Token = token.Value;
        return result;
    }
}