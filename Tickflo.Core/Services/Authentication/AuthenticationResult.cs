namespace Tickflo.Core.Services.Authentication;

public class AuthenticationResult
{
    public int UserId { get; set; }
    public required string Token { get; set; }
}

