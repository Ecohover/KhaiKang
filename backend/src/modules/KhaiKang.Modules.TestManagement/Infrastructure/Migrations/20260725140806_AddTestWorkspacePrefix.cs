using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.TestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTestWorkspacePrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "prefix",
                table: "test_workspaces",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH numbered AS (
                    SELECT id, ROW_NUMBER() OVER (ORDER BY created_at, id) AS sequence
                    FROM test_workspaces
                )
                UPDATE test_workspaces AS workspace
                SET prefix = 'TW' || numbered.sequence
                FROM numbered
                WHERE workspace.id = numbered.id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "prefix",
                table: "test_workspaces",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_test_workspaces_prefix",
                table: "test_workspaces",
                column: "prefix",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_test_workspaces_prefix",
                table: "test_workspaces");

            migrationBuilder.DropColumn(
                name: "prefix",
                table: "test_workspaces");
        }
    }
}
