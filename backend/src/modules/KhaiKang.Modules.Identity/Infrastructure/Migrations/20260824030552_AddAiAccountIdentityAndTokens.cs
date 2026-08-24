using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiAccountIdentityAndTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "owner_account_id_snapshot",
                table: "audit_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "owner_display_name_snapshot",
                table: "audit_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_fixed_role_snapshot",
                table: "audit_events",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_name_snapshot",
                table: "audit_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ai_api_enabled",
                table: "accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "fixed_role",
                table: "accounts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_account_id",
                table: "accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_account_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ai_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protected_secret = table.Column<string>(type: "text", nullable: false),
                    lookup_verifier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    protection_version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_account_tokens", x => x.id);
                    table.CheckConstraint("ck_ai_account_tokens_revocation", "(revoked_at IS NULL AND revoked_by_account_id IS NULL) OR (revoked_at IS NOT NULL AND revoked_by_account_id IS NOT NULL)");
                    table.CheckConstraint("ck_ai_account_tokens_verifier", "length(lookup_verifier) = 64");
                    table.CheckConstraint("ck_ai_account_tokens_version", "version >= 1");
                    table.ForeignKey(
                        name: "fk_ai_account_tokens_account",
                        column: x => x.ai_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "code", "created_at", "created_by_account_id", "description", "name", "scope_type", "updated_at", "updated_by_account_id", "version" },
                values: new object[] { new Guid("af88a1cf-866f-457e-8502-72fe9ea222d1"), "ai_account.create", new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "建立固定角色的 AI 帳號。", "AI Account Create", "system", new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 });

            migrationBuilder.CreateIndex(
                name: "idx_accounts_owner_account_id",
                table: "accounts",
                column: "owner_account_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_accounts_ai_identity",
                table: "accounts",
                sql: "(account_type = 'human' AND owner_account_id IS NULL AND fixed_role IS NULL AND ai_api_enabled = FALSE) OR (account_type = 'ai_agent' AND owner_account_id IS NOT NULL AND fixed_role IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_accounts_fixed_role",
                table: "accounts",
                sql: "fixed_role IS NULL OR fixed_role IN ('pm_ai', 'development_ai', 'qa_ai', 'reviewer_ai', 'devops_ai')");

            migrationBuilder.CreateIndex(
                name: "uq_ai_account_tokens_account_id",
                table: "ai_account_tokens",
                column: "ai_account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_ai_account_tokens_lookup_verifier",
                table: "ai_account_tokens",
                column: "lookup_verifier",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_accounts_ai_owner",
                table: "accounts",
                column: "owner_account_id",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_accounts_ai_owner",
                table: "accounts");

            migrationBuilder.DropTable(
                name: "ai_account_tokens");

            migrationBuilder.DropIndex(
                name: "idx_accounts_owner_account_id",
                table: "accounts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_accounts_ai_identity",
                table: "accounts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_accounts_fixed_role",
                table: "accounts");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("af88a1cf-866f-457e-8502-72fe9ea222d1"));

            migrationBuilder.DropColumn(
                name: "owner_account_id_snapshot",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "owner_display_name_snapshot",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "target_fixed_role_snapshot",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "target_name_snapshot",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "ai_api_enabled",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "fixed_role",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "owner_account_id",
                table: "accounts");
        }
    }
}
