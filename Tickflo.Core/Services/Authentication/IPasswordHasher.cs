namespace Tickflo.Core.Services.Authentication;

public interface IPasswordHasher
{
    public string Hash(string input);
    public bool Verify(string input, string hash);
}

