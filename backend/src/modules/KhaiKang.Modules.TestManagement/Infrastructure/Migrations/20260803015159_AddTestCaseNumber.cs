using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.TestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTestCaseNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "case_no",
                table: "test_cases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                WITH numbered_cases AS (
                    SELECT c.id,
                           ROW_NUMBER() OVER (
                               PARTITION BY s.test_workspace_id
                               ORDER BY c.created_at, c.id) AS case_no
                    FROM test_cases AS c
                    INNER JOIN test_suites AS s ON s.id = c.test_suite_id
                )
                UPDATE test_cases AS c
                SET case_no = numbered_cases.case_no
                FROM numbered_cases
                WHERE c.id = numbered_cases.id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "case_no",
                table: "test_cases");
        }
    }
}
