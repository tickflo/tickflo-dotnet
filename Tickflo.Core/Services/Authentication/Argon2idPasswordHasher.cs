using Isopoh.Cryptography.Argon2;

namespace Tickflo.Core.Services.Authentication;

public class Argon2idPasswordHasher : IPasswordHasher
{
    public string Hash(string input)
    {
        return Argon2.Hash(input);
    }

    public bool Verify(string input, string hash)
    {
        return Argon2.Verify(hash, input);
    }
}

