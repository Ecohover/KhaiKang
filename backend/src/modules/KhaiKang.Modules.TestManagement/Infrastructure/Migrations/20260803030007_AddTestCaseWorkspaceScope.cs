using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.TestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTestCaseWorkspaceScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "test_workspace_id",
                table: "test_cases",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
                UPDATE test_cases AS c
                SET test_workspace_id = s.test_workspace_id
                FROM test_suites AS s
                WHERE s.id = c.test_suite_id;
                """);

            migrationBuilder.CreateIndex(
                name: "uq_test_cases_workspace_case_no",
                table: "test_cases",
                columns: new[] { "test_workspace_id", "case_no" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_test_cases_workspace_case_no",
                table: "test_cases");

            migrationBuilder.DropColumn(
                name: "test_workspace_id",
                table: "test_cases");
        }
    }
}
