using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductOrderLimitSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxOrderQuantity",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxOrderQuantity",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinOrderQuantity",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityStep",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ProductOrderLimitSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DefaultMaxQuantityPiece = table.Column<int>(type: "int", nullable: false),
                    DefaultMinQuantityPiece = table.Column<int>(type: "int", nullable: false),
                    DefaultQuantityStepPiece = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DefaultMaxWeightKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DefaultMinWeightKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DefaultWeightStepKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOrderLimitSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ProductOrderLimitSettings",
                columns: new[]
                {
                    "Id", "DefaultMaxQuantityPiece", "DefaultMinQuantityPiece", "DefaultQuantityStepPiece",
                    "DefaultMaxWeightKg", "DefaultMinWeightKg", "DefaultWeightStepKg",
                    "IsActive", "CreatedAt"
                },
                values: new object[]
                {
                    1, 5, 1, 1m, 10m, 0.25m, 0.25m, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductOrderLimitSettings");

            migrationBuilder.DropColumn(
                name: "MaxOrderQuantity",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "MaxOrderQuantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MinOrderQuantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "QuantityStep",
                table: "Products");
        }
    }
}
