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
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Inventory> Inventory => Set<Inventory>();
    public DbSet<TicketStatus> TicketStatuses => Set<TicketStatus>();
    public DbSet<TicketPriority> TicketPriorities => Set<TicketPriority>();

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

        modelBuilder.Entity<Location>()
            .HasIndex(l => new { l.WorkspaceId, l.Name })
            .IsUnique();

        modelBuilder.Entity<Report>()
            .HasIndex(r => new { r.WorkspaceId, r.Name })
            .IsUnique();

        modelBuilder.Entity<Contact>()
            .HasIndex(c => new { c.WorkspaceId, c.Email })
            .IsUnique();

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => new { t.WorkspaceId, t.Id });

        modelBuilder.Entity<Inventory>()
            .HasIndex(i => new { i.WorkspaceId, i.Sku })
            .IsUnique();

        modelBuilder.Entity<TicketStatus>()
            .HasIndex(s => new { s.WorkspaceId, s.Name })
            .IsUnique();

        modelBuilder.Entity<TicketPriority>()
            .HasIndex(p => new { p.WorkspaceId, p.Name })
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}