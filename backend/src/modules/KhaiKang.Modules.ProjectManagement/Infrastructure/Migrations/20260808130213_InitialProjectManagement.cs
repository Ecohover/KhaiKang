using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KhaiKang.Modules.ProjectManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialProjectManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE public.project_number_counters
                (
                    counter_type varchar(20) NOT NULL,
                    scope_id uuid NOT NULL,
                    last_value integer NOT NULL,
                    CONSTRAINT pk_project_number_counters PRIMARY KEY (counter_type, scope_id),
                    CONSTRAINT ck_project_number_counters_type CHECK (counter_type IN ('issue')),
                    CONSTRAINT ck_project_number_counters_value CHECK (last_value > 0)
                );

                CREATE FUNCTION public.next_project_number(
                    p_counter_type varchar,
                    p_scope_id uuid)
                RETURNS integer
                LANGUAGE sql
                VOLATILE
                AS $function$
                    INSERT INTO public.project_number_counters
                        (counter_type, scope_id, last_value)
                    VALUES
                        (p_counter_type, p_scope_id, 1)
                    ON CONFLICT (counter_type, scope_id)
                    DO UPDATE
                    SET last_value = public.project_number_counters.last_value + 1
                    RETURNING last_value;
                $function$;
                """);

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
                name: "project_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_audit_events_actor",
                        column: x => x.actor_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_project_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.ForeignKey(
                        name: "fk_projects_created_by_account",
                        column: x => x.created_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_role_permissions_permission",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_project_role_permissions_role",
                        column: x => x.project_role_id,
                        principalTable: "project_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_members_account",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_project_members_project",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "issue_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    storage_key = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    file_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_issue_attachments_issue",
                        column: x => x.issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_issue_attachments_uploaded_by_account",
                        column: x => x.uploaded_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_member_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_member_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_member_roles_member",
                        column: x => x.project_member_id,
                        principalTable: "project_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_project_member_roles_role",
                        column: x => x.project_role_id,
                        principalTable: "project_roles",
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

            migrationBuilder.InsertData(
                table: "project_roles",
                columns: new[] { "id", "code", "created_at", "created_by_account_id", "description", "is_active", "is_system", "name", "sort_order", "updated_at", "updated_by_account_id", "version" },
                values: new object[,]
                {
                    { new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), "owner", new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "專案擁有者", true, true, "Owner", 1, new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), "manager", new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "專案管理者", true, true, "Manager", 2, new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("c5684ccd-30b0-43aa-85ef-7f1c23835492"), "reviewer", new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "專案審查者", true, true, "Reviewer", 4, new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("ead22957-af22-47eb-a7de-782145087141"), "contributor", new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "專案參與者", true, true, "Contributor", 3, new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 }
                });

            migrationBuilder.InsertData(
                table: "project_role_permissions",
                columns: new[] { "id", "created_at", "created_by_account_id", "permission_id", "project_role_id", "updated_at", "updated_by_account_id", "version" },
                values: new object[,]
                {
                    { new Guid("06a0d3a1-6481-bacc-bf1c-aee267417d30"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("084b565e-e59c-4b48-9a4d-2de2a58e0a9d"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("09d1f8f6-e79f-2549-4831-57b5a466b4c4"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("f622c88d-d0f0-40ed-86c0-bba2c3ff44c9"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("13deec66-f65a-27c8-0bb7-6ba91139d72f"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("811e7203-b5ab-4b2c-83aa-8e071f68b36f"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("21ef0d84-116e-fdf9-2dd5-acfdfd36f1a3"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("2f36b9f4-b7f1-4cd2-af25-46393d560b13"), new Guid("c5684ccd-30b0-43aa-85ef-7f1c23835492"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("24db33d8-ef7f-b4d8-430c-cbbeecdf7f0f"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("bce4a59e-47d6-4664-bda4-c1ea66b50ec1"), new Guid("ead22957-af22-47eb-a7de-782145087141"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("2604dda8-60ba-05de-d32e-c2dbd8d76bdf"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("bb8afb36-8753-4383-bce7-83065a92c0d3"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("288b0347-ad68-8719-c2d1-994aa373b7cd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("811e7203-b5ab-4b2c-83aa-8e071f68b36f"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("30a8a773-c19c-bbdb-209e-f6f8db9cf14e"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("f622c88d-d0f0-40ed-86c0-bba2c3ff44c9"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("33e94581-847a-d7f3-704d-f90bba5fc295"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("bce4a59e-47d6-4664-bda4-c1ea66b50ec1"), new Guid("c5684ccd-30b0-43aa-85ef-7f1c23835492"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("385b399f-53a5-3276-7af4-50f7a5f51094"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("2f36b9f4-b7f1-4cd2-af25-46393d560b13"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("3ffe1b27-a34a-df3f-2dbb-0b6c5f3590f2"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("23cffaf6-9d01-4116-9a79-a4970bc01eae"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("4c1e77da-615a-901c-3ff4-4d7f087cceb2"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("3ed83136-9532-4e6e-8adb-b73ae9863a2c"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("4cccee23-9e2f-95c9-c31d-c907cb5be262"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("bce4a59e-47d6-4664-bda4-c1ea66b50ec1"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("51894281-7d9b-8c4c-a6b2-caa5d4b4b572"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("081eced1-97b4-4f3e-bb05-56cc3053de6f"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("5521f42d-10ad-a57a-2a6a-27580593012e"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("3c2bdd52-7445-4a16-9750-1b28d97bf109"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("5664acb8-a42e-bf85-3b01-87392c6af413"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("60098112-f880-40d8-97d6-bed5784f83a0"), new Guid("ead22957-af22-47eb-a7de-782145087141"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("5b915700-ab90-4cd7-5be9-6ee4170dc5b5"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("23cffaf6-9d01-4116-9a79-a4970bc01eae"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("5d43c4cc-275e-37d2-2aee-2b334478cdec"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("2f36b9f4-b7f1-4cd2-af25-46393d560b13"), new Guid("ead22957-af22-47eb-a7de-782145087141"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("66274a3a-f0c7-2ccc-0464-9477bb2eb2f1"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("081eced1-97b4-4f3e-bb05-56cc3053de6f"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("6ce13b9e-40e6-714e-a06e-3e9d5ca6ba57"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("bce4a59e-47d6-4664-bda4-c1ea66b50ec1"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("7492771b-b822-715a-42f4-c9037f8e470e"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("084b565e-e59c-4b48-9a4d-2de2a58e0a9d"), new Guid("ead22957-af22-47eb-a7de-782145087141"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("7bd83598-3fbc-cfee-018f-ce06caa5557f"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("60098112-f880-40d8-97d6-bed5784f83a0"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("7d3e0fb5-621d-7b7b-e25b-5e5b6071536c"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("3c2bdd52-7445-4a16-9750-1b28d97bf109"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("7e0b87b3-8e88-a35b-0f7e-cefc0668cd79"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("1e42b09d-9839-4e8c-951c-38c941f9e4ca"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("86141116-7282-5a53-7909-22b3f100fb42"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("60098112-f880-40d8-97d6-bed5784f83a0"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("92f69bdd-ef64-70c9-0b80-d0f0e20b113f"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("a5fa36de-f8eb-491a-981c-f2f17244fa2b"), new Guid("ead22957-af22-47eb-a7de-782145087141"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("93e8919c-4290-a88f-691f-79561e047706"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("1e42b09d-9839-4e8c-951c-38c941f9e4ca"), new Guid("ead22957-af22-47eb-a7de-782145087141"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("96c506cf-9f94-c7f0-8b37-fb5e36d3c942"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("1e42b09d-9839-4e8c-951c-38c941f9e4ca"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("99b841b8-6726-5ae3-696b-5627dc4ba212"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("a5fa36de-f8eb-491a-981c-f2f17244fa2b"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("ab7c0798-0b76-41fd-2347-fa7098996c4c"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("084b565e-e59c-4b48-9a4d-2de2a58e0a9d"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("ad5b3190-abee-b56b-cdf2-33da880f5c0d"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("0cc976a0-4f5a-4ce4-9309-6f4476522aa6"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("b71a6531-a4ad-830f-fa2b-fe76b71663f6"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("084b565e-e59c-4b48-9a4d-2de2a58e0a9d"), new Guid("c5684ccd-30b0-43aa-85ef-7f1c23835492"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("bb483fe6-2bfd-cf0c-96d3-249f659a0bea"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("2f36b9f4-b7f1-4cd2-af25-46393d560b13"), new Guid("836e894f-ca1d-4fd9-af14-b987882400dd"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("bb795ad9-8d86-b5d1-c5c9-ff5a8b85377b"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("081eced1-97b4-4f3e-bb05-56cc3053de6f"), new Guid("ead22957-af22-47eb-a7de-782145087141"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("c1ab8a31-12ad-ae37-f6f6-f4ebd9388803"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("a5fa36de-f8eb-491a-981c-f2f17244fa2b"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("cc648413-32df-b9ce-6d4f-15b5338e85c7"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("3ed83136-9532-4e6e-8adb-b73ae9863a2c"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("d7731f17-381a-1370-3cad-5bb18f577f51"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("3c2bdd52-7445-4a16-9750-1b28d97bf109"), new Guid("ead22957-af22-47eb-a7de-782145087141"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("deb893dd-4005-6b3e-cce7-273101e44b58"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("bb8afb36-8753-4383-bce7-83065a92c0d3"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("eb2537bb-c144-5722-3327-ce1fa43ea265"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("1e42b09d-9839-4e8c-951c-38c941f9e4ca"), new Guid("c5684ccd-30b0-43aa-85ef-7f1c23835492"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("f25f7d44-4184-8932-7b42-269884b8ce8f"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("3ed83136-9532-4e6e-8adb-b73ae9863a2c"), new Guid("c5684ccd-30b0-43aa-85ef-7f1c23835492"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("f6cf006e-4182-6ea6-219a-5bb2b6627857"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("60098112-f880-40d8-97d6-bed5784f83a0"), new Guid("c5684ccd-30b0-43aa-85ef-7f1c23835492"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("faf62a01-0652-7193-e98d-610e1534037e"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("3ed83136-9532-4e6e-8adb-b73ae9863a2c"), new Guid("ead22957-af22-47eb-a7de-782145087141"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("ff720db8-68d9-f312-14d8-e48eb43a11d4"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("0cc976a0-4f5a-4ce4-9309-6f4476522aa6"), new Guid("4f5961ac-5a4b-49e1-a73c-451d43a39718"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "idx_issue_attachments_issue_deleted",
                table: "issue_attachments",
                columns: new[] { "issue_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "idx_issue_attachments_issue_id",
                table: "issue_attachments",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "idx_issue_attachments_uploaded_by_account_id",
                table: "issue_attachments",
                column: "uploaded_by_account_id");

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

            migrationBuilder.CreateIndex(
                name: "idx_project_audit_events_occurred_at",
                table: "project_audit_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_project_audit_events_actor_id",
                table: "project_audit_events",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "idx_project_member_roles_project_member_id",
                table: "project_member_roles",
                column: "project_member_id");

            migrationBuilder.CreateIndex(
                name: "idx_project_member_roles_project_role_id",
                table: "project_member_roles",
                column: "project_role_id");

            migrationBuilder.CreateIndex(
                name: "uq_project_member_roles_member_role",
                table: "project_member_roles",
                columns: new[] { "project_member_id", "project_role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_project_members_account_id",
                table: "project_members",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "idx_project_members_project_id",
                table: "project_members",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "idx_project_members_project_status",
                table: "project_members",
                columns: new[] { "project_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_project_members_active_member",
                table: "project_members",
                columns: new[] { "project_id", "account_id" },
                unique: true,
                filter: "status = 'active'");

            migrationBuilder.CreateIndex(
                name: "idx_project_role_permissions_permission_id",
                table: "project_role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "idx_project_role_permissions_project_role_id",
                table: "project_role_permissions",
                column: "project_role_id");

            migrationBuilder.CreateIndex(
                name: "uq_project_role_permissions_role_permission",
                table: "project_role_permissions",
                columns: new[] { "project_role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_project_roles_code",
                table: "project_roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_projects_created_by_account_id",
                table: "projects",
                column: "created_by_account_id");

            migrationBuilder.CreateIndex(
                name: "uq_projects_code",
                table: "projects",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS public.next_project_number(varchar, uuid);");

            migrationBuilder.DropTable(
                name: "project_number_counters");

            migrationBuilder.DropTable(
                name: "issue_attachments");

            migrationBuilder.DropTable(
                name: "project_audit_events");

            migrationBuilder.DropTable(
                name: "project_member_roles");

            migrationBuilder.DropTable(
                name: "project_role_permissions");

            migrationBuilder.DropTable(
                name: "issues");

            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropTable(
                name: "project_roles");

            migrationBuilder.DropTable(
                name: "issue_priorities");

            migrationBuilder.DropTable(
                name: "issue_statuses");

            migrationBuilder.DropTable(
                name: "issue_types");

            migrationBuilder.DropTable(
                name: "projects");
        }
    }
}
