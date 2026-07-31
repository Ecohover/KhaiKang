using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhaiKang.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "system_roles",
                columns: new[] { "id", "name", "normalized_name" },
                values: new object[] { new Guid("94fbc708-1764-4d11-a8e6-df5e786cb678"), "User", "USER" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "system_roles",
                keyColumn: "id",
                keyValue: new Guid("94fbc708-1764-4d11-a8e6-df5e786cb678"));
        }
    }
}
