namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class RoleManagementServiceTests
{
    private static IRoleManagementService CreateService(
        IUserWorkspaceRoleRepository? uwrRepo = null,
        IRoleRepository? roleRepo = null)
    {
        uwrRepo ??= Mock.Of<IUserWorkspaceRoleRepository>();
        roleRepo ??= Mock.Of<IRoleRepository>();
        return new RoleManagementService(uwrRepo, roleRepo);
    }

    [Fact]
    public async Task AssignRoleToUserAsyncCreatesAssignment()
    {
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.FindByIdAsync(5)).ReturnsAsync(new Role { Id = 5, WorkspaceId = 1 });

        var svc = CreateService(uwrRepo.Object, roleRepo.Object);
        var result = await svc.AssignRoleToUserAsync(10, 1, 5, 9);

        Assert.Equal(10, result.UserId);
        Assert.Equal(1, result.WorkspaceId);
        Assert.Equal(5, result.RoleId);
        Assert.Equal(9, result.CreatedBy);
        uwrRepo.Verify(r => r.AddAsync(10, 1, 5, 9), Times.Once);
    }

    [Fact]
    public async Task AssignRoleToUserAsyncThrowsWhenRoleNotFound()
    {
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.FindByIdAsync(99)).ReturnsAsync((Role)null!);

        var svc = CreateService(roleRepo: roleRepo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AssignRoleToUserAsync(1, 1, 99, 9));
    }

    [Fact]
    public async Task AssignRoleToUserAsyncThrowsWhenRoleNotInWorkspace()
    {
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.FindByIdAsync(5)).ReturnsAsync(new Role { Id = 5, WorkspaceId = 2 });

        var svc = CreateService(roleRepo: roleRepo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AssignRoleToUserAsync(1, 1, 5, 9));
    }

    [Fact]
    public async Task RemoveRoleFromUserAsyncRemovesRole()
    {
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();
        var svc = CreateService(uwrRepo.Object);

        var result = await svc.RemoveRoleFromUserAsync(10, 1, 5);

        Assert.True(result);
        uwrRepo.Verify(r => r.RemoveAsync(10, 1, 5), Times.Once);
    }

    [Fact]
    public async Task RemoveRoleFromUserAsyncReturnsFalseOnError()
    {
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();
        uwrRepo.Setup(r => r.RemoveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Test error"));

        var svc = CreateService(uwrRepo.Object);

        var result = await svc.RemoveRoleFromUserAsync(10, 1, 5);

        Assert.False(result);
    }

    [Fact]
    public async Task CountRoleAssignmentsAsyncReturnsCount()
    {
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();
        uwrRepo.Setup(r => r.CountAssignmentsForRoleAsync(1, 2)).ReturnsAsync(3);

        var svc = CreateService(uwrRepo.Object);

        var count = await svc.CountRoleAssignmentsAsync(1, 2);

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task RoleBelongsToWorkspaceAsyncReturnsTrue()
    {
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.FindByIdAsync(5)).ReturnsAsync(new Role { Id = 5, WorkspaceId = 1 });

        var svc = CreateService(roleRepo: roleRepo.Object);

        var result = await svc.RoleBelongsToWorkspaceAsync(5, 1);

        Assert.True(result);
    }

    [Fact]
    public async Task RoleBelongsToWorkspaceAsyncReturnsFalseWhenDifferentWorkspace()
    {
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.FindByIdAsync(5)).ReturnsAsync(new Role { Id = 5, WorkspaceId = 2 });

        var svc = CreateService(roleRepo: roleRepo.Object);

        var result = await svc.RoleBelongsToWorkspaceAsync(5, 1);

        Assert.False(result);
    }

    [Fact]
    public async Task RoleBelongsToWorkspaceAsyncReturnsFalseWhenNotFound()
    {
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.FindByIdAsync(99)).ReturnsAsync((Role)null!);

        var svc = CreateService(roleRepo: roleRepo.Object);

        var result = await svc.RoleBelongsToWorkspaceAsync(99, 1);

        Assert.False(result);
    }

    [Fact]
    public async Task GetWorkspaceRolesAsyncReturnsRoles()
    {
        var roles = new List<Role>
        {
            new() { Id = 1, WorkspaceId = 1, Name = "Admin" },
            new() { Id = 2, WorkspaceId = 1, Name = "User" }
        };
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.ListForWorkspaceAsync(1)).ReturnsAsync(roles);

        var svc = CreateService(roleRepo: roleRepo.Object);

        var result = await svc.GetWorkspaceRolesAsync(1);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Name == "Admin");
        Assert.Contains(result, r => r.Name == "User");
    }

    [Fact]
    public async Task GetUserRolesAsyncReturnsUserRoles()
    {
        var roles = new List<Role>
        {
            new() { Id = 1, Name = "Manager" }
        };
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();
        uwrRepo.Setup(r => r.GetRolesAsync(5, 1)).ReturnsAsync(roles);

        var svc = CreateService(uwrRepo.Object);

        var result = await svc.GetUserRolesAsync(5, 1);

        Assert.Single(result);
        Assert.Equal("Manager", result[0].Name);
    }

    [Fact]
    public async Task EnsureRoleCanBeDeletedAsyncThrowsWhenAssigned()
    {
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();
        uwrRepo.Setup(r => r.CountAssignmentsForRoleAsync(1, 5)).ReturnsAsync(3);

        var svc = CreateService(uwrRepo.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.EnsureRoleCanBeDeletedAsync(1, 5, "TestRole"));

        Assert.Contains("TestRole", ex.Message);
        Assert.Contains("3 user(s)", ex.Message);
    }

    [Fact]
    public async Task EnsureRoleCanBeDeletedAsyncSucceedsWhenNoAssignments()
    {
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();
        uwrRepo.Setup(r => r.CountAssignmentsForRoleAsync(1, 5)).ReturnsAsync(0);

        var svc = CreateService(uwrRepo.Object);

        await svc.EnsureRoleCanBeDeletedAsync(1, 5, "TestRole");
    }
}
