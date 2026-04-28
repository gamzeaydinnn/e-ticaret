using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnagrupKodToMikroProductCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnagrupKod",
                table: "MikroProductCache",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SyncAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MikroProductCache_AnagrupKod",
                table: "MikroProductCache",
                column: "AnagrupKod");

            migrationBuilder.CreateIndex(
                name: "IX_SyncAuditLogs_CorrelationId",
                table: "SyncAuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncAuditLogs_CreatedAt",
                table: "SyncAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SyncAuditLogs_EventType",
                table: "SyncAuditLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_SyncAuditLogs_Severity",
                table: "SyncAuditLogs",
                column: "Severity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_MikroProductCache_AnagrupKod",
                table: "MikroProductCache");

            migrationBuilder.DropColumn(
                name: "AnagrupKod",
                table: "MikroProductCache");
        }
    }
}
