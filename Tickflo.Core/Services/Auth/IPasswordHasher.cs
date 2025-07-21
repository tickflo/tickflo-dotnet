namespace Tickflo.Core.Services.Auth;

public interface IPasswordHasher
{
    public string Hash(string input);
    public bool Verify(string input, string hash);
}