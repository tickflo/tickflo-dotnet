using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tickflo.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddFileStorageWorkspaceCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "fk_file_storage_workspaces_workspace_id",
                table: "file_storage",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_file_storage_workspaces_workspace_id",
                table: "file_storage");
        }
    }
}
