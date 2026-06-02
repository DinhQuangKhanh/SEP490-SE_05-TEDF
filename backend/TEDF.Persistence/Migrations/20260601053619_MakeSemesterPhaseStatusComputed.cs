using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeSemesterPhaseStatusComputed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SemesterPhases_Status",
                table: "SemesterPhases");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "SemesterPhases",
                type: "int",
                nullable: false,
                computedColumnSql: "CASE WHEN SYSUTCDATETIME() < [StartDate] THEN 0 WHEN SYSUTCDATETIME() > [EndDate] THEN 2 ELSE 1 END",
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "SemesterPhases",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComputedColumnSql: "CASE WHEN SYSUTCDATETIME() < [StartDate] THEN 0 WHEN SYSUTCDATETIME() > [EndDate] THEN 2 ELSE 1 END");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterPhases_Status",
                table: "SemesterPhases",
                column: "Status");
        }
    }
}
