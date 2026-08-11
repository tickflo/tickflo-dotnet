#nullable disable

namespace Tickflo.Core.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class AddWorkspaceIdToTicketInventory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "workspace_id",
            table: "ticket_inventory",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        // Backfill workspace_id from parent ticket
        migrationBuilder.Sql("""
            UPDATE ticket_inventory ti
            SET workspace_id = t.workspace_id
            FROM tickets t
            WHERE ti.ticket_id = t.id
              AND ti.workspace_id = 0;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
            name: "workspace_id",
            table: "ticket_inventory");
}
