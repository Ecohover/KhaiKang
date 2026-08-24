using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.ProjectManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueRelationIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "issue_relation_idempotency_records",
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
                    table.PrimaryKey("pk_issue_relation_idempotency_records", x => x.id);
                    table.CheckConstraint("ck_issue_relation_idempotency_fingerprint", "length(request_fingerprint) = 64");
                    table.CheckConstraint("ck_issue_relation_idempotency_terminal", "(state = 'incomplete_transient_failure' AND response_snapshot IS NULL AND rejection_snapshot IS NULL AND completed_at IS NULL) OR (state = 'terminal_success' AND response_snapshot IS NOT NULL AND rejection_snapshot IS NULL AND completed_at IS NOT NULL) OR (state = 'terminal_business_rejection' AND response_snapshot IS NULL AND rejection_snapshot IS NOT NULL AND completed_at IS NOT NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "uq_issue_relation_idempotency_actor_key",
                table: "issue_relation_idempotency_records",
                columns: new[] { "actor_account_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_relation_idempotency_records");
        }
    }
}
