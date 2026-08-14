using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropChecklistScoringColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // defaultValue là 1m, không phải 0m như EF sinh mặc định: ngay bên dưới các CHECK
            // constraint "[MaxScore] > 0" và "[PassScore] <= [MaxScore]" được thêm lại, nên rollback
            // trên bảng đã có dữ liệu sẽ fail nếu cột được backfill bằng 0. 1/1 cũng đúng với giá trị
            // thực tế của toàn bộ dữ liệu tại thời điểm drop (checklist đã chuyển sang Pass/Fail).
            migrationBuilder.AddColumn<decimal>(
                name: "MaxScore",
                table: "ChecklistResultItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "PassScore",
                table: "ChecklistResultItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "ChecklistResultItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxScore",
                table: "ChecklistCriteria",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "PassScore",
                table: "ChecklistCriteria",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

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
    }
}
