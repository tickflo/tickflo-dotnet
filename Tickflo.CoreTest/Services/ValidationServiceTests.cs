namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class ValidationServiceTests
{
    private static ValidationService CreateService(
        IUserRepository? userRepository = null,
        IRoleRepository? roleRepo = null)
    {
        userRepository ??= Mock.Of<IUserRepository>();
        roleRepo ??= Mock.Of<IRoleRepository>();
        return new ValidationService(userRepository, roleRepo);
    }

    [Fact]
    public async Task ValidateEmailAsyncSucceedsWithValidEmail()
    {
        var svc = CreateService();
        var result = await svc.ValidateEmailAsync("test@example.com");
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateEmailAsyncFailsWhenEmpty()
    {
        var svc = CreateService();
        var result = await svc.ValidateEmailAsync("");
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateEmailAsyncFailsWhenInvalidFormat()
    {
        var svc = CreateService();
        var result = await svc.ValidateEmailAsync("not-an-email");
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateEmailAsyncFailsWhenDuplicate()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.FindByEmailAsync("dup@example.com")).ReturnsAsync(new User { Id = 1 });
        var svc = CreateService(users.Object);

        var result = await svc.ValidateEmailAsync("dup@example.com", checkUniqueness: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "Email");
    }

    [Fact]
    public void ValidateTicketSubjectSucceedsWithValidSubject()
    {
        var svc = CreateService();
        var result = svc.ValidateTicketSubject("Valid Subject");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateTicketSubjectFailsWhenEmpty()
    {
        var svc = CreateService();
        var result = svc.ValidateTicketSubject("");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateTicketSubjectFailsWhenTooLong()
    {
        var svc = CreateService();
        var longSubject = new string('a', 256);
        var result = svc.ValidateTicketSubject(longSubject);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateContactNameSucceedsWithValidName()
    {
        var svc = CreateService();
        var result = svc.ValidateContactName("John Doe");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateWorkspaceSlugFailsWhenEmpty()
    {
        var svc = CreateService();
        var result = svc.ValidateWorkspaceSlug("");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateWorkspaceSlugFailsWithInvalidCharacters()
    {
        var svc = CreateService();
        var result = svc.ValidateWorkspaceSlug("Invalid_Slug");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateQuantitySucceedsWithPositive()
    {
        var svc = CreateService();
        var result = svc.ValidateQuantity(5);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateQuantityFailsWithNegative()
    {
        var svc = CreateService();
        var result = svc.ValidateQuantity(-1);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidatePriceValueSucceedsWithPositive()
    {
        var svc = CreateService();
        var result = svc.ValidatePriceValue(19.99m);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidatePriceValueFailsWithNegative()
    {
        var svc = CreateService();
        var result = svc.ValidatePriceValue(-5.00m);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateStatusTransitionSucceedsWithValidTransition()
    {
        var svc = CreateService();
        var result = svc.ValidateStatusTransition("New", "Open");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateStatusTransitionFailsWithInvalidTransition()
    {
        var svc = CreateService();
        var result = svc.ValidateStatusTransition("New", "Resolved");
        Assert.False(result.IsValid);
    }
}
