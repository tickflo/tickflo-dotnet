namespace Tickflo.Core.Test.Services.Workspace;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;
using Xunit;

public class WorkspaceCreationServiceTests
{
    private static TickfloDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TickfloDbContext(options);
    }

    private static TickfloConfig CreateConfig() => new()
    {
        Workspace = new WorkspaceConfig
        {
            MinNameLength = 2,
            MaxNameLength = 100,
            MaxSlugLength = 100
        }
    };

    [Fact]
    public async Task CreateWorkspaceAsync_CreatesDefaultInboundEmailRoute()
    {
        // Arrange
        var db = CreateDbContext();
        var service = new WorkspaceCreationService(db, CreateConfig());

        // Act
        var workspace = await service.CreateWorkspaceAsync("Acme Support", createdByUserId: 1);

        // Assert
        var route = await db.InboundEmailRoutes.SingleAsync(r => r.WorkspaceId == workspace.Id);
        Assert.Equal("acme-support", route.LocalPart);
        Assert.Equal("Default", route.Label);
        Assert.True(route.Active);
        Assert.Equal(1, route.CreatedByUserId);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_WithConflictingLocalPart_SuffixesRoute()
    {
        // Arrange
        var db = CreateDbContext();
        var otherWorkspace = new Workspace
        {
            Name = "Other",
            Slug = "other",
            CreatedBy = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Workspaces.Add(otherWorkspace);
        await db.SaveChangesAsync();

        db.InboundEmailRoutes.Add(new InboundEmailRoute
        {
            WorkspaceId = otherWorkspace.Id,
            LocalPart = "acme",
            Label = "Taken",
            Active = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new WorkspaceCreationService(db, CreateConfig());

        // Act
        var workspace = await service.CreateWorkspaceAsync("Acme", createdByUserId: 1);

        // Assert
        var route = await db.InboundEmailRoutes.SingleAsync(r => r.WorkspaceId == workspace.Id);
        Assert.Equal("acme-2", route.LocalPart);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_WhenAllSuffixesTaken_SkipsRouteAndStillSucceeds()
    {
        // Arrange
        var db = CreateDbContext();
        var otherWorkspace = new Workspace
        {
            Name = "Other",
            Slug = "other",
            CreatedBy = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Workspaces.Add(otherWorkspace);
        await db.SaveChangesAsync();

        db.InboundEmailRoutes.Add(new InboundEmailRoute
        {
            WorkspaceId = otherWorkspace.Id,
            LocalPart = "acme",
            Label = "Taken",
            Active = true,
            CreatedAt = DateTime.UtcNow
        });
        for (var i = 2; i <= 11; i++)
        {
            db.InboundEmailRoutes.Add(new InboundEmailRoute
            {
                WorkspaceId = otherWorkspace.Id,
                LocalPart = $"acme-{i}",
                Label = "Taken",
                Active = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var service = new WorkspaceCreationService(db, CreateConfig());

        // Act
        var workspace = await service.CreateWorkspaceAsync("Acme", createdByUserId: 1);

        // Assert — workspace created, no route for it
        Assert.NotNull(workspace);
        Assert.False(await db.InboundEmailRoutes.AnyAsync(r => r.WorkspaceId == workspace.Id));
    }
}
