using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDivisionAndMajor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Division",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MajorId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_MajorId",
                table: "Users",
                column: "MajorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Majors_MajorId",
                table: "Users",
                column: "MajorId",
                principalTable: "Majors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Majors_MajorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_MajorId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Division",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MajorId",
                table: "Users");
        }
    }
}
