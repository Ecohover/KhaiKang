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
            migrationBuilder.CreateTable(
                name: "test_workspaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_test_workspaces", x => x.id);
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
                        name: "fk_test_workspace_members_workspace",
                        column: x => x.test_workspace_id,
                        principalTable: "test_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_test_suites_workspace_parent_sort_order",
                table: "test_suites",
                columns: new[] { "test_workspace_id", "parent_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_test_suites_parent_id",
                table: "test_suites",
                column: "parent_id");

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
                name: "uq_test_workspaces_name",
                table: "test_workspaces",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_suites");

            migrationBuilder.DropTable(
                name: "test_workspace_members");

            migrationBuilder.DropTable(
                name: "test_workspaces");
        }
    }
}
