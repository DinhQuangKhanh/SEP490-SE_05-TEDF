using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TEDF.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTicketAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupportTicketAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SupportTicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTicketAttachments_SupportTickets_SupportTicketId",
                        column: x => x.SupportTicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketMessageAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TicketMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketMessageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketMessageAttachments_TicketMessages_TicketMessageId",
                        column: x => x.TicketMessageId,
                        principalTable: "TicketMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketAttachments_SupportTicketId",
                table: "SupportTicketAttachments",
                column: "SupportTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMessageAttachments_TicketMessageId",
                table: "TicketMessageAttachments",
                column: "TicketMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportTicketAttachments");

            migrationBuilder.DropTable(
                name: "TicketMessageAttachments");
        }
    }
}
