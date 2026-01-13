using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Entities;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class ValidationServiceTests
{
    [Fact]
    public async Task ValidateEmailAsync_Fails_WhenDuplicate()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.FindByEmailAsync("dup@example.com")).ReturnsAsync(new User { Id = 1 });
        var roles = Mock.Of<IRoleRepository>();
        var teams = Mock.Of<ITeamRepository>();
        var svc = new ValidationService(users.Object, roles, teams);

        var result = await svc.ValidateEmailAsync("dup@example.com", checkUniqueness: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "Email");
    }

    [Fact]
    public void ValidateTicketSubject_Fails_WhenEmpty()
    {
        var svc = new ValidationService(Mock.Of<IUserRepository>(), Mock.Of<IRoleRepository>(), Mock.Of<ITeamRepository>());
        var result = svc.ValidateTicketSubject("");
        Assert.False(result.IsValid);
    }
}
