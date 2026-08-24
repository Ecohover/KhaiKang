using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.TestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImmutableTraceabilitySources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_test_run_items_id_test_run_id",
                table: "test_run_items",
                columns: new[] { "id", "test_run_id" });

            migrationBuilder.CreateTable(
                name: "bug_test_issue_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bug_issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bug_test_issue_sources", x => x.id);
                    table.ForeignKey(
                        name: "FK_bug_test_issue_sources_issues_bug_issue_id",
                        column: x => x.bug_issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bug_test_issue_sources_issues_test_issue_id",
                        column: x => x.test_issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bug_test_issue_sources_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bug_test_issue_sources_test_workspaces_test_workspace_id",
                        column: x => x.test_workspace_id,
                        principalTable: "test_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bug_test_run_item_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bug_issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_run_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bug_test_run_item_sources", x => x.id);
                    table.ForeignKey(
                        name: "FK_bug_test_run_item_sources_issues_bug_issue_id",
                        column: x => x.bug_issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bug_test_run_item_sources_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bug_test_run_item_sources_test_run_items_test_run_item_id_t~",
                        columns: x => new { x.test_run_item_id, x.test_run_id },
                        principalTable: "test_run_items",
                        principalColumns: new[] { "id", "test_run_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bug_test_run_item_sources_test_runs_test_run_id",
                        column: x => x.test_run_id,
                        principalTable: "test_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bug_test_run_item_sources_test_workspaces_test_workspace_id",
                        column: x => x.test_workspace_id,
                        principalTable: "test_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "test_case_test_issue_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_case_test_issue_sources", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_case_test_issue_sources_issues_test_issue_id",
                        column: x => x.test_issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_case_test_issue_sources_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_case_test_issue_sources_test_cases_test_case_id",
                        column: x => x.test_case_id,
                        principalTable: "test_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_case_test_issue_sources_test_workspaces_test_workspace~",
                        column: x => x.test_workspace_id,
                        principalTable: "test_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "traceability_idempotency_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    state = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    response_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    rejection_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_traceability_idempotency_records", x => x.id);
                    table.CheckConstraint("ck_traceability_idempotency_fingerprint", "length(request_fingerprint) = 64");
                    table.CheckConstraint("ck_traceability_idempotency_terminal", "(state = 'incomplete_transient_failure' AND response_snapshot IS NULL AND rejection_snapshot IS NULL AND completed_at IS NULL) OR (state = 'terminal_success' AND response_snapshot IS NOT NULL AND rejection_snapshot IS NULL AND completed_at IS NOT NULL) OR (state = 'terminal_business_rejection' AND response_snapshot IS NULL AND rejection_snapshot IS NOT NULL AND completed_at IS NOT NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_bug_test_issue_sources_project_id",
                table: "bug_test_issue_sources",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_bug_test_issue_sources_test_issue_id",
                table: "bug_test_issue_sources",
                column: "test_issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_bug_test_issue_sources_test_workspace_id",
                table: "bug_test_issue_sources",
                column: "test_workspace_id");

            migrationBuilder.CreateIndex(
                name: "uq_bug_test_issue_sources_bug",
                table: "bug_test_issue_sources",
                column: "bug_issue_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bug_test_run_item_sources_project_id",
                table: "bug_test_run_item_sources",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_bug_test_run_item_sources_test_run_id",
                table: "bug_test_run_item_sources",
                column: "test_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_bug_test_run_item_sources_test_run_item_id_test_run_id",
                table: "bug_test_run_item_sources",
                columns: new[] { "test_run_item_id", "test_run_id" });

            migrationBuilder.CreateIndex(
                name: "IX_bug_test_run_item_sources_test_workspace_id",
                table: "bug_test_run_item_sources",
                column: "test_workspace_id");

            migrationBuilder.CreateIndex(
                name: "uq_bug_test_run_item_sources_bug",
                table: "bug_test_run_item_sources",
                column: "bug_issue_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_case_test_issue_sources_project_id",
                table: "test_case_test_issue_sources",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_case_test_issue_sources_test_issue_id",
                table: "test_case_test_issue_sources",
                column: "test_issue_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_case_test_issue_sources_test_workspace_id",
                table: "test_case_test_issue_sources",
                column: "test_workspace_id");

            migrationBuilder.CreateIndex(
                name: "uq_test_case_test_issue_sources_case",
                table: "test_case_test_issue_sources",
                column: "test_case_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_traceability_idempotency_actor_key",
                table: "traceability_idempotency_records",
                columns: new[] { "actor_account_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bug_test_issue_sources");

            migrationBuilder.DropTable(
                name: "bug_test_run_item_sources");

            migrationBuilder.DropTable(
                name: "test_case_test_issue_sources");

            migrationBuilder.DropTable(
                name: "traceability_idempotency_records");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_test_run_items_id_test_run_id",
                table: "test_run_items");
        }
    }
}
