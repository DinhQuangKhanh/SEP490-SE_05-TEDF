using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhaseIdToEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PhaseId",
                table: "ProjectEvaluatorAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PhaseId",
                table: "EvaluationSubmissions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhaseId",
                table: "ProjectEvaluatorAssignments");

            migrationBuilder.DropColumn(
                name: "PhaseId",
                table: "EvaluationSubmissions");
        }
    }
}
