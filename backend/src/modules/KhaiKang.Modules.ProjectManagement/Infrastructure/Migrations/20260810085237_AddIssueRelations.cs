using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KhaiKang.Modules.ProjectManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "issue_relation_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    forward_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reverse_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    direction_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("pk_issue_relation_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issue_relations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relation_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_issue_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_issue_relations", x => x.id);
                    table.CheckConstraint("ck_issue_relations_deleted_metadata", "(is_deleted = false AND deleted_at IS NULL AND deleted_by_account_id IS NULL) OR (is_deleted = true AND deleted_at IS NOT NULL AND deleted_by_account_id IS NOT NULL)");
                    table.CheckConstraint("ck_issue_relations_distinct_issues", "source_issue_id <> target_issue_id");
                    table.ForeignKey(
                        name: "fk_issue_relations_created_by_account",
                        column: x => x.created_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issue_relations_deleted_by_account",
                        column: x => x.deleted_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issue_relations_project",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_issue_relations_source_issue",
                        column: x => x.source_issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issue_relations_target_issue",
                        column: x => x.target_issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issue_relations_type",
                        column: x => x.relation_type_id,
                        principalTable: "issue_relation_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issue_relations_updated_by_account",
                        column: x => x.updated_by_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "issue_relation_types",
                columns: new[] { "id", "code", "created_at", "created_by_account_id", "direction_kind", "forward_label", "is_active", "is_system", "reverse_label", "sort_order", "updated_at", "updated_by_account_id", "version" },
                values: new object[,]
                {
                    { new Guid("04fd64b0-e17c-41a4-9907-d36dc630b377"), "blocks", new DateTimeOffset(new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "directed", "Blocks", true, true, "Blocked by", 3, new DateTimeOffset(new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("2f01d4ad-70e3-4c7f-9b7c-27f5d32001a1"), "related", new DateTimeOffset(new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "symmetric", "Relates to", true, true, "Relates to", 1, new DateTimeOffset(new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("5be46e72-6c93-4766-8599-31e90ed45248"), "duplicates", new DateTimeOffset(new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "directed", "Duplicates", true, true, "Duplicated by", 4, new DateTimeOffset(new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("9fe7e36b-a461-4bfa-8ba8-3e5d201184c2"), "parent_of", new DateTimeOffset(new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "hierarchical", "Parent of", true, true, "Child of", 2, new DateTimeOffset(new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 },
                    { new Guid("c8d1fd1f-3528-4b20-bb6e-febda1adcb71"), "tests", new DateTimeOffset(new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "directed", "Tests / verifies", true, true, "Tested / verified by", 5, new DateTimeOffset(new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "uq_issue_relation_types_code",
                table: "issue_relation_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_issue_relations_project",
                table: "issue_relations",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "idx_issue_relations_source_active",
                table: "issue_relations",
                columns: new[] { "source_issue_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "idx_issue_relations_target_active",
                table: "issue_relations",
                columns: new[] { "target_issue_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_issue_relations_created_by_account_id",
                table: "issue_relations",
                column: "created_by_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_issue_relations_deleted_by_account_id",
                table: "issue_relations",
                column: "deleted_by_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_issue_relations_updated_by_account_id",
                table: "issue_relations",
                column: "updated_by_account_id");

            migrationBuilder.CreateIndex(
                name: "uq_issue_relations_active",
                table: "issue_relations",
                columns: new[] { "relation_type_id", "source_issue_id", "target_issue_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "uq_issue_relations_active_parent",
                table: "issue_relations",
                column: "target_issue_id",
                unique: true,
                filter: "is_deleted = false AND relation_type_id = '9fe7e36b-a461-4bfa-8ba8-3e5d201184c2'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_relations");

            migrationBuilder.DropTable(
                name: "issue_relation_types");
        }
    }
}
