namespace Tickflo.Web.Authentication;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Tickflo.Core.Data;

public class TokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ITokenRepository tokenRepository,
    IUserRepository userRepository) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly ITokenRepository tokenRepository = tokenRepository;
    private readonly IUserRepository userRepository = userRepository;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Read token from header or cookie
        var authHeader = this.Request.Headers.Authorization.ToString();
        var tokenValue = !string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.Ordinal)
            ? authHeader["Bearer ".Length..].Trim()
            : this.Request.Cookies["user_token"];

        if (string.IsNullOrWhiteSpace(tokenValue))
        {
            return AuthenticateResult.NoResult();
        }

        var token = await this.tokenRepository.FindByValueAsync(tokenValue);
        if (token == null)
        {
            return AuthenticateResult.Fail("Invalid token");
        }

        // Use TimeProvider instead of ISystemClock
        var now = this.Options.TimeProvider?.GetUtcNow() ?? TimeProvider.System.GetUtcNow();
        if (token.CreatedAt.AddSeconds(token.MaxAge) < now)
        {
            return AuthenticateResult.Fail("Token expired");
        }

        var user = await this.userRepository.FindByIdAsync(token.UserId);
        if (user == null)
        {
            return AuthenticateResult.Fail("User not found");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, $"{user.Id}"),
            new Claim(ClaimTypes.Name, user.Name),
        };

        var identity = new ClaimsIdentity(claims, this.Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, this.Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
