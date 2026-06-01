using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeleteDefenseMeeting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CouncilMembers");

            migrationBuilder.DropTable(
                name: "DefenseSchedules");

            migrationBuilder.DropTable(
                name: "MeetingSchedules");

            migrationBuilder.DropTable(
                name: "DefenseCouncils");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DefenseCouncils",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChairmanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SecretaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefenseCouncils", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefenseCouncils_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefenseCouncils_Users_ChairmanId",
                        column: x => x.ChairmanId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefenseCouncils_Users_SecretaryId",
                        column: x => x.SecretaryId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MentorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingSchedules_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingSchedules_Users_MentorId",
                        column: x => x.MentorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingSchedules_Users_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CouncilMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DefenseCouncilId = table.Column<int>(type: "int", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouncilMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CouncilMembers_DefenseCouncils_DefenseCouncilId",
                        column: x => x.DefenseCouncilId,
                        principalTable: "DefenseCouncils",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CouncilMembers_Users_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DefenseSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CouncilId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefenseSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefenseSchedules_DefenseCouncils_CouncilId",
                        column: x => x.CouncilId,
                        principalTable: "DefenseCouncils",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DefenseSchedules_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CouncilMembers_DefenseCouncilId",
                table: "CouncilMembers",
                column: "DefenseCouncilId");

            migrationBuilder.CreateIndex(
                name: "IX_CouncilMembers_MemberId",
                table: "CouncilMembers",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_CouncilMembers_Role",
                table: "CouncilMembers",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_DefenseCouncils_ChairmanId",
                table: "DefenseCouncils",
                column: "ChairmanId");

            migrationBuilder.CreateIndex(
                name: "IX_DefenseCouncils_SecretaryId",
                table: "DefenseCouncils",
                column: "SecretaryId");

            migrationBuilder.CreateIndex(
                name: "IX_DefenseCouncils_SemesterId",
                table: "DefenseCouncils",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_DefenseSchedules_CouncilId",
                table: "DefenseSchedules",
                column: "CouncilId");

            migrationBuilder.CreateIndex(
                name: "IX_DefenseSchedules_GroupId",
                table: "DefenseSchedules",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DefenseSchedules_ScheduledDate",
                table: "DefenseSchedules",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_DefenseSchedules_Status",
                table: "DefenseSchedules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSchedules_GroupId",
                table: "MeetingSchedules",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSchedules_MentorId",
                table: "MeetingSchedules",
                column: "MentorId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSchedules_RequestedBy",
                table: "MeetingSchedules",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSchedules_ScheduledDate",
                table: "MeetingSchedules",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSchedules_Status",
                table: "MeetingSchedules",
                column: "Status");
        }
    }
}
