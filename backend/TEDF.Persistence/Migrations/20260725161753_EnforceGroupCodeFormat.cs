using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <summary>
    /// Moves group identity to the {SemesterCode}-SE_NN / SE_NN scheme.
    ///
    /// Order matters: Groups.Code is materialised through GroupCodeConverter, which now validates
    /// the format. Any row left in an old format (G-2026-001, FA25-G-001, SU26-R-001) would throw
    /// the moment EF reads it, so every row is rewritten here — including soft-deleted ones, which
    /// still occupy a code under the unique index.
    /// </summary>
    public partial class EnforceGroupCodeFormat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Widen Code first — new codes are longer than the old 20-char limit.
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Groups",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            // 2. Add the nickname column while Name is still wide enough to be copied out of.
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Groups",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // 3. Preserve the student-chosen names before Name gets taken over by SE_NN.
            migrationBuilder.Sql(@"
                UPDATE Groups
                SET DisplayName = Name
                WHERE Name IS NOT NULL AND LTRIM(RTRIM(Name)) <> '';");

            // 4. Renumber per semester, ordered by creation, so the sequence is deterministic and
            //    unique within each semester. Padding matches C# {seq:D2}: 2 digits, more if needed.
            migrationBuilder.Sql(@"
                WITH numbered AS (
                    SELECT  g.Id,
                            s.Code AS SemesterCode,
                            ROW_NUMBER() OVER (PARTITION BY g.SemesterId ORDER BY g.CreatedAt, g.Id) AS Seq
                    FROM Groups g
                    INNER JOIN Semesters s ON s.Id = g.SemesterId
                )
                UPDATE g
                SET g.Name = 'SE_' + n.Padded,
                    g.Code = n.SemesterCode + '-SE_' + n.Padded
                FROM Groups g
                INNER JOIN (
                    SELECT Id,
                           SemesterCode,
                           CASE WHEN Seq < 100
                                THEN RIGHT('0' + CAST(Seq AS varchar(10)), 2)
                                ELSE CAST(Seq AS varchar(10))
                           END AS Padded
                    FROM numbered
                ) n ON n.Id = g.Id;");

            // 5. Only now is every Name a short SE_NN, so the column can be narrowed and made NOT NULL.
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Groups",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Widen Name back first so the preserved nicknames fit when copied in.
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Groups",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            // Restore the nicknames into Name; groups that never had one keep their SE_NN.
            migrationBuilder.Sql(@"
                UPDATE Groups
                SET Name = DisplayName
                WHERE DisplayName IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Groups");

            // Codes are left in the new format: the old scheme mixed several incompatible formats
            // and was not reconstructible. Narrowing Code back would truncate them, so it stays 30.
        }
    }
}
