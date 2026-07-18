using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMajorProgram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MajorProgramId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MajorPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MajorId = table.Column<int>(type: "int", nullable: false),
                    ProgramCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProgramDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MajorPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MajorPrograms_Majors_MajorId",
                        column: x => x.MajorId,
                        principalTable: "Majors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_MajorProgramId",
                table: "Users",
                column: "MajorProgramId");

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
                name: "FK_Users_MajorPrograms_MajorProgramId",
                table: "Users",
                column: "MajorProgramId",
                principalTable: "MajorPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_MajorPrograms_MajorProgramId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "MajorPrograms");

            migrationBuilder.DropIndex(
                name: "IX_Users_MajorProgramId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MajorProgramId",
                table: "Users");
        }
    }
}
