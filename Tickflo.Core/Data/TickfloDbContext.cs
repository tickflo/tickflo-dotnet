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
    public DbSet<ReportRun> ReportRuns => Set<ReportRun>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Inventory> Inventory => Set<Inventory>();
    public DbSet<TicketInventory> TicketInventories => Set<TicketInventory>();
    public DbSet<TicketStatus> TicketStatuses => Set<TicketStatus>();
    public DbSet<TicketPriority> TicketPriorities => Set<TicketPriority>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<TicketHistory> TicketHistory => Set<TicketHistory>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermissionLink> RolePermissions => Set<RolePermissionLink>();

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

        modelBuilder.Entity<ReportRun>()
            .HasIndex(rr => new { rr.WorkspaceId, rr.ReportId, rr.StartedAt });

        modelBuilder.Entity<Contact>()
            .HasIndex(c => new { c.WorkspaceId, c.Email })
            .IsUnique();

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => new { t.WorkspaceId, t.Id });

        // TicketInventory relationship
        modelBuilder.Entity<TicketInventory>()
            .HasOne(ti => ti.Ticket)
            .WithMany(t => t.TicketInventories)
            .HasForeignKey(ti => ti.TicketId);
        modelBuilder.Entity<TicketInventory>()
            .HasOne(ti => ti.Inventory)
            .WithMany()
            .HasForeignKey(ti => ti.InventoryId);

        modelBuilder.Entity<Inventory>()
            .HasIndex(i => new { i.WorkspaceId, i.Sku })
            .IsUnique();

        modelBuilder.Entity<TicketStatus>()
            .HasIndex(s => new { s.WorkspaceId, s.Name })
            .IsUnique();

        modelBuilder.Entity<TicketPriority>()
            .HasIndex(p => new { p.WorkspaceId, p.Name })
            .IsUnique();

        modelBuilder.Entity<TicketType>()
            .HasIndex(t => new { t.WorkspaceId, t.Name })
            .IsUnique();

        modelBuilder.Entity<TicketHistory>()
            .HasIndex(h => new { h.WorkspaceId, h.TicketId, h.CreatedAt });

        modelBuilder.Entity<Team>()
            .HasIndex(t => new { t.WorkspaceId, t.Name })
            .IsUnique();

        modelBuilder.Entity<TeamMember>()
            .HasKey(tm => new { tm.TeamId, tm.UserId });

        // Permissions catalog
        modelBuilder.Entity<Permission>()
            .HasIndex(p => new { p.Action, p.Resource })
            .IsUnique();

        // Role to permission link table
        modelBuilder.Entity<RolePermissionLink>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        base.OnModelCreating(modelBuilder);
    }
}