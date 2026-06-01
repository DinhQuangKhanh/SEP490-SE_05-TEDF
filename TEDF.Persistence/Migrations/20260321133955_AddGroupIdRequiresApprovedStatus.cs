using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupIdRequiresApprovedStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enforce: a project can only have a GroupId when its Status is
            // Approved (3), InProgress (5), or Completed (6).
            migrationBuilder.Sql(@"
                ALTER TABLE Projects ADD CONSTRAINT CK_Projects_GroupId_RequiresApprovedStatus
                CHECK (GroupId IS NULL OR Status IN (3, 5, 6));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE Projects DROP CONSTRAINT CK_Projects_GroupId_RequiresApprovedStatus;
            ");
        }
    }
}
