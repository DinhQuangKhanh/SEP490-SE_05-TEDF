using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DeleteData(
                table: "Majors",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Majors",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Majors",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Majors",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Majors",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Format = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Parameters = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SemesterId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Reports_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Reports_Users_GeneratedBy",
                        column: x => x.GeneratedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "HeadOfDepartmentId", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "CNTT", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Khoa Công nghệ thông tin", null, true, "Công nghệ thông tin", null },
                    { 2, "KT", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Khoa Kinh tế", null, true, "Kinh tế", null },
                    { 3, "NNA", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Khoa Ngôn ngữ Anh", null, true, "Ngôn ngữ Anh", null }
                });

            migrationBuilder.InsertData(
                table: "Majors",
                columns: new[] { "Id", "Code", "CreatedAt", "DepartmentId", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "SE", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Chuyên ngành Kỹ thuật phần mềm", true, "Kỹ thuật phần mềm", null },
                    { 2, "IA", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Chuyên ngành An toàn thông tin", true, "An toàn thông tin", null },
                    { 3, "AI", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Chuyên ngành Trí tuệ nhân tạo", true, "Trí tuệ nhân tạo", null },
                    { 4, "BA", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Chuyên ngành Quản trị kinh doanh", true, "Quản trị kinh doanh", null },
                    { 5, "ENG", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, "Chuyên ngành Ngôn ngữ Anh", true, "Ngôn ngữ Anh", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_DepartmentId",
                table: "Reports",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_GeneratedAt",
                table: "Reports",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_GeneratedBy",
                table: "Reports",
                column: "GeneratedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_SemesterId",
                table: "Reports",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Type",
                table: "Reports",
                column: "Type");
        }
    }
}
