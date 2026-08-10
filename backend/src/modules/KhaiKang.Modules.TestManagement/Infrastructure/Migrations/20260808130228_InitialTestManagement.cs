using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.TestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialTestManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE public.test_number_counters
                (
                    counter_type varchar(20) NOT NULL,
                    scope_id uuid NOT NULL,
                    last_value integer NOT NULL,
                    CONSTRAINT pk_test_number_counters PRIMARY KEY (counter_type, scope_id),
                    CONSTRAINT ck_test_number_counters_type CHECK (counter_type IN ('case', 'plan', 'run')),
                    CONSTRAINT ck_test_number_counters_value CHECK (last_value > 0)
                );

                CREATE FUNCTION public.next_test_number(
                    p_counter_type varchar,
                    p_scope_id uuid)
                RETURNS integer
                LANGUAGE sql
                VOLATILE
                AS $function$
                    INSERT INTO public.test_number_counters
                        (counter_type, scope_id, last_value)
                    VALUES
                        (p_counter_type, p_scope_id, 1)
                    ON CONFLICT (counter_type, scope_id)
                    DO UPDATE
                    SET last_value = public.test_number_counters.last_value + 1
                    RETURNING last_value;
                $function$;
                """);

            migrationBuilder.CreateTable(
                name: "test_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("pk_test_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "test_workspaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
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
                    table.PrimaryKey("pk_test_workspaces", x => x.id);
                });

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
                name: "test_suites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_suites", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_suites_parent",
                        column: x => x.parent_id,
                        principalTable: "test_suites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_suites_workspace",
                        column: x => x.test_workspace_id,
                        principalTable: "test_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_workspace_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("pk_test_workspace_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_workspace_members_account",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_workspace_members_workspace",
                        column: x => x.test_workspace_id,
                        principalTable: "test_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_workspace_projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_workspace_projects", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_workspace_projects_project",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_workspace_projects_workspace",
                        column: x => x.test_workspace_id,
                        principalTable: "test_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_no = table.Column<int>(type: "integer", nullable: false),
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
                name: "test_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_suite_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_no = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    preconditions = table.Column<string>(type: "text", nullable: true),
                    overall_expected_result = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_cases", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_cases_test_suite",
                        column: x => x.test_suite_id,
                        principalTable: "test_suites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "test_case_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_test_case_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_case_attachments_case",
                        column: x => x.test_case_id,
                        principalTable: "test_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_test_case_attachments_uploaded_by_account",
                        column: x => x.uploaded_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "test_case_steps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_no = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    expected_result = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_case_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_case_steps_test_case",
                        column: x => x.test_case_id,
                        principalTable: "test_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_case_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_case_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_case_tags_case",
                        column: x => x.test_case_id,
                        principalTable: "test_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_test_case_tags_tag",
                        column: x => x.test_tag_id,
                        principalTable: "test_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "test_run_item_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_run_item_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_test_run_item_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_run_item_attachments_item",
                        column: x => x.test_run_item_id,
                        principalTable: "test_run_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_test_run_item_attachments_uploaded_by_account",
                        column: x => x.uploaded_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "idx_test_case_attachments_case_deleted",
                table: "test_case_attachments",
                columns: new[] { "test_case_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_test_case_attachments_uploaded_by_account_id",
                table: "test_case_attachments",
                column: "uploaded_by_account_id");

            migrationBuilder.CreateIndex(
                name: "idx_test_case_steps_test_case_id",
                table: "test_case_steps",
                column: "test_case_id");

            migrationBuilder.CreateIndex(
                name: "uq_test_case_steps_case_step_no",
                table: "test_case_steps",
                columns: new[] { "test_case_id", "step_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_test_case_tags_test_case_id",
                table: "test_case_tags",
                column: "test_case_id");

            migrationBuilder.CreateIndex(
                name: "idx_test_case_tags_test_tag_id",
                table: "test_case_tags",
                column: "test_tag_id");

            migrationBuilder.CreateIndex(
                name: "uq_test_case_tags_case_tag",
                table: "test_case_tags",
                columns: new[] { "test_case_id", "test_tag_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_test_cases_test_suite_id",
                table: "test_cases",
                column: "test_suite_id");

            migrationBuilder.CreateIndex(
                name: "idx_test_cases_test_suite_sort_order",
                table: "test_cases",
                columns: new[] { "test_suite_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "idx_test_cases_test_suite_status",
                table: "test_cases",
                columns: new[] { "test_suite_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_test_cases_workspace_case_no",
                table: "test_cases",
                columns: new[] { "test_workspace_id", "case_no" },
                unique: true);

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
                name: "idx_test_run_item_attachments_item_deleted",
                table: "test_run_item_attachments",
                columns: new[] { "test_run_item_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_test_run_item_attachments_uploaded_by_account_id",
                table: "test_run_item_attachments",
                column: "uploaded_by_account_id");

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

            migrationBuilder.CreateIndex(
                name: "uq_test_runs_plan_run_no",
                table: "test_runs",
                columns: new[] { "test_plan_id", "run_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_test_suites_workspace_parent_sort_order",
                table: "test_suites",
                columns: new[] { "test_workspace_id", "parent_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_test_suites_parent_id",
                table: "test_suites",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "idx_test_tags_status",
                table: "test_tags",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_test_tags_name",
                table: "test_tags",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_test_workspace_members_account_id",
                table: "test_workspace_members",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "idx_test_workspace_members_workspace_status",
                table: "test_workspace_members",
                columns: new[] { "test_workspace_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_test_workspace_members_workspace_account",
                table: "test_workspace_members",
                columns: new[] { "test_workspace_id", "account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_test_workspace_projects_project_id",
                table: "test_workspace_projects",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "uq_test_workspace_projects_workspace_project",
                table: "test_workspace_projects",
                columns: new[] { "test_workspace_id", "project_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_test_workspaces_name",
                table: "test_workspaces",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_test_workspaces_prefix",
                table: "test_workspaces",
                column: "prefix",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS public.next_test_number(varchar, uuid);");

            migrationBuilder.DropTable(
                name: "test_number_counters");

            migrationBuilder.DropTable(
                name: "test_case_attachments");

            migrationBuilder.DropTable(
                name: "test_case_steps");

            migrationBuilder.DropTable(
                name: "test_case_tags");

            migrationBuilder.DropTable(
                name: "test_plan_items");

            migrationBuilder.DropTable(
                name: "test_run_item_attachments");

            migrationBuilder.DropTable(
                name: "test_run_item_step_results");

            migrationBuilder.DropTable(
                name: "test_workspace_members");

            migrationBuilder.DropTable(
                name: "test_workspace_projects");

            migrationBuilder.DropTable(
                name: "test_tags");

            migrationBuilder.DropTable(
                name: "test_run_items");

            migrationBuilder.DropTable(
                name: "test_cases");

            migrationBuilder.DropTable(
                name: "test_runs");

            migrationBuilder.DropTable(
                name: "test_suites");

            migrationBuilder.DropTable(
                name: "test_plans");

            migrationBuilder.DropTable(
                name: "test_workspaces");
        }
    }
}
