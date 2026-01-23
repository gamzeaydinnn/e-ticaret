using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightBasedPaymentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWeightBased",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxOrderWeight",
                table: "Products",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinOrderWeight",
                table: "Products",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerUnit",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightTolerancePercent",
                table: "Products",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WeightUnit",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AllItemsWeighed",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DifferenceSettled",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DifferenceSettledAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "HasWeightBasedItems",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PosnetTransactionId",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreAuthAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreAuthDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPriceDifference",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalWeightDifference",
                table: "Orders",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "WeighingCompletedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeightAdjustmentStatus",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualPrice",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualWeight",
                table: "OrderItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedPrice",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedWeight",
                table: "OrderItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsWeighed",
                table: "OrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWeightBased",
                table: "OrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceDifference",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerUnit",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "WeighedAt",
                table: "OrderItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeighedByCourierId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightDifference",
                table: "OrderItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeightUnit",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "WeightAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WeightUnit = table.Column<int>(type: "int", nullable: false),
                    EstimatedWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ActualWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WeightDifference = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DifferencePercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PricePerUnit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceDifference = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WeighedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WeighedByCourierId = table.Column<int>(type: "int", nullable: true),
                    WeighedByCourierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsSettled = table.Column<bool>(type: "bit", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentTransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequiresAdminApproval = table.Column<bool>(type: "bit", nullable: false),
                    AdminReviewed = table.Column<bool>(type: "bit", nullable: false),
                    AdminApproved = table.Column<bool>(type: "bit", nullable: true),
                    AdminAdjustedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AdminReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminUserId = table.Column<int>(type: "int", nullable: true),
                    AdminUserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomerNotified = table.Column<bool>(type: "bit", nullable: false),
                    CustomerNotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotificationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightAdjustments_Couriers_WeighedByCourierId",
                        column: x => x.WeighedByCourierId,
                        principalTable: "Couriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WeightAdjustments_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WeightAdjustments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WeightAdjustments_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WeightAdjustments_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeightAdjustments_AdminUserId",
                table: "WeightAdjustments",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightAdjustments_CreatedAt",
                table: "WeightAdjustments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WeightAdjustments_OrderId",
                table: "WeightAdjustments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightAdjustments_OrderItemId",
                table: "WeightAdjustments",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightAdjustments_ProductId",
                table: "WeightAdjustments",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightAdjustments_Status",
                table: "WeightAdjustments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WeightAdjustments_StatusAdmin",
                table: "WeightAdjustments",
                columns: new[] { "Status", "RequiresAdminApproval" });

            migrationBuilder.CreateIndex(
                name: "IX_WeightAdjustments_WeighedByCourierId",
                table: "WeightAdjustments",
                column: "WeighedByCourierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeightAdjustments");

            migrationBuilder.DropColumn(
                name: "IsWeightBased",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxOrderWeight",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MinOrderWeight",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PricePerUnit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WeightTolerancePercent",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WeightUnit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AllItemsWeighed",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DifferenceSettled",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DifferenceSettledAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FinalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HasWeightBasedItems",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PosnetTransactionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PreAuthAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PreAuthDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TotalPriceDifference",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TotalWeightDifference",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "WeighingCompletedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "WeightAdjustmentStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ActualPrice",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ActualWeight",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "EstimatedPrice",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "EstimatedWeight",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IsWeighed",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IsWeightBased",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PriceDifference",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PricePerUnit",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "WeighedAt",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "WeighedByCourierId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "WeightDifference",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "WeightUnit",
                table: "OrderItems");
        }
    }
}
