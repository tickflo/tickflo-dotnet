namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class WorkspaceCreationServiceTests
{
    [Fact]
    public async Task CreateWorkspaceAsyncThrowsWhenSlugExists()
    {
        var workspaceRepo = new Mock<IWorkspaceRepository>();
        workspaceRepo.Setup(r => r.FindBySlugAsync(It.IsAny<string>())).ReturnsAsync(new Workspace { Id = 9, Slug = "existing" });
        var roleRepo = new Mock<IRoleRepository>();
        var uw = Mock.Of<IUserWorkspaceRepository>();
        var uwr = Mock.Of<IUserWorkspaceRoleRepository>();
        var svc = new WorkspaceCreationService(workspaceRepo.Object, roleRepo.Object, uw, uwr);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateWorkspaceAsync(new WorkspaceCreationRequest { Name = "Existing" }, 1));
    }

    [Fact]
    public async Task CreateWorkspaceAsyncAddsDefaultRolesAndAdminMembership()
    {
        var workspaceRepo = new Mock<IWorkspaceRepository>();
        workspaceRepo.Setup(r => r.FindBySlugAsync(It.IsAny<string>())).ReturnsAsync((Workspace?)null);
        workspaceRepo.Setup(r => r.AddAsync(It.IsAny<Workspace>())).Callback<Workspace>(w => w.Id = 1).Returns(Task.CompletedTask);
        var roleRepo = new Mock<IRoleRepository>();
        var findCalls = 0;
        roleRepo.Setup(r => r.FindByNameAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync((int wsId, string name) =>
        {
            findCalls++;
            if (findCalls <= 4)
            {
                return null;
            }

            return new Role { Id = 99, WorkspaceId = wsId, Name = name, Admin = name == "Admin" };
        });
        roleRepo.Setup(r => r.AddAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((int wsId, string name, bool admin, int createdBy) => new Role { WorkspaceId = wsId, Name = name, Admin = admin, CreatedBy = createdBy, Id = admin ? 99 : 0 });
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.AddAsync(It.IsAny<UserWorkspace>())).Returns(Task.CompletedTask);
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        uwr.Setup(r => r.AddAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);
        var svc = new WorkspaceCreationService(workspaceRepo.Object, roleRepo.Object, uw.Object, uwr.Object);

        var workspace = await svc.CreateWorkspaceAsync(new WorkspaceCreationRequest { Name = "New Space" }, 7);

        roleRepo.Verify(r => r.AddAsync(It.IsAny<int>(), "Admin", true, 7), Times.Once);
        uw.Verify(r => r.AddAsync(It.Is<UserWorkspace>(uws => uws.UserId == 7 && uws.Accepted)), Times.Once);
    }
}
