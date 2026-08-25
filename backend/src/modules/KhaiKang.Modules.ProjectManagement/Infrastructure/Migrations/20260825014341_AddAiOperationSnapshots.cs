using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.ProjectManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiOperationSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "actor_fixed_role_snapshot",
                table: "project_audit_events",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "actor_name_snapshot",
                table: "project_audit_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_account_id_snapshot",
                table: "project_audit_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "owner_display_name_snapshot",
                table: "project_audit_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "actor_fixed_role_snapshot",
                table: "project_audit_events");

            migrationBuilder.DropColumn(
                name: "actor_name_snapshot",
                table: "project_audit_events");

            migrationBuilder.DropColumn(
                name: "owner_account_id_snapshot",
                table: "project_audit_events");

            migrationBuilder.DropColumn(
                name: "owner_display_name_snapshot",
                table: "project_audit_events");
        }
    }
}
