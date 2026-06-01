using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class UpdateProjectsGroupIdConstraintForDirectRegistration : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1
                    FROM sys.check_constraints
                    WHERE name = 'CK_Projects_GroupId_RequiresApprovedStatus'
                )
                BEGIN
                    ALTER TABLE Projects DROP CONSTRAINT CK_Projects_GroupId_RequiresApprovedStatus;
                END
            ");

      // Allow GroupId for direct-registration projects in any status.
      // For non-direct-registration projects, keep the original status restriction.
      migrationBuilder.Sql(@"
                ALTER TABLE Projects ADD CONSTRAINT CK_Projects_GroupId_RequiresApprovedStatus
                CHECK (
                    GroupId IS NULL
                    OR SourceType = 1
                    OR Status IN (3, 5, 6)
                );
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1
                    FROM sys.check_constraints
                    WHERE name = 'CK_Projects_GroupId_RequiresApprovedStatus'
                )
                BEGIN
                    ALTER TABLE Projects DROP CONSTRAINT CK_Projects_GroupId_RequiresApprovedStatus;
                END
            ");

      migrationBuilder.Sql(@"
                ALTER TABLE Projects ADD CONSTRAINT CK_Projects_GroupId_RequiresApprovedStatus
                CHECK (GroupId IS NULL OR Status IN (3, 5, 6));
            ");
    }
  }
}
