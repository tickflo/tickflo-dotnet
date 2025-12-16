using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class TickfloDbContext(DbContextOptions<TickfloDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Token> Tokens => Set<Token>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<UserWorkspace> UserWorkspaces => Set<UserWorkspace>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserWorkspaceRole> UserWorkspaceRoles => Set<UserWorkspaceRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Token>()
            .HasKey(t => new { t.UserId, t.Value });

        modelBuilder.Entity<UserWorkspace>()
            .HasKey(uw => new { uw.UserId, uw.WorkspaceId });

        modelBuilder.Entity<Role>()
            .HasIndex(r => new { r.WorkspaceId, r.Name })
            .IsUnique();

        modelBuilder.Entity<UserWorkspaceRole>()
            .HasKey(uwr => new { uwr.UserId, uwr.WorkspaceId, uwr.RoleId });

        modelBuilder.Entity<Workspace>()
            .HasIndex(w => w.Slug)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}