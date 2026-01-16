using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Entities;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class ValidationServiceTests
{
    private static IValidationService CreateService(
        IUserRepository? userRepo = null,
        IRoleRepository? roleRepo = null,
        ITeamRepository? teamRepo = null)
    {
        userRepo ??= Mock.Of<IUserRepository>();
        roleRepo ??= Mock.Of<IRoleRepository>();
        teamRepo ??= Mock.Of<ITeamRepository>();
        return new ValidationService(userRepo, roleRepo, teamRepo);
    }

    [Fact]
    public async Task ValidateEmailAsync_Succeeds_WithValidEmail()
    {
        var svc = CreateService();
        var result = await svc.ValidateEmailAsync("test@example.com");
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateEmailAsync_Fails_WhenEmpty()
    {
        var svc = CreateService();
        var result = await svc.ValidateEmailAsync("");
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateEmailAsync_Fails_WhenInvalidFormat()
    {
        var svc = CreateService();
        var result = await svc.ValidateEmailAsync("not-an-email");
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateEmailAsync_Fails_WhenDuplicate()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.FindByEmailAsync("dup@example.com")).ReturnsAsync(new User { Id = 1 });
        var svc = CreateService(users.Object);

        var result = await svc.ValidateEmailAsync("dup@example.com", checkUniqueness: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "Email");
    }

    [Fact]
    public void ValidateTicketSubject_Succeeds_WithValidSubject()
    {
        var svc = CreateService();
        var result = svc.ValidateTicketSubject("Valid Subject");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateTicketSubject_Fails_WhenEmpty()
    {
        var svc = CreateService();
        var result = svc.ValidateTicketSubject("");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateTicketSubject_Fails_WhenTooLong()
    {
        var svc = CreateService();
        var longSubject = new string('a', 256);
        var result = svc.ValidateTicketSubject(longSubject);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateContactName_Succeeds_WithValidName()
    {
        var svc = CreateService();
        var result = svc.ValidateContactName("John Doe");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateWorkspaceSlug_Fails_WhenEmpty()
    {
        var svc = CreateService();
        var result = svc.ValidateWorkspaceSlug("");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateWorkspaceSlug_Fails_WithInvalidCharacters()
    {
        var svc = CreateService();
        var result = svc.ValidateWorkspaceSlug("Invalid_Slug");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateQuantity_Succeeds_WithPositive()
    {
        var svc = CreateService();
        var result = svc.ValidateQuantity(5);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateQuantity_Fails_WithNegative()
    {
        var svc = CreateService();
        var result = svc.ValidateQuantity(-1);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidatePriceValue_Succeeds_WithPositive()
    {
        var svc = CreateService();
        var result = svc.ValidatePriceValue(19.99m);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidatePriceValue_Fails_WithNegative()
    {
        var svc = CreateService();
        var result = svc.ValidatePriceValue(-5.00m);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateStatusTransition_Succeeds_WithValidTransition()
    {
        var svc = CreateService();
        var result = svc.ValidateStatusTransition("New", "Open");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateStatusTransition_Fails_WithInvalidTransition()
    {
        var svc = CreateService();
        var result = svc.ValidateStatusTransition("New", "Resolved");
        Assert.False(result.IsValid);
    }
}
