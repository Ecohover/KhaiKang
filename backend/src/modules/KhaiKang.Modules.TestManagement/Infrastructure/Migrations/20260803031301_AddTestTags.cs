using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.TestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTestTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "idx_test_tags_status",
                table: "test_tags",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_test_tags_name",
                table: "test_tags",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_case_tags");

            migrationBuilder.DropTable(
                name: "test_tags");
        }
    }
}
