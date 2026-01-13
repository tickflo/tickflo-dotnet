using Moq;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Contacts;
using Tickflo.Core.Services.Export;
using Tickflo.Core.Services.Inventory;
using Tickflo.Core.Services.Locations;
using Tickflo.Core.Services.Notifications;
using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Roles;
using Tickflo.Core.Services.Storage;
using Tickflo.Core.Services.Teams;
using Tickflo.Core.Services.Tickets;
using Tickflo.Core.Services.Users;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class CurrentUserServiceTests
{
    [Fact]
    public void GetUserIdOrThrow_Throws_WhenMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var svc = new CurrentUserService();
        Assert.Throws<InvalidOperationException>(() => svc.GetUserIdOrThrow(principal));
    }

    [Fact]
    public void TryGetUserId_Succeeds_WhenClaimPresent()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42") }));
        var svc = new CurrentUserService();
        Assert.True(svc.TryGetUserId(principal, out var id));
        Assert.Equal(42, id);
    }
}
