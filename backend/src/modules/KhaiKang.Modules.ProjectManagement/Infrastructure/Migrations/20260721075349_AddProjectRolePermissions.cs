using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KhaiKang.Modules.ProjectManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_role_permissions");
        }
    }
}
