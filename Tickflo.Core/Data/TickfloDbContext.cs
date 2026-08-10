namespace Tickflo.Core.Data;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class TickfloDbContext(DbContextOptions<TickfloDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => this.Set<User>();
    public DbSet<Token> Tokens => this.Set<Token>();
    public DbSet<Workspace> Workspaces => this.Set<Workspace>();
    public DbSet<UserWorkspace> UserWorkspaces => this.Set<UserWorkspace>();
    public DbSet<Role> Roles => this.Set<Role>();
    public DbSet<UserWorkspaceRole> UserWorkspaceRoles => this.Set<UserWorkspaceRole>();
    public DbSet<Location> Locations => this.Set<Location>();
    public DbSet<Report> Reports => this.Set<Report>();
    public DbSet<ReportRun> ReportRuns => this.Set<ReportRun>();
    public DbSet<Contact> Contacts => this.Set<Contact>();
    public DbSet<ContactLocation> ContactLocations => this.Set<ContactLocation>();
    public DbSet<Ticket> Tickets => this.Set<Ticket>();
    public DbSet<Inventory> Inventory => this.Set<Inventory>();
    public DbSet<TicketInventory> TicketInventories => this.Set<TicketInventory>();
    public DbSet<TicketStatus> TicketStatuses => this.Set<TicketStatus>();
    public DbSet<TicketPriority> TicketPriorities => this.Set<TicketPriority>();
    public DbSet<TicketType> TicketTypes => this.Set<TicketType>();
    public DbSet<TicketHistory> TicketHistory => this.Set<TicketHistory>();
    public DbSet<TicketComment> TicketComments => this.Set<TicketComment>();
    public DbSet<Team> Teams => this.Set<Team>();
    public DbSet<TeamMember> TeamMembers => this.Set<TeamMember>();
    public DbSet<Permission> Permissions => this.Set<Permission>();
    public DbSet<RolePermission> RolePermissions => this.Set<RolePermission>();
    public DbSet<Notification> Notifications => this.Set<Notification>();
    public DbSet<UserNotificationPreference> UserNotificationPreferences => this.Set<UserNotificationPreference>();
    public DbSet<FileStorage> FileStorages => this.Set<FileStorage>();
    public DbSet<EmailTemplate> EmailTemplates => this.Set<EmailTemplate>();
    public DbSet<Email> Emails => this.Set<Email>();
    public DbSet<UserEmailChange> UserEmailChanges => this.Set<UserEmailChange>();
    public DbSet<InboundEmailRoute> InboundEmailRoutes => this.Set<InboundEmailRoute>();
    public DbSet<InboundEmail> InboundEmails => this.Set<InboundEmail>();
    public DbSet<InboundEmailAttachment> InboundEmailAttachments => this.Set<InboundEmailAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Token>(entity =>
        {
            entity.HasKey(t => new { t.UserId, t.Value });

            // The session-token middleware looks tokens up by value on every
            // authenticated request. The composite PK (user_id, token) does
            // not help that query, so add a unique index on the value column.
            entity.HasIndex(t => t.Value).IsUnique();
        });

        modelBuilder.Entity<UserWorkspace>(entity =>
        {
            entity.HasKey(uw => new { uw.UserId, uw.WorkspaceId });
            entity.HasOne(userWorkspace => userWorkspace.Workspace)
            .WithMany(workspace => workspace.UserWorkspaces)
            .HasForeignKey(userWorkspace => userWorkspace.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<Role>()
            .HasIndex(r => new { r.WorkspaceId, r.Name })
            .IsUnique();

        modelBuilder.Entity<UserWorkspaceRole>()
            .HasKey(userWorkspaceRoleRepository => new { userWorkspaceRoleRepository.UserId, userWorkspaceRoleRepository.WorkspaceId, userWorkspaceRoleRepository.RoleId });

        modelBuilder.Entity<Workspace>(e =>
        {
            e.HasIndex(w => w.Slug).IsUnique();
            e.HasOne(w => w.CreatedByUser)
                .WithMany()
                .HasForeignKey(w => w.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Location>()
            .HasIndex(l => new { l.WorkspaceId, l.Name })
            .IsUnique();

        modelBuilder.Entity<Report>()
            .HasIndex(r => new { r.WorkspaceId, r.Name })
            .IsUnique();

        modelBuilder.Entity<ReportRun>()
            .HasIndex(rr => new { rr.WorkspaceId, rr.ReportId, rr.StartedAt })
            .IsDescending(false, false, true);

        modelBuilder.Entity<Contact>()
            .HasIndex(c => new { c.WorkspaceId, c.Email })
            .IsUnique();

        modelBuilder.Entity<ContactLocation>()
            .HasKey(cl => new { cl.ContactId, cl.LocationId });
        modelBuilder.Entity<ContactLocation>()
            .HasIndex(cl => new { cl.WorkspaceId, cl.ContactId });

        modelBuilder.Entity<TicketInventory>()
            .ToTable("ticket_inventory");

        modelBuilder.Entity<TicketInventory>()
            .HasOne(ti => ti.Ticket)
            .WithMany(t => t.TicketInventories)
            .HasForeignKey(ti => ti.TicketId);

        modelBuilder.Entity<TicketInventory>()
            .HasOne(ti => ti.Inventory)
            .WithMany()
            .HasForeignKey(ti => ti.InventoryId);

        modelBuilder.Entity<Inventory>()
            .ToTable("inventory");

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
            .ToTable("ticket_history");

        modelBuilder.Entity<TicketHistory>()
            .HasIndex(h => new { h.WorkspaceId, h.TicketId, h.CreatedAt });

        modelBuilder.Entity<TicketComment>()
            .HasIndex(c => new { c.WorkspaceId, c.TicketId, c.CreatedAt });
        modelBuilder.Entity<TicketComment>()
            .HasOne(c => c.CreatedByUser)
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TicketComment>()
            .HasOne(c => c.UpdatedByUser)
            .WithMany()
            .HasForeignKey(c => c.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Team>()
            .HasIndex(t => new { t.WorkspaceId, t.Name })
            .IsUnique();

        modelBuilder.Entity<TeamMember>()
            .HasKey(tm => new { tm.TeamId, tm.UserId });

        // Permissions catalog
        modelBuilder.Entity<Permission>()
            .HasIndex(p => new { p.Action, p.Resource })
            .IsUnique();

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // User notification preferences
        modelBuilder.Entity<UserNotificationPreference>()
            .HasKey(unp => new { unp.UserId, unp.NotificationType });

        // File storage
        modelBuilder.Entity<FileStorage>(entity =>
        {
            entity.ToTable("file_storage");
            entity.Property(fs => fs.CreatedByUserId).HasColumnName("created_by");
            entity.Property(fs => fs.UpdatedByUserId).HasColumnName("updated_by");

            entity.HasIndex(fs => new { fs.WorkspaceId, fs.CreatedAt });
            entity.HasIndex(fs => new { fs.WorkspaceId, fs.Category });
            entity.HasIndex(fs => fs.Path);
            entity.HasIndex(fs => new { fs.RelatedEntityType, fs.RelatedEntityId });

            // Cascade delete FileStorage records when workspace is deleted
            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(fs => fs.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Email>(entity =>
        {
            entity.Property(e => e.State).HasDefaultValueSql("'created'::character varying");
            entity.Property(e => e.Vars)
                .HasConversion(
                    v => JsonSerializer.Serialize(v),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v) ?? new Dictionary<string, string>());
        });

        modelBuilder.Entity<UserEmailChange>(entity =>
        {
            entity.HasKey(uec => uec.UserId);
            entity.Property(uec => uec.OldEmail).HasColumnName("old");
            entity.Property(uec => uec.NewEmail).HasColumnName("new");
        });

        // Inbound email routing
        modelBuilder.Entity<InboundEmailRoute>(entity =>
        {
            entity.ToTable("inbound_email_routes");
            entity.HasIndex(r => r.LocalPart).IsUnique();
            entity.Property(r => r.LocalPart).IsRequired().HasMaxLength(128);
            entity.Property(r => r.Label).IsRequired().HasMaxLength(256);
        });

        modelBuilder.Entity<InboundEmail>(entity =>
        {
            entity.ToTable("inbound_emails");
            entity.Property(e => e.FromEmail).IsRequired().HasMaxLength(320);
            entity.Property(e => e.ToEmail).IsRequired().HasMaxLength(320);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(998);
            entity.Property(e => e.MessageId).IsRequired().HasMaxLength(512);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32).HasDefaultValue("Pending");
            entity.HasIndex(e => e.MessageId).IsUnique();
            entity.HasIndex(e => new { e.WorkspaceId, e.ReceivedAt });
            entity.HasIndex(e => new { e.WorkspaceId, e.Status });

            entity.HasOne(e => e.Route)
                .WithMany()
                .HasForeignKey(e => e.RouteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InboundEmailAttachment>(entity =>
        {
            entity.ToTable("inbound_email_attachments");
            entity.Property(a => a.FileName).IsRequired().HasMaxLength(512);
            entity.Property(a => a.ContentType).IsRequired().HasMaxLength(256);
            entity.HasIndex(a => new { a.InboundEmailId });
        });
    }
}
