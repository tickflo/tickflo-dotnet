using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Tickflo.Core.Data;

public class TokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ITokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;

    public TokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ITokenRepository tokenRepository,
        IUserRepository userRepository)
        : base(options, logger, encoder)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Read token from header or cookie
        var authHeader = Request.Headers.Authorization.ToString();
        var tokenValue = !string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ")
            ? authHeader["Bearer ".Length..].Trim()
            : Request.Cookies["user_token"];

        if (string.IsNullOrWhiteSpace(tokenValue))
            return AuthenticateResult.NoResult();

        var token = await _tokenRepository.FindByValueAsync(tokenValue);
        if (token == null)
            return AuthenticateResult.Fail("Invalid token");

        // Use TimeProvider instead of ISystemClock
        var now = Options.TimeProvider?.GetUtcNow() ?? TimeProvider.System.GetUtcNow();
        if (token.CreatedAt.AddSeconds(token.MaxAge) < now)
            return AuthenticateResult.Fail("Token expired");

        var user = await _userRepository.FindByIdAsync(token.UserId);
        if (user == null)
            return AuthenticateResult.Fail("User not found");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name.ToString()),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
