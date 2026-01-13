
using Tickflo.Core.Services.Authentication;
using Xunit;

namespace Tickflo.CoreTest.Services.Auth;

public class Argon2idPasswordHasherTests
{
    [Fact]
    public void Hash_CreatesValidHash()
    {
        var hasher = new Argon2idPasswordHasher();
        var hash = hasher.Hash("password");

        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void Verify_PassesValidHash()
    {
        var hasher = new Argon2idPasswordHasher();
        var hash = hasher.Hash("password");

        Assert.True(hasher.Verify("password", hash));
    }

    [Fact]
    public void Verify_FailsInvalidHash()
    {
        var hasher = new Argon2idPasswordHasher();
        var hash = hasher.Hash("password");

        Assert.False(hasher.Verify("wrong", hash));
    }

    [Fact]
    public void Hash_ProducesDifferentHashesForSamePassword()
    {
        var hasher = new Argon2idPasswordHasher();
        var hash1 = hasher.Hash("password");
        var hash2 = hasher.Hash("password");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_InvalidHashFormat_ReturnsFalse()
    {
        var hasher = new Argon2idPasswordHasher();
        var result = hasher.Verify("password", "not-a-real-hash");

        Assert.False(result);
    }
}

