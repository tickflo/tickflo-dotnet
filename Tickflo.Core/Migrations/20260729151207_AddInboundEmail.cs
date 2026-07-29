#nullable disable

namespace Tickflo.Core.Migrations;

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

/// <inheritdoc />
public partial class AddInboundEmail : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "inbound_email_routes",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                created_by_user_id = table.Column<int>(type: "integer", nullable: true),
                local_part = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                default_ticket_type = table.Column<string>(type: "text", nullable: true),
                default_ticket_priority = table.Column<string>(type: "text", nullable: true),
                default_location_id = table.Column<int>(type: "integer", nullable: true),
                active = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by_user_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_inbound_email_routes", x => x.id));

        migrationBuilder.CreateTable(
            name: "inbound_emails",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                route_id = table.Column<int>(type: "integer", nullable: false),
                from_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                from_name = table.Column<string>(type: "text", nullable: true),
                to_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                subject = table.Column<string>(type: "character varying(998)", maxLength: 998, nullable: false),
                body_plain = table.Column<string>(type: "text", nullable: false),
                body_html = table.Column<string>(type: "text", nullable: true),
                message_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                in_reply_to_email_id = table.Column<int>(type: "integer", nullable: true),
                ticket_id = table.Column<int>(type: "integer", nullable: true),
                contact_id = table.Column<int>(type: "integer", nullable: true),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Pending"),
                error_message = table.Column<string>(type: "text", nullable: true),
                raw_payload = table.Column<string>(type: "text", nullable: true),
                received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_inbound_emails", x => x.id);
                table.ForeignKey(
                    name: "fk_inbound_emails_inbound_email_routes_route_id",
                    column: x => x.route_id,
                    principalTable: "inbound_email_routes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "inbound_email_attachments",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                inbound_email_id = table.Column<int>(type: "integer", nullable: false),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                content_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                size = table.Column<long>(type: "bigint", nullable: false),
                mailgun_url = table.Column<string>(type: "text", nullable: true),
                storage_path = table.Column<string>(type: "text", nullable: true),
                public_url = table.Column<string>(type: "text", nullable: true),
                is_stored = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_inbound_email_attachments", x => x.id);
                table.ForeignKey(
                    name: "fk_inbound_email_attachments_inbound_emails_inbound_email_id",
                    column: x => x.inbound_email_id,
                    principalTable: "inbound_emails",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_inbound_email_attachments_inbound_email_id",
            table: "inbound_email_attachments",
            column: "inbound_email_id");

        migrationBuilder.CreateIndex(
            name: "ix_inbound_email_routes_workspace_id_local_part",
            table: "inbound_email_routes",
            columns: ["workspace_id", "local_part"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_inbound_emails_message_id",
            table: "inbound_emails",
            column: "message_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_inbound_emails_route_id",
            table: "inbound_emails",
            column: "route_id");

        migrationBuilder.CreateIndex(
            name: "ix_inbound_emails_workspace_id_received_at",
            table: "inbound_emails",
            columns: ["workspace_id", "received_at"]);

        migrationBuilder.CreateIndex(
            name: "ix_inbound_emails_workspace_id_status",
            table: "inbound_emails",
            columns: ["workspace_id", "status"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "inbound_email_attachments");

        migrationBuilder.DropTable(
            name: "inbound_emails");

        migrationBuilder.DropTable(
            name: "inbound_email_routes");
    }
}
