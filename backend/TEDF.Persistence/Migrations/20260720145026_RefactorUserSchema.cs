using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUserSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MajorPrograms_Majors_MajorId",
                table: "MajorPrograms");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_MajorPrograms_MajorProgramId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Majors_MajorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeCode",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_MajorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_MajorProgramId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_StudentCode",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_RoleName",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId_RoleName",
                table: "UserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MajorPrograms",
                table: "MajorPrograms");

            migrationBuilder.DropIndex(
                name: "IX_MajorPrograms_IsActive",
                table: "MajorPrograms");

            migrationBuilder.DropIndex(
                name: "IX_MajorPrograms_MajorId",
                table: "MajorPrograms");

            migrationBuilder.DropIndex(
                name: "IX_MajorPrograms_ProgramCode",
                table: "MajorPrograms");

            migrationBuilder.DropColumn(
                name: "AcademicTitle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Division",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmployeeCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MajorId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MajorProgramId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StudentCode",
                table: "Users");

            // Clear existing role assignments before dropping RoleName — all rows would get RoleId=0
            // (the column default), producing duplicate (UserId, 0) entries that break the new unique index.
            // LoadTestDataSeeder repopulates UserRoles with correct RoleId values after migration.
            migrationBuilder.Sql("DELETE FROM UserRoles;");

            migrationBuilder.DropColumn(
                name: "RoleName",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MajorPrograms");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "MajorPrograms");

            migrationBuilder.DropColumn(
                name: "ProgramCode",
                table: "MajorPrograms");

            migrationBuilder.DropColumn(
                name: "ProgramDescription",
                table: "MajorPrograms");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MajorPrograms");

            migrationBuilder.RenameTable(
                name: "MajorPrograms",
                newName: "Programs");

            migrationBuilder.RenameColumn(
                name: "MajorId",
                table: "Programs",
                newName: "TotalCredit");

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "UserRoles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Programs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Programs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Programs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Programs",
                table: "Programs",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Combos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Abbr = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lecturers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AcademicTitle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lecturers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lecturers_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProgramId = table.Column<int>(type: "int", nullable: true),
                    ComboId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Students_Combos_ComboId",
                        column: x => x.ComboId,
                        principalTable: "Combos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Students_Programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "Programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Students_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Combos",
                columns: new[] { "Id", "Abbr", "Name" },
                values: new object[,]
                {
                    { 340, "JBE", "SE_COM5.2: Topic on Japanese Bridge Engineer_Chủ đề Kỹ sư cầu nối Nhật Bản (Định hướng Tiếng Nhật nâng cao cho kỹ sư CNTT) BIT_SE_K15A" },
                    { 402, "KOR", "SE_COM6: Topic on Information Technology - Korean Language_Chủ đề Công nghệ thông tin - tiếng Hàn BIT_SE_K15C" },
                    { 1469, "JFE", "SE_COM5.1.1:Topic on Japanese Bridge Engineer_Chủ đề Kỹ sư cầu nối Nhật Bản (Định hướng Tiếng Nhật CNTT: Lựa chọn JFE301 và 1 trong 2 học phần JIS401, JIT401 để triển khai ở kỳ 8) BIT_SE_K15C" },
                    { 2497, "React", "SE_COM4.1: Topic on React/NodeJS_Chủ đề React/NodeJS" },
                    { 2566, "AI", "SE_COM7.1:Topic on AI_Chủ đề AI" },
                    { 2605, "IC", "SE_COM11: Topic on IC design_Chủ đề Thiết kế vi mạch" },
                    { 2628, "Game", "SE_COM12: Topic on Game Development_Phát triển game" },
                    { 2640, "Java", "SE_COM10.2: Topic on Intensive Java_Chủ đề Java chuyên sâu_K19A" },
                    { 2675, "DS", "SE_COM14: Topic on Applied Data Science_Chủ đề Khoa học dữ liệu (KHDL) ứng dụng_K19B" },
                    { 2686, ".NET", "SE_COM3.3: Topic on .NET Programming_Chủ đề lập trình .NET BIT_SE_From_K18C" }
                });

            // MajorPrograms was renamed to Programs; delete old rows so the seed data below can insert cleanly.
            migrationBuilder.Sql("DELETE FROM Programs;");

            migrationBuilder.InsertData(
                table: "Programs",
                columns: new[] { "Id", "Code", "Description", "Name", "TotalCredit" },
                values: new object[,]
                {
                    { 1, "BIT_SE_K20B", "Đào tạo cử nhân ngành CNTT chuyên ngành Kỹ thuật phần mềm (KTPM) có nhân cách và năng lực đáp ứng nhu cầu thực tế của xã hội; nắm vững kiến thức chuyên môn và thực hành, có khả năng tổ chức, thiết kế và phát triển các hệ thống phần mềm.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 2, "BIT_SE_K20C", "Đào tạo cử nhân ngành CNTT chuyên ngành Kỹ thuật phần mềm (KTPM) có nhân cách và năng lực đáp ứng nhu cầu thực tế của xã hội; nắm vững kiến thức chuyên môn và thực hành, có khả năng tổ chức, thiết kế và phát triển các hệ thống phần mềm.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 3, "BIT_SE_K20D_K21A", "Đào tạo cử nhân ngành CNTT chuyên ngành Kỹ thuật phần mềm (KTPM) có nhân cách và năng lực đáp ứng nhu cầu thực tế của xã hội; nắm vững kiến thức chuyên môn và thực hành, có khả năng tổ chức, thiết kế và phát triển các hệ thống phần mềm.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 4, "BIT_SE_K21B", "Đào tạo cử nhân ngành CNTT chuyên ngành Kỹ thuật phần mềm (KTPM) có nhân cách và năng lực đáp ứng nhu cầu thực tế của xã hội; nắm vững kiến thức chuyên môn và thực hành, có khả năng tổ chức, thiết kế và phát triển các hệ thống phần mềm.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 5, "BIT_SE_K21C", "Đào tạo cử nhân ngành CNTT chuyên ngành Kỹ thuật phần mềm (KTPM) có nhân cách và năng lực đáp ứng nhu cầu thực tế của xã hội; nắm vững kiến thức chuyên môn và thực hành, có khả năng tổ chức, thiết kế và phát triển các hệ thống phần mềm.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 6, "BIT_SE_K19D_K20A", "Đào tạo cử nhân ngành CNTT chuyên ngành Kỹ thuật phần mềm (KTPM) có nhân cách và năng lực đáp ứng nhu cầu thực tế của xã hội; nắm vững kiến thức chuyên môn và thực hành, có khả năng tổ chức, thiết kế và phát triển các hệ thống phần mềm.", "The Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 7, "BIT_SE_K19B", "Đào tạo cử nhân ngành CNTT chuyên ngành Kỹ thuật phần mềm (KTPM) có nhân cách và năng lực đáp ứng nhu cầu thực tế của xã hội; nắm vững kiến thức chuyên môn và thực hành, có khả năng tổ chức, thiết kế và phát triển các hệ thống phần mềm.", "The Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 8, "BIT_SE_K19C", "Đào tạo cử nhân ngành CNTT chuyên ngành Kỹ thuật phần mềm (KTPM) có nhân cách và năng lực đáp ứng nhu cầu thực tế của xã hội; nắm vững kiến thức chuyên môn và thực hành, có khả năng tổ chức, thiết kế và phát triển các hệ thống phần mềm.", "The Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 9, "BIT_SE_K18D_19A", "Đào tạo cử nhân ngành CNTT chuyên ngành Kỹ thuật phần mềm (KTPM) có nhân cách và năng lực đáp ứng nhu cầu thực tế của xã hội; nắm vững kiến thức chuyên môn và thực hành, có khả năng tổ chức, thiết kế và phát triển các hệ thống phần mềm.", "The Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 10, "BIT_SE_K18C", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 11, "BIT_SE_K18B", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 12, "BIT_SE_K17D_18A", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 13, "BIT_SE_K17C", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 14, "BIT_SE_K17B", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 15, "BIT_SE_K16D_K17A", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 16, "BIT_SE_K16C", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 17, "BIT_SE_K16B", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 18, "BIT_SE_K16D,K17A", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 19, "BIT_SE_K15C", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 20, "BIT_SE_K15D,K16A", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 21, "BIT_SE_K17D,K18A", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 22, "BIT_SE_K15A", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 23, "BIT_SE_K15B", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 },
                    { 24, "BIT_SE_tuK16B", "Training Information Technology/Software Engineering specialty graduates with personality and capacity to meet the needs of society, mastering professional knowledge and practice, capable of organizing, designing and developing software systems.", "Bachelor Program of Information Technology, Software Engineering Major (Chương trình cử nhân ngành CNTT, chuyên ngành Kỹ thuật phần mềm)", 145 }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Mentor" },
                    { 3, "Student" },
                    { 4, "Evaluator" },
                    { 5, "DepartmentHead" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Programs_Code",
                table: "Programs",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lecturers_EmployeeCode",
                table: "Lecturers",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_ComboId",
                table: "Students",
                column: "ComboId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_ProgramId",
                table: "Students",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentCode",
                table: "Students",
                column: "StudentCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropTable(
                name: "Lecturers");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Combos");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Programs",
                table: "Programs");

            migrationBuilder.DropIndex(
                name: "IX_Programs_Code",
                table: "Programs");

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Programs",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Programs");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Programs");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Programs");

            migrationBuilder.RenameTable(
                name: "Programs",
                newName: "MajorPrograms");

            migrationBuilder.RenameColumn(
                name: "TotalCredit",
                table: "MajorPrograms",
                newName: "MajorId");

            migrationBuilder.AddColumn<string>(
                name: "AcademicTitle",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Division",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeCode",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MajorId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MajorProgramId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentCode",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleName",
                table: "UserRoles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MajorPrograms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "MajorPrograms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProgramCode",
                table: "MajorPrograms",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProgramDescription",
                table: "MajorPrograms",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MajorPrograms",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MajorPrograms",
                table: "MajorPrograms",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeCode",
                table: "Users",
                column: "EmployeeCode",
                unique: true,
                filter: "[EmployeeCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MajorId",
                table: "Users",
                column: "MajorId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MajorProgramId",
                table: "Users",
                column: "MajorProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StudentCode",
                table: "Users",
                column: "StudentCode",
                unique: true,
                filter: "[StudentCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleName",
                table: "UserRoles",
                column: "RoleName");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleName",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MajorPrograms_IsActive",
                table: "MajorPrograms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MajorPrograms_MajorId",
                table: "MajorPrograms",
                column: "MajorId");

            migrationBuilder.CreateIndex(
                name: "IX_MajorPrograms_ProgramCode",
                table: "MajorPrograms",
                column: "ProgramCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MajorPrograms_Majors_MajorId",
                table: "MajorPrograms",
                column: "MajorId",
                principalTable: "Majors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_MajorPrograms_MajorProgramId",
                table: "Users",
                column: "MajorProgramId",
                principalTable: "MajorPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Majors_MajorId",
                table: "Users",
                column: "MajorId",
                principalTable: "Majors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
