using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCartSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CartSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinimumCartAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsMinimumCartAmountActive = table.Column<bool>(type: "bit", nullable: false),
                    MinimumCartAmountMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CartSettings",
                columns: new[] { "Id", "CreatedAt", "IsActive", "IsMinimumCartAmountActive", "MinimumCartAmount", "MinimumCartAmountMessage", "UpdatedAt", "UpdatedByUserId", "UpdatedByUserName" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 0m, "Sipariş verebilmek için sepet tutarınız en az {amount} TL olmalıdır.", null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartSettings");
        }
    }
}
