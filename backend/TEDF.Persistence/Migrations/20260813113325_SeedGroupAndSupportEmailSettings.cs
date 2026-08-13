using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedGroupAndSupportEmailSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SystemConfigurations",
                columns: new[] { "Id", "Category", "DataType", "Description", "Key", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { 15, "Notifications", 2, "Email students about group invitations and join-request outcomes", "EmailOnGroupMembership", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "true" },
                    { 16, "Notifications", 2, "Email participants about support ticket activity (new, replied, resolved)", "EmailOnSupportTicket", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "true" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 16);
        }
    }
}
