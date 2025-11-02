using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightReportEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnitWeightGrams",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExpectedWeightGrams",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "WeightReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalReportId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OrderItemId = table.Column<int>(type: "int", nullable: true),
                    ExpectedWeightGrams = table.Column<int>(type: "int", nullable: false),
                    ReportedWeightGrams = table.Column<int>(type: "int", nullable: false),
                    OverageGrams = table.Column<int>(type: "int", nullable: false),
                    OverageAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CourierNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentAttemptId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightReports_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeightReports_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeightReports_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeightReports_ApprovedByUserId",
                table: "WeightReports",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightReports_ExternalReportId",
                table: "WeightReports",
                column: "ExternalReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeightReports_OrderId",
                table: "WeightReports",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightReports_OrderItemId",
                table: "WeightReports",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightReports_Status",
                table: "WeightReports",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeightReports");

            migrationBuilder.DropColumn(
                name: "UnitWeightGrams",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ExpectedWeightGrams",
                table: "OrderItems");
        }
    }
}
