using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJoinRequestExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "GroupJoinRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_GroupJoinRequests_GroupId_StudentId",
                table: "GroupJoinRequests",
                columns: new[] { "GroupId", "StudentId" },
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_GroupJoinRequests_Status_ExpiresAt",
                table: "GroupJoinRequests",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupInvitations_GroupId_InviteeId",
                table: "GroupInvitations",
                columns: new[] { "GroupId", "InviteeId" },
                unique: true,
                filter: "[Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GroupJoinRequests_GroupId_StudentId",
                table: "GroupJoinRequests");

            migrationBuilder.DropIndex(
                name: "IX_GroupJoinRequests_Status_ExpiresAt",
                table: "GroupJoinRequests");

            migrationBuilder.DropIndex(
                name: "IX_GroupInvitations_GroupId_InviteeId",
                table: "GroupInvitations");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "GroupJoinRequests");
        }
    }
}
