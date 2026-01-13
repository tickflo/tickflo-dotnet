using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;
using Tickflo.Core.Services.Contacts;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class ContactRegistrationServiceTests
{
    [Fact]
    public async Task RegisterContactAsync_Throws_When_DuplicateName()
    {
        var repo = new Mock<IContactRepository>();
        var tokenService = new Mock<IAccessTokenService>();
        repo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(new List<Contact> { new() { Name = "Existing" } });
        var svc = new ContactRegistrationService(repo.Object, tokenService.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RegisterContactAsync(1, new ContactRegistrationRequest { Name = "Existing" }, 9));
    }

    [Fact]
    public async Task RegisterContactAsync_GeneratesAccessToken()
    {
        var repo = new Mock<IContactRepository>();
        var tokenService = new Mock<IAccessTokenService>();
        var expectedToken = "test_token_123456789";

        repo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(new List<Contact>());
        tokenService.Setup(t => t.GenerateToken()).Returns(expectedToken);
        repo.Setup(r => r.CreateAsync(It.IsAny<Contact>(), CancellationToken.None))
            .ReturnsAsync((Contact c, CancellationToken _) => c);

        var svc = new ContactRegistrationService(repo.Object, tokenService.Object);
        var created = await svc.RegisterContactAsync(1, new ContactRegistrationRequest { Name = "New Contact" }, 9);

        Assert.Equal(expectedToken, created.AccessToken);
        tokenService.Verify(t => t.GenerateToken(), Times.Once);
    }

    [Fact]
    public async Task UpdateContactInformationAsync_Updates_Email_When_Valid()
    {
        var repo = new Mock<IContactRepository>();
        var tokenService = new Mock<IAccessTokenService>();
        repo.Setup(r => r.FindAsync(1, 2, CancellationToken.None)).ReturnsAsync(new Contact { Id = 2, Name = "C", Email = "old@test.com" });
        repo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(new List<Contact> { new() { Id = 2, Name = "C" } });

        var svc = new ContactRegistrationService(repo.Object, tokenService.Object);
        var updated = await svc.UpdateContactInformationAsync(1, 2, new ContactUpdateRequest { Email = "new@test.com" }, 5);

        Assert.Equal("new@test.com", updated.Email);
        repo.Verify(r => r.UpdateAsync(It.Is<Contact>(c => c.Email == "new@test.com"), CancellationToken.None), Times.Once);
    }
}
