#nullable disable

namespace Tickflo.Core.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class UniqueInboundEmailRouteLocalPart : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_inbound_email_routes_workspace_id_local_part",
            table: "inbound_email_routes");

        migrationBuilder.CreateIndex(
            name: "ix_inbound_email_routes_local_part",
            table: "inbound_email_routes",
            column: "local_part",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_inbound_email_routes_local_part",
            table: "inbound_email_routes");

        migrationBuilder.CreateIndex(
            name: "ix_inbound_email_routes_workspace_id_local_part",
            table: "inbound_email_routes",
            columns: ["workspace_id", "local_part"],
            unique: true);
    }
}
