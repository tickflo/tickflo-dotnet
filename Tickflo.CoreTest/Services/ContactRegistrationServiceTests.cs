namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class ContactRegistrationServiceTests
{
    private static IContactRegistrationService CreateService(IContactRepository? repo = null)
    {
        repo ??= Mock.Of<IContactRepository>();
        return new ContactRegistrationService(repo);
    }

    [Fact]
    public async Task RegisterContactAsyncCreatesContact()
    {
        var repo = new Mock<IContactRepository>();
        repo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync([]);

        var svc = CreateService(repo.Object);
        var request = new ContactRegistrationRequest
        {
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "555-0100",
            Company = "Acme Corp"
        };

        var result = await svc.RegisterContactAsync(1, request, 9);

        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("555-0100", result.Phone);
        Assert.Equal("Acme Corp", result.Company);
        repo.Verify(r => r.CreateAsync(It.IsAny<Contact>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RegisterContactAsyncThrowsWhenNameEmpty()
    {
        var svc = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RegisterContactAsync(1, new ContactRegistrationRequest { Name = "" }, 9));
    }

    [Fact]
    public async Task RegisterContactAsyncThrowsWhenDuplicateName()
    {
        var repo = new Mock<IContactRepository>();
        repo.Setup(r => r.ListAsync(1, CancellationToken.None))
            .ReturnsAsync([new() { Name = "Existing" }]);

        var svc = CreateService(repo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RegisterContactAsync(1, new ContactRegistrationRequest { Name = "Existing" }, 9));
    }

    [Fact]
    public async Task RegisterContactAsyncThrowsWhenInvalidEmail()
    {
        var repo = new Mock<IContactRepository>();
        repo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync([]);

        var svc = CreateService(repo.Object);
        var request = new ContactRegistrationRequest
        {
            Name = "John",
            Email = "not-an-email"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RegisterContactAsync(1, request, 9));
    }

    [Fact]
    public async Task RegisterContactAsyncAllowsEmptyEmail()
    {
        var repo = new Mock<IContactRepository>();
        repo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync([]);

        var svc = CreateService(repo.Object);
        var request = new ContactRegistrationRequest { Name = "John" };

        var result = await svc.RegisterContactAsync(1, request, 9);

        Assert.Equal("John", result.Name);
        Assert.Equal(string.Empty, result.Email);
    }

    [Fact]
    public async Task RegisterContactAsyncTrimsWhitespace()
    {
        var repo = new Mock<IContactRepository>();
        repo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync([]);

        var svc = CreateService(repo.Object);
        var request = new ContactRegistrationRequest
        {
            Name = "  John Doe  ",
            Email = "  john@example.com  ",
            Phone = "  555-0100  "
        };

        var result = await svc.RegisterContactAsync(1, request, 9);

        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("555-0100", result.Phone);
    }

    [Fact]
    public async Task UpdateContactInformationAsyncUpdatesEmail()
    {
        var repo = new Mock<IContactRepository>();
        repo.Setup(r => r.FindAsync(1, 2, CancellationToken.None))
            .ReturnsAsync(new Contact { Id = 2, Name = "C", Email = "old@test.com" });
        repo.Setup(r => r.ListAsync(1, CancellationToken.None))
            .ReturnsAsync([new() { Id = 2, Name = "C" }]);

        var svc = CreateService(repo.Object);
        var updated = await svc.UpdateContactInformationAsync(1, 2, new ContactUpdateRequest { Email = "new@test.com" }, 5);

        Assert.Equal("new@test.com", updated.Email);
        repo.Verify(r => r.UpdateAsync(It.Is<Contact>(c => c.Email == "new@test.com"), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task UpdateContactInformationAsyncThrowsWhenNotFound()
    {
        var repo = new Mock<IContactRepository>();
        repo.Setup(r => r.FindAsync(1, 99, CancellationToken.None))
            .ReturnsAsync((Contact)null!);

        var svc = CreateService(repo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.UpdateContactInformationAsync(1, 99, new ContactUpdateRequest(), 5));
    }

    [Fact]
    public async Task UpdateContactInformationAsyncThrowsWhenInvalidEmail()
    {
        var repo = new Mock<IContactRepository>();
        repo.Setup(r => r.FindAsync(1, 2, CancellationToken.None))
            .ReturnsAsync(new Contact { Id = 2, Name = "C" });

        var svc = CreateService(repo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.UpdateContactInformationAsync(1, 2, new ContactUpdateRequest { Email = "invalid" }, 5));
    }

    [Fact]
    public async Task UpdateContactInformationAsyncUpdatesName()
    {
        var repo = new Mock<IContactRepository>();
        repo.Setup(r => r.FindAsync(1, 2, CancellationToken.None))
            .ReturnsAsync(new Contact { Id = 2, Name = "Old Name" });
        repo.Setup(r => r.ListAsync(1, CancellationToken.None))
            .ReturnsAsync([new() { Id = 2, Name = "Old Name" }]);

        var svc = CreateService(repo.Object);
        var updated = await svc.UpdateContactInformationAsync(1, 2, new ContactUpdateRequest { Name = "New Name" }, 5);

        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task UpdateContactInformationAsyncThrowsWhenDuplicateName()
    {
        var repo = new Mock<IContactRepository>();
        repo.Setup(r => r.FindAsync(1, 2, CancellationToken.None))
            .ReturnsAsync(new Contact { Id = 2, Name = "Contact A" });
        repo.Setup(r => r.ListAsync(1, CancellationToken.None))
            .ReturnsAsync(
            [
                new() { Id = 1, Name = "Existing" },
                new() { Id = 2, Name = "Contact A" }
            ]);

        var svc = CreateService(repo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.UpdateContactInformationAsync(1, 2, new ContactUpdateRequest { Name = "Existing" }, 5));
    }

    [Fact]
    public async Task RemoveContactAsyncDeletesContact()
    {
        var repo = new Mock<IContactRepository>();
        var svc = CreateService(repo.Object);

        await svc.RemoveContactAsync(1, 5);

        repo.Verify(r => r.DeleteAsync(1, 5, CancellationToken.None), Times.Once);
    }
}
