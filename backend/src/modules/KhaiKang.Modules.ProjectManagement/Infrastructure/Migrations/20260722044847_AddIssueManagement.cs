using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KhaiKang.Modules.ProjectManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "issue_priorities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_priorities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issue_statuses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issue_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_no = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    user_story = table.Column<string>(type: "text", nullable: true),
                    definition_of_done = table.Column<string>(type: "text", nullable: true),
                    issue_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_status_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_priority_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignee_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completion_summary = table.Column<string>(type: "text", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issues", x => x.id);
                    table.ForeignKey(
                        name: "fk_issues_assignee_account",
                        column: x => x.assignee_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issues_issue_priority",
                        column: x => x.issue_priority_id,
                        principalTable: "issue_priorities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issues_issue_status",
                        column: x => x.issue_status_id,
                        principalTable: "issue_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issues_issue_type",
                        column: x => x.issue_type_id,
                        principalTable: "issue_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issues_project",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_issues_reporter_account",
                        column: x => x.reporter_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "issue_priorities",
                columns: new[] { "id", "code", "created_at", "created_by_account_id", "description", "is_active", "is_system", "name", "sort_order", "updated_at", "updated_by_account_id", "version" },
                values: new object[,]
                {
                    { new Guid("1cc8b25d-a2e8-40d2-b971-5193ffbf2fe3"), "low", new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "低優先級。", true, true, "Low", 1, new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("3e722449-95fd-4793-a3c1-8437acd5d5e4"), "high", new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "高優先級。", true, true, "High", 3, new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("8178b599-48da-4743-a560-d3633477f1ac"), "critical", new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "需要立即關注的關鍵優先級。", true, true, "Critical", 4, new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("dc8a3357-7002-46f8-98c8-ad46476d7515"), "medium", new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "一般預設優先級。", true, true, "Medium", 2, new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 }
                });

            migrationBuilder.InsertData(
                table: "issue_statuses",
                columns: new[] { "id", "category", "code", "created_at", "created_by_account_id", "description", "is_active", "is_system", "name", "sort_order", "updated_at", "updated_by_account_id", "version" },
                values: new object[,]
                {
                    { new Guid("343e8e3c-4baa-41a3-bd3e-7840ae938244"), "doing", "in_progress", new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "表示任務目前正在處理中。", true, true, "In Progress", 2, new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("62059722-9c39-4bce-b805-2490cdb6fe77"), "done", "completed", new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "表示任務已完成。", true, true, "Completed", 4, new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("8b211fd1-20f5-4bcb-a3f2-2bb222472c10"), "todo", "created", new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "表示任務已建立，尚未正式進入處理。", true, true, "Created", 1, new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("b48dfc2c-1084-45ff-8c93-ac7d9613943b"), "doing", "verifying", new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "表示任務目前正在驗證中。", true, true, "Verifying", 3, new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 }
                });

            migrationBuilder.InsertData(
                table: "issue_types",
                columns: new[] { "id", "code", "created_at", "created_by_account_id", "description", "is_active", "is_system", "name", "sort_order", "updated_at", "updated_by_account_id", "version" },
                values: new object[,]
                {
                    { new Guid("28395b03-a812-4e53-bbde-85a598166d71"), "story", new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "用來表示具體需求或使用者價值。", true, true, "Story", 1, new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("66f5813f-a357-47de-961a-572744bc25a9"), "bug", new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "用來表示缺陷或異常問題。", true, true, "Bug", 3, new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("7c80596d-325c-43c1-9e1b-757b14f975e8"), "spike", new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "用來表示研究、驗證或技術探索工作。", true, true, "Spike", 4, new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("d179951d-a8b4-4a37-8059-e79dc8ea25fb"), "task", new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "用來表示一般執行工作項目。", true, true, "Task", 2, new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "uq_issue_priorities_code",
                table: "issue_priorities",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_issue_statuses_code",
                table: "issue_statuses",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_issue_types_code",
                table: "issue_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_issues_assignee_account_id",
                table: "issues",
                column: "assignee_account_id");

            migrationBuilder.CreateIndex(
                name: "idx_issues_issue_priority_id",
                table: "issues",
                column: "issue_priority_id");

            migrationBuilder.CreateIndex(
                name: "idx_issues_issue_status_id",
                table: "issues",
                column: "issue_status_id");

            migrationBuilder.CreateIndex(
                name: "idx_issues_issue_type_id",
                table: "issues",
                column: "issue_type_id");

            migrationBuilder.CreateIndex(
                name: "idx_issues_project_assignee",
                table: "issues",
                columns: new[] { "project_id", "assignee_account_id" });

            migrationBuilder.CreateIndex(
                name: "idx_issues_project_id",
                table: "issues",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "idx_issues_project_status",
                table: "issues",
                columns: new[] { "project_id", "issue_status_id" });

            migrationBuilder.CreateIndex(
                name: "idx_issues_reporter_account_id",
                table: "issues",
                column: "reporter_account_id");

            migrationBuilder.CreateIndex(
                name: "uq_issues_project_issue_no",
                table: "issues",
                columns: new[] { "project_id", "issue_no" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issues");

            migrationBuilder.DropTable(
                name: "issue_priorities");

            migrationBuilder.DropTable(
                name: "issue_statuses");

            migrationBuilder.DropTable(
                name: "issue_types");
        }
    }
}
