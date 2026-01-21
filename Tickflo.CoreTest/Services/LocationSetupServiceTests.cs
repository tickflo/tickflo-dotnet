namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class LocationSetupServiceTests
{
    [Fact]
    public async Task CreateLocationAsyncThrowsWhenDuplicateName()
    {
        var repo = new Mock<ILocationRepository>();
        repo.Setup(r => r.ListAsync(1)).ReturnsAsync([new() { Name = "Main" }]);
        var contactRepo = Mock.Of<IContactRepository>();
        var svc = new LocationSetupService(repo.Object, contactRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateLocationAsync(1, new LocationCreationRequest { Name = "Main" }, 9));
    }

    [Fact]
    public async Task ActivateLocationAsyncSetsActive()
    {
        var repo = new Mock<ILocationRepository>();
        repo.Setup(r => r.FindAsync(1, 2)).ReturnsAsync(new Location { Id = 2, Active = false });
        var svc = new LocationSetupService(repo.Object, Mock.Of<IContactRepository>());

        var loc = await svc.ActivateLocationAsync(1, 2, 4);

        Assert.True(loc.Active);
        repo.Verify(r => r.UpdateAsync(It.Is<Location>(l => l.Active)), Times.Once);
    }
}
