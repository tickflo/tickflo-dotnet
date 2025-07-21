namespace Tickflo.Core.Services.Auth;

public class AuthenticationResult
{
    public bool Success { get; set; }
    public int? UserId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Token { get; set; }
}