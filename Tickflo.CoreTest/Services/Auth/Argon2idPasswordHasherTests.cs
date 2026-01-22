namespace Tickflo.CoreTest.Services.Auth;

using Xunit;

public class Argon2idPasswordHasherTests
{
    [Fact]
    public void HashCreatesValidHash()
    {
        var hasher = new Argon2idPasswordHasher();
        var hash = hasher.Hash("password");

        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void VerifyPassesValidHash()
    {
        var hasher = new Argon2idPasswordHasher();
        var hash = hasher.Hash("password");

        Assert.True(hasher.Verify("password", hash));
    }

    [Fact]
    public void VerifyFailsInvalidHash()
    {
        var hasher = new Argon2idPasswordHasher();
        var hash = hasher.Hash("password");

        Assert.False(hasher.Verify("wrong", hash));
    }

    [Fact]
    public void HashProducesDifferentHashesForSamePassword()
    {
        var hasher = new Argon2idPasswordHasher();
        var hash1 = hasher.Hash("password");
        var hash2 = hasher.Hash("password");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyInvalidHashFormatReturnsFalse()
    {
        var hasher = new Argon2idPasswordHasher();
        var result = hasher.Verify("password", "not-a-real-hash");

        Assert.False(result);
    }
}

