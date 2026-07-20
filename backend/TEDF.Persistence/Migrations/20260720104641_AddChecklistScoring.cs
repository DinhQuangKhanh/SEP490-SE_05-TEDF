using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "ChecklistResultItems",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            // Backfill legacy result rows with a valid 10/7 snapshot so the CHECK constraints below pass.
            // Legacy rows keep their original IsPassed/PassedCount (source of truth); Score stays NULL.
            migrationBuilder.AddColumn<decimal>(
                name: "MaxScore",
                table: "ChecklistResultItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<decimal>(
                name: "PassScore",
                table: "ChecklistResultItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 7m);

            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "ChecklistResultItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            // Backfill legacy criteria (which had no scores) with the default 10/7 scale so existing
            // checklist configs stay valid under the CHECK constraints below.
            migrationBuilder.AddColumn<decimal>(
                name: "MaxScore",
                table: "ChecklistCriteria",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<decimal>(
                name: "PassScore",
                table: "ChecklistCriteria",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 7m);

            migrationBuilder.AddColumn<string>(
                name: "SourceFileName",
                table: "ChecklistConfigs",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChecklistResultItems_MaxScore",
                table: "ChecklistResultItems",
                sql: "[MaxScore] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChecklistResultItems_PassScore",
                table: "ChecklistResultItems",
                sql: "[PassScore] >= 0 AND [PassScore] <= [MaxScore]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChecklistResultItems_Score",
                table: "ChecklistResultItems",
                sql: "[Score] IS NULL OR ([Score] >= 0 AND [Score] <= [MaxScore])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChecklistCriteria_MaxScore",
                table: "ChecklistCriteria",
                sql: "[MaxScore] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChecklistCriteria_PassScore",
                table: "ChecklistCriteria",
                sql: "[PassScore] >= 0 AND [PassScore] <= [MaxScore]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ChecklistResultItems_MaxScore",
                table: "ChecklistResultItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChecklistResultItems_PassScore",
                table: "ChecklistResultItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChecklistResultItems_Score",
                table: "ChecklistResultItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChecklistCriteria_MaxScore",
                table: "ChecklistCriteria");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChecklistCriteria_PassScore",
                table: "ChecklistCriteria");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "ChecklistResultItems");

            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "ChecklistResultItems");

            migrationBuilder.DropColumn(
                name: "PassScore",
                table: "ChecklistResultItems");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "ChecklistResultItems");

            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "ChecklistCriteria");

            migrationBuilder.DropColumn(
                name: "PassScore",
                table: "ChecklistCriteria");

            migrationBuilder.DropColumn(
                name: "SourceFileName",
                table: "ChecklistConfigs");
        }
    }
}
