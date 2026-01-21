namespace Tickflo.CoreTest.Services;

using System.Security.Claims;
using Xunit;

public class CurrentUserServiceTests
{
    [Fact]
    public void GetUserIdOrThrowThrowsWhenMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var svc = new CurrentUserService();
        Assert.Throws<InvalidOperationException>(() => svc.GetUserIdOrThrow(principal));
    }

    [Fact]
    public void TryGetUserIdSucceedsWhenClaimPresent()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "42")]));
        var svc = new CurrentUserService();
        Assert.True(svc.TryGetUserId(principal, out var id));
        Assert.Equal(42, id);
    }
}
