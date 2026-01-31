using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeProductBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeProductBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BlockType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "manual"),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    BannerId = table.Column<int>(type: "int", nullable: true),
                    PosterImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BackgroundColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    MaxProductCount = table.Column<int>(type: "int", nullable: false, defaultValue: 6),
                    ViewAllUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ViewAllText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Tümünü Gör"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeProductBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeProductBlocks_Banners_BannerId",
                        column: x => x.BannerId,
                        principalTable: "Banners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HomeProductBlocks_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HomeBlockProducts",
                columns: table => new
                {
                    BlockId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeBlockProducts", x => new { x.BlockId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_HomeBlockProducts_HomeProductBlocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "HomeProductBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HomeBlockProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeBlockProducts_Block_Order",
                table: "HomeBlockProducts",
                columns: new[] { "BlockId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_HomeBlockProducts_ProductId",
                table: "HomeBlockProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeProductBlocks_Active_Order",
                table: "HomeProductBlocks",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_HomeProductBlocks_BannerId",
                table: "HomeProductBlocks",
                column: "BannerId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeProductBlocks_CategoryId",
                table: "HomeProductBlocks",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeProductBlocks_Slug",
                table: "HomeProductBlocks",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeBlockProducts");

            migrationBuilder.DropTable(
                name: "HomeProductBlocks");
        }
    }
}
