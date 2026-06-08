using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminSettingsAndArchiveSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "ProjectArchives",
                type: "bigint",
                nullable: true);

            migrationBuilder.InsertData(
                table: "SystemConfigurations",
                columns: new[] { "Id", "Category", "DataType", "Description", "Key", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { 6, "Registration", 1, "Maximum active topics a mentor may propose per pool", "MaxTopicsPerMentor", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "5" },
                    { 7, "Registration", 2, "Allow students to propose their own topics (direct registration)", "AllowDirectRegistration", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "true" },
                    { 8, "Registration", 2, "Require mentor approval of the outline before implementation", "RequireOutlineApproval", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "true" },
                    { 9, "Appearance", 0, "System primary theme color (hex)", "PrimaryColor", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "#2c6090" },
                    { 10, "Appearance", 0, "Header / brand display name", "HeaderName", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "TEDF" },
                    { 11, "Appearance", 0, "System logo URL", "LogoUrl", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "" },
                    { 12, "System", 2, "When enabled, only Admins can access the system", "MaintenanceMode", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "false" },
                    { 13, "Notifications", 2, "Email students when an evaluation result is finalized", "EmailOnEvaluationResult", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "true" },
                    { 14, "Notifications", 2, "Notify a mentor when a group registers for their topic", "NotifyMentorOnRegistration", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "true" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "ProjectArchives");
        }
    }
}
