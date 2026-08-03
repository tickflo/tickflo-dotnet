#nullable disable

namespace Tickflo.Core.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class BackfillDefaultInboundEmailRoutes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) =>
        // Create a default inbound email route for every workspace that doesn't already
        // have one, using the workspace slug as the local part. Workspaces whose slug
        // collides with an existing route's local part are skipped (local parts are
        // globally unique) and can be given a route manually.
        migrationBuilder.Sql("""
            INSERT INTO inbound_email_routes (workspace_id, created_by_user_id, local_part, label, active, created_at)
            SELECT w.id, w.created_by, w.slug, 'Default', true, NOW() AT TIME ZONE 'utc'
            FROM workspaces w
            WHERE NOT EXISTS (
                SELECT 1 FROM inbound_email_routes r WHERE r.workspace_id = w.id
            )
            AND NOT EXISTS (
                SELECT 1 FROM inbound_email_routes r2 WHERE r2.local_part = w.slug
            );
            """);

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) =>
        // Best-effort reverse: remove the auto-created default routes (identified by
        // label 'Default' and local part matching the workspace slug).
        migrationBuilder.Sql("""
            DELETE FROM inbound_email_routes r
            USING workspaces w
            WHERE r.workspace_id = w.id
              AND r.label = 'Default'
              AND r.local_part = w.slug;
            """);
}
