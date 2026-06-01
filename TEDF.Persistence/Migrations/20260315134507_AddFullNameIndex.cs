using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFullNameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_FullName",
                table: "Users",
                column: "FullName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_FullName",
                table: "Users");
        }
    }
}
