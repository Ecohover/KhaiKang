using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.TestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTestRunNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "run_no",
                table: "test_runs",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE test_runs AS target
                SET run_no = numbered.run_no
                FROM (
                    SELECT
                        id,
                        ROW_NUMBER() OVER (
                            PARTITION BY test_plan_id
                            ORDER BY created_at, id
                        )::integer AS run_no
                    FROM test_runs
                ) AS numbered
                WHERE target.id = numbered.id;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "run_no",
                table: "test_runs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_test_runs_plan_run_no",
                table: "test_runs",
                columns: new[] { "test_plan_id", "run_no" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_test_runs_plan_run_no",
                table: "test_runs");

            migrationBuilder.DropColumn(
                name: "run_no",
                table: "test_runs");
        }
    }
}
