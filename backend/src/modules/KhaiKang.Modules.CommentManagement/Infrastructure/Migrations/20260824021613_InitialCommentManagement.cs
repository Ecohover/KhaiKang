using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.CommentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "comment_idempotency_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    response_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    rejection_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comment_idempotency_records", x => x.id);
                    table.CheckConstraint("ck_comment_idempotency_fingerprint", "length(request_fingerprint) = 64");
                    table.CheckConstraint("ck_comment_idempotency_state", "state IN ('incomplete_transient_failure', 'terminal_success', 'terminal_business_rejection')");
                    table.CheckConstraint("ck_comment_idempotency_terminal_snapshot", "(state = 'incomplete_transient_failure' AND response_snapshot IS NULL AND rejection_snapshot IS NULL AND target_id IS NULL AND completed_at IS NULL) OR (state = 'terminal_success' AND response_snapshot IS NOT NULL AND rejection_snapshot IS NULL AND target_id IS NOT NULL AND completed_at IS NOT NULL) OR (state = 'terminal_business_rejection' AND response_snapshot IS NULL AND rejection_snapshot IS NOT NULL AND target_id IS NULL AND completed_at IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    author_account_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    author_fixed_role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    author_owner_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_owner_display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
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
                    table.PrimaryKey("pk_comments", x => x.id);
                    table.CheckConstraint("ck_comments_author_account_type", "author_account_type IN ('human', 'ai_agent')");
                    table.CheckConstraint("ck_comments_resource_type", "resource_type IN ('issue', 'test_case', 'test_plan', 'test_run')");
                    table.CheckConstraint("ck_comments_version", "version >= 1");
                });

            migrationBuilder.CreateIndex(
                name: "uq_comment_idempotency_actor_operation_key",
                table: "comment_idempotency_records",
                columns: new[] { "actor_account_id", "operation_code", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_comments_author",
                table: "comments",
                column: "author_account_id");

            migrationBuilder.CreateIndex(
                name: "idx_comments_resource_created",
                table: "comments",
                columns: new[] { "resource_type", "resource_id", "created_at", "id" },
                descending: new[] { false, false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comment_idempotency_records");

            migrationBuilder.DropTable(
                name: "comments");
        }
    }
}
