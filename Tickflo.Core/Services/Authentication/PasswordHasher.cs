namespace Tickflo.Core.Services.Authentication;

using Isopoh.Cryptography.Argon2;

public interface IPasswordHasher
{
    public string Hash(string input);
    public bool Verify(string input, string hash);
}

public class Argon2idPasswordHasher : IPasswordHasher
{
    public string Hash(string input) => Argon2.Hash(input);

    public bool Verify(string input, string hash) => Argon2.Verify(hash, input);
}

