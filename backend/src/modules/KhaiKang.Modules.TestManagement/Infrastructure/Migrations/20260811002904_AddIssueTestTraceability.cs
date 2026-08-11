using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.TestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueTestTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "test_issue_id",
                table: "test_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "test_issue_project_id",
                table: "test_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "test_issue_id",
                table: "test_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "test_issue_project_id",
                table: "test_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "test_case_requirement_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_case_requirement_links", x => x.id);
                    table.CheckConstraint("ck_test_case_requirement_links_deleted_metadata", "(is_deleted = false AND deleted_at IS NULL AND deleted_by_account_id IS NULL) OR (is_deleted = true AND deleted_at IS NOT NULL AND deleted_by_account_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_test_case_requirement_links_case",
                        column: x => x.test_case_id,
                        principalTable: "test_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_test_case_requirement_links_deleted_by_account",
                        column: x => x.deleted_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_case_requirement_links_issue",
                        column: x => x.requirement_issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_case_requirement_links_project",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_case_requirement_links_workspace",
                        column: x => x.test_workspace_id,
                        principalTable: "test_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_run_bug_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bug_issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_run_bug_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_run_bug_links_issue",
                        column: x => x.bug_issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_run_bug_links_project",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_run_bug_links_run",
                        column: x => x.test_run_id,
                        principalTable: "test_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_test_run_bug_links_workspace",
                        column: x => x.test_workspace_id,
                        principalTable: "test_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_test_runs_test_issue_id",
                table: "test_runs",
                column: "test_issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_runs_test_issue_project_id",
                table: "test_runs",
                column: "test_issue_project_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_test_runs_test_issue_pair",
                table: "test_runs",
                sql: "(test_issue_project_id IS NULL AND test_issue_id IS NULL) OR (test_issue_project_id IS NOT NULL AND test_issue_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_test_plans_test_issue_id",
                table: "test_plans",
                column: "test_issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_plans_test_issue_project_id",
                table: "test_plans",
                column: "test_issue_project_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_test_plans_test_issue_pair",
                table: "test_plans",
                sql: "(test_issue_project_id IS NULL AND test_issue_id IS NULL) OR (test_issue_project_id IS NOT NULL AND test_issue_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_test_case_requirement_links_issue_active",
                table: "test_case_requirement_links",
                columns: new[] { "requirement_issue_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "idx_test_case_requirement_links_workspace_project_active",
                table: "test_case_requirement_links",
                columns: new[] { "test_workspace_id", "project_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_test_case_requirement_links_deleted_by_account_id",
                table: "test_case_requirement_links",
                column: "deleted_by_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_case_requirement_links_project_id",
                table: "test_case_requirement_links",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "uq_test_case_requirement_links_active",
                table: "test_case_requirement_links",
                columns: new[] { "test_case_id", "requirement_issue_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "idx_test_run_bug_links_run",
                table: "test_run_bug_links",
                column: "test_run_id");

            migrationBuilder.CreateIndex(
                name: "idx_test_run_bug_links_workspace_project",
                table: "test_run_bug_links",
                columns: new[] { "test_workspace_id", "project_id" });

            migrationBuilder.CreateIndex(
                name: "IX_test_run_bug_links_project_id",
                table: "test_run_bug_links",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "uq_test_run_bug_links_bug_issue",
                table: "test_run_bug_links",
                column: "bug_issue_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_test_plans_test_issue",
                table: "test_plans",
                column: "test_issue_id",
                principalTable: "issues",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_test_plans_test_issue_project",
                table: "test_plans",
                column: "test_issue_project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_test_runs_test_issue",
                table: "test_runs",
                column: "test_issue_id",
                principalTable: "issues",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_test_runs_test_issue_project",
                table: "test_runs",
                column: "test_issue_project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_test_plans_test_issue",
                table: "test_plans");

            migrationBuilder.DropForeignKey(
                name: "fk_test_plans_test_issue_project",
                table: "test_plans");

            migrationBuilder.DropForeignKey(
                name: "fk_test_runs_test_issue",
                table: "test_runs");

            migrationBuilder.DropForeignKey(
                name: "fk_test_runs_test_issue_project",
                table: "test_runs");

            migrationBuilder.DropTable(
                name: "test_case_requirement_links");

            migrationBuilder.DropTable(
                name: "test_run_bug_links");

            migrationBuilder.DropIndex(
                name: "IX_test_runs_test_issue_id",
                table: "test_runs");

            migrationBuilder.DropIndex(
                name: "IX_test_runs_test_issue_project_id",
                table: "test_runs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_test_runs_test_issue_pair",
                table: "test_runs");

            migrationBuilder.DropIndex(
                name: "IX_test_plans_test_issue_id",
                table: "test_plans");

            migrationBuilder.DropIndex(
                name: "IX_test_plans_test_issue_project_id",
                table: "test_plans");

            migrationBuilder.DropCheckConstraint(
                name: "ck_test_plans_test_issue_pair",
                table: "test_plans");

            migrationBuilder.DropColumn(
                name: "test_issue_id",
                table: "test_runs");

            migrationBuilder.DropColumn(
                name: "test_issue_project_id",
                table: "test_runs");

            migrationBuilder.DropColumn(
                name: "test_issue_id",
                table: "test_plans");

            migrationBuilder.DropColumn(
                name: "test_issue_project_id",
                table: "test_plans");
        }
    }
}
