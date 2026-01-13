using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Contacts;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class ContactRegistrationServiceTests
{
    [Fact]
    public async Task RegisterContactAsync_Throws_When_DuplicateName()
    {
        var repo = new Mock<IContactRepository>();
        repo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(new List<Contact> { new() { Name = "Existing" } });
        var svc = new ContactRegistrationService(repo.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RegisterContactAsync(1, new ContactRegistrationRequest { Name = "Existing" }, 9));
    }

    [Fact]
    public async Task UpdateContactInformationAsync_Updates_Email_When_Valid()
    {
        var repo = new Mock<IContactRepository>();
        repo.Setup(r => r.FindAsync(1, 2, CancellationToken.None)).ReturnsAsync(new Contact { Id = 2, Name = "C", Email = "old@test.com" });
        repo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(new List<Contact> { new() { Id = 2, Name = "C" } });

        var svc = new ContactRegistrationService(repo.Object);
        var updated = await svc.UpdateContactInformationAsync(1, 2, new ContactUpdateRequest { Email = "new@test.com" }, 5);

        Assert.Equal("new@test.com", updated.Email);
        repo.Verify(r => r.UpdateAsync(It.Is<Contact>(c => c.Email == "new@test.com"), CancellationToken.None), Times.Once);
    }
}
