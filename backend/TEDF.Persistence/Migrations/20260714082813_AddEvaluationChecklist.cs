using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationChecklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChecklistConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PassThreshold = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistConfigs_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChecklistConfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    TitleVi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistCriteria_ChecklistConfigs_ChecklistConfigId",
                        column: x => x.ChecklistConfigId,
                        principalTable: "ChecklistConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectEvaluationChecklists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    ChecklistConfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionNumber = table.Column<int>(type: "int", nullable: false),
                    RequiredPassCount = table.Column<int>(type: "int", nullable: false),
                    PassedCount = table.Column<int>(type: "int", nullable: false),
                    EvaluatorNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectEvaluationChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectEvaluationChecklists_ChecklistConfigs_ChecklistConfigId",
                        column: x => x.ChecklistConfigId,
                        principalTable: "ChecklistConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectEvaluationChecklists_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectEvaluationChecklists_Users_EvaluatorId",
                        column: x => x.EvaluatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistResultItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectEvaluationChecklistId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    TitleVi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistResultItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistResultItems_ProjectEvaluationChecklists_ProjectEvaluationChecklistId",
                        column: x => x.ProjectEvaluationChecklistId,
                        principalTable: "ProjectEvaluationChecklists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistConfigs_Active_Semester",
                table: "ChecklistConfigs",
                column: "SemesterId",
                unique: true,
                filter: "[Status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistConfigs_SemesterId_Version",
                table: "ChecklistConfigs",
                columns: new[] { "SemesterId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistCriteria_ChecklistConfigId",
                table: "ChecklistCriteria",
                column: "ChecklistConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistResultItems_ProjectEvaluationChecklistId",
                table: "ChecklistResultItems",
                column: "ProjectEvaluationChecklistId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEvaluationChecklists_ChecklistConfigId",
                table: "ProjectEvaluationChecklists",
                column: "ChecklistConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEvaluationChecklists_EvaluatorId",
                table: "ProjectEvaluationChecklists",
                column: "EvaluatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEvaluationChecklists_ProjectId_EvaluatorId_SubmissionNumber",
                table: "ProjectEvaluationChecklists",
                columns: new[] { "ProjectId", "EvaluatorId", "SubmissionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChecklistCriteria");

            migrationBuilder.DropTable(
                name: "ChecklistResultItems");

            migrationBuilder.DropTable(
                name: "ProjectEvaluationChecklists");

            migrationBuilder.DropTable(
                name: "ChecklistConfigs");
        }
    }
}
