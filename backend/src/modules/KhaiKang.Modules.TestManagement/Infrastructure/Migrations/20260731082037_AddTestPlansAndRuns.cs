using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.TestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTestPlansAndRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "test_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_no = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_plans_workspace",
                        column: x => x.test_workspace_id,
                        principalTable: "test_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_plan_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_plan_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_plan_items_case",
                        column: x => x.test_case_id,
                        principalTable: "test_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_plan_items_plan",
                        column: x => x.test_plan_id,
                        principalTable: "test_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_by_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_runs_plan",
                        column: x => x.test_plan_id,
                        principalTable: "test_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_runs_started_by_account",
                        column: x => x.started_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "test_run_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    case_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    case_description = table.Column<string>(type: "text", nullable: true),
                    preconditions = table.Column<string>(type: "text", nullable: true),
                    overall_expected_result = table.Column<string>(type: "text", nullable: true),
                    result_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actual_result = table.Column<string>(type: "text", nullable: true),
                    executed_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    executed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_run_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_run_items_case",
                        column: x => x.test_case_id,
                        principalTable: "test_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_run_items_executed_by_account",
                        column: x => x.executed_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_run_items_run",
                        column: x => x.test_run_id,
                        principalTable: "test_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_run_item_step_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_run_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_no = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    expected_result = table.Column<string>(type: "text", nullable: false),
                    result_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actual_result = table.Column<string>(type: "text", nullable: true),
                    executed_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    executed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_run_item_step_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_run_item_steps_executed_by_account",
                        column: x => x.executed_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_run_item_steps_run_item",
                        column: x => x.test_run_item_id,
                        principalTable: "test_run_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_test_plan_items_plan_sort_order",
                table: "test_plan_items",
                columns: new[] { "test_plan_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_test_plan_items_test_case_id",
                table: "test_plan_items",
                column: "test_case_id");

            migrationBuilder.CreateIndex(
                name: "uq_test_plan_items_plan_case",
                table: "test_plan_items",
                columns: new[] { "test_plan_id", "test_case_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_test_plans_workspace_status",
                table: "test_plans",
                columns: new[] { "test_workspace_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_test_plans_workspace_plan_no",
                table: "test_plans",
                columns: new[] { "test_workspace_id", "plan_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_run_item_step_results_executed_by_account_id",
                table: "test_run_item_step_results",
                column: "executed_by_account_id");

            migrationBuilder.CreateIndex(
                name: "uq_test_run_item_steps_item_step_no",
                table: "test_run_item_step_results",
                columns: new[] { "test_run_item_id", "step_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_test_run_items_run_sort_order",
                table: "test_run_items",
                columns: new[] { "test_run_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_test_run_items_executed_by_account_id",
                table: "test_run_items",
                column: "executed_by_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_run_items_test_case_id",
                table: "test_run_items",
                column: "test_case_id");

            migrationBuilder.CreateIndex(
                name: "idx_test_runs_plan_status",
                table: "test_runs",
                columns: new[] { "test_plan_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_test_runs_started_by_account_id",
                table: "test_runs",
                column: "started_by_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_plan_items");

            migrationBuilder.DropTable(
                name: "test_run_item_step_results");

            migrationBuilder.DropTable(
                name: "test_run_items");

            migrationBuilder.DropTable(
                name: "test_runs");

            migrationBuilder.DropTable(
                name: "test_plans");
        }
    }
}
