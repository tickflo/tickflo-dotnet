namespace Tickflo.CoreTest.Pages;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Web.Pages.Workspaces;
using Xunit;

/// <summary>
/// Tests for WorkspacePageModel workspace membership validation.
/// </summary>
public class WorkspacePageModelTests
{
    private readonly Mock<IWorkspaceRepository> _mockWorkspaceRepo;
    private readonly Mock<IUserWorkspaceRepository> _mockUserWorkspaceRepo;
    private readonly TestableWorkspacePageModel _pageModel;

    public WorkspacePageModelTests()
    {
        this._mockWorkspaceRepo = new Mock<IWorkspaceRepository>();
        this._mockUserWorkspaceRepo = new Mock<IUserWorkspaceRepository>();
        this._pageModel = new TestableWorkspacePageModel();
    }

    [Fact]
    public async Task LoadWorkspaceAndValidateUserMembershipAsyncReturnsNotFoundWhenWorkspaceDoesNotExist()
    {
        // Arrange
        const string slug = "nonexistent";
        this._mockWorkspaceRepo
            .Setup(r => r.FindBySlugAsync(slug))
            .ReturnsAsync((Workspace?)null);

        // Act
        var result = await this._pageModel.PublicLoadWorkspaceAndValidateUserMembershipAsync(
            this._mockWorkspaceRepo.Object,
            this._mockUserWorkspaceRepo.Object,
            slug);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<NotFoundResult>(result);
        this._mockWorkspaceRepo.Verify(r => r.FindBySlugAsync(slug), Times.Once);
        this._mockUserWorkspaceRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoadWorkspaceAndValidateUserMembershipAsyncReturnsForbidWhenUserIsNotAuthenticated()
    {
        // Arrange
        const string slug = "test-workspace";
        var workspace = new Workspace { Id = 1, Slug = slug, Name = "Test Workspace" };

        this._mockWorkspaceRepo
            .Setup(r => r.FindBySlugAsync(slug))
            .ReturnsAsync(workspace);

        this._pageModel.SetUserAsUnauthenticated();

        // Act
        var result = await this._pageModel.PublicLoadWorkspaceAndValidateUserMembershipAsync(
            this._mockWorkspaceRepo.Object,
            this._mockUserWorkspaceRepo.Object,
            slug);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ForbidResult>(result);
        this._mockUserWorkspaceRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoadWorkspaceAndValidateUserMembershipAsyncReturnsForbidWhenUserIsNotMemberOfWorkspace()
    {
        // Arrange
        const string slug = "test-workspace";
        const int userId = 42;
        var workspace = new Workspace { Id = 1, Slug = slug, Name = "Test Workspace" };

        this._mockWorkspaceRepo
            .Setup(r => r.FindBySlugAsync(slug))
            .ReturnsAsync(workspace);

        this._mockUserWorkspaceRepo
            .Setup(r => r.FindAsync(userId, workspace.Id))
            .ReturnsAsync((UserWorkspace?)null);

        this._pageModel.SetUserWithId(userId);

        // Act
        var result = await this._pageModel.PublicLoadWorkspaceAndValidateUserMembershipAsync(
            this._mockWorkspaceRepo.Object,
            this._mockUserWorkspaceRepo.Object,
            slug);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ForbidResult>(result);
        this._mockUserWorkspaceRepo.Verify(r => r.FindAsync(userId, workspace.Id), Times.Once);
    }

    [Fact]
    public async Task LoadWorkspaceAndValidateUserMembershipAsyncReturnsWorkspaceAndUserIdWhenUserIsMember()
    {
        // Arrange
        const string slug = "test-workspace";
        const int userId = 42;
        var workspace = new Workspace { Id = 1, Slug = slug, Name = "Test Workspace" };
        var membership = new UserWorkspace { UserId = userId, WorkspaceId = workspace.Id, Accepted = true };

        this._mockWorkspaceRepo
            .Setup(r => r.FindBySlugAsync(slug))
            .ReturnsAsync(workspace);

        this._mockUserWorkspaceRepo
            .Setup(r => r.FindAsync(userId, workspace.Id))
            .ReturnsAsync(membership);

        this._pageModel.SetUserWithId(userId);

        // Act
        var result = await this._pageModel.PublicLoadWorkspaceAndValidateUserMembershipAsync(
            this._mockWorkspaceRepo.Object,
            this._mockUserWorkspaceRepo.Object,
            slug);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<WorkspaceUserLoadResult>(result);
        var loadResult = (WorkspaceUserLoadResult)result;
        Assert.NotNull(loadResult.Workspace);
        Assert.Equal(workspace.Id, loadResult.Workspace.Id);
        Assert.Equal(userId, loadResult.UserId);
        this._mockUserWorkspaceRepo.Verify(r => r.FindAsync(userId, workspace.Id), Times.Once);
    }

    [Fact]
    public async Task LoadWorkspaceAndValidateUserMembershipAsyncAllowsUnacceptedMembershipWhenUserIsMember()
    {
        // Arrange - User has membership but hasn't accepted invite yet
        const string slug = "test-workspace";
        const int userId = 42;
        var workspace = new Workspace { Id = 1, Slug = slug, Name = "Test Workspace" };
        var membership = new UserWorkspace { UserId = userId, WorkspaceId = workspace.Id, Accepted = false };

        this._mockWorkspaceRepo
            .Setup(r => r.FindBySlugAsync(slug))
            .ReturnsAsync(workspace);

        this._mockUserWorkspaceRepo
            .Setup(r => r.FindAsync(userId, workspace.Id))
            .ReturnsAsync(membership);

        this._pageModel.SetUserWithId(userId);

        // Act
        var result = await this._pageModel.PublicLoadWorkspaceAndValidateUserMembershipAsync(
            this._mockWorkspaceRepo.Object,
            this._mockUserWorkspaceRepo.Object,
            slug);

        // Assert - Should still succeed because membership exists (acceptance is handled by view services)
        Assert.NotNull(result);
        Assert.IsType<WorkspaceUserLoadResult>(result);
        var loadResult = (WorkspaceUserLoadResult)result;
        Assert.Equal(userId, loadResult.UserId);
    }

    [Fact]
    public async Task LoadWorkspaceAndValidateUserMembershipAsyncReturnsForbidWhenUserIdIsInvalid()
    {
        // Arrange
        const string slug = "test-workspace";
        var workspace = new Workspace { Id = 1, Slug = slug, Name = "Test Workspace" };

        this._mockWorkspaceRepo
            .Setup(r => r.FindBySlugAsync(slug))
            .ReturnsAsync(workspace);

        this._pageModel.SetUserWithInvalidId("invalid-user-id");

        // Act
        var result = await this._pageModel.PublicLoadWorkspaceAndValidateUserMembershipAsync(
            this._mockWorkspaceRepo.Object,
            this._mockUserWorkspaceRepo.Object,
            slug);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ForbidResult>(result);
        this._mockUserWorkspaceRepo.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Testable implementation of WorkspacePageModel to expose protected methods.
    /// </summary>
    private sealed class TestableWorkspacePageModel : WorkspacePageModel
    {
        public void SetUserWithId(int userId)
        {
            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ], "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            this.SetupPageContext(principal);
        }

        public void SetUserWithInvalidId(string invalidId)
        {
            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, invalidId)
            ], "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            this.SetupPageContext(principal);
        }

        public void SetUserAsUnauthenticated()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity());
            this.SetupPageContext(principal);
        }

        private void SetupPageContext(ClaimsPrincipal principal)
        {
            // We need to use reflection to set the readonly HttpContext property
            var httpContext = new DefaultHttpContext { User = principal };

            var pageContextProperty = typeof(PageModel).GetProperty("PageContext",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (pageContextProperty?.GetSetMethod(nonPublic: true) != null)
            {
                var pageContext = new PageContext
                {
                    HttpContext = httpContext
                };
                pageContextProperty.SetValue(this, pageContext);
            }
        }

        public async Task<object> PublicLoadWorkspaceAndValidateUserMembershipAsync(
            IWorkspaceRepository workspaceRepo,
            IUserWorkspaceRepository userWorkspaceRepository,
            string slug) => await this.LoadWorkspaceAndValidateUserMembershipAsync(workspaceRepo, userWorkspaceRepository, slug);
    }
}
