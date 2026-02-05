using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMikroSyncStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MikroCategoryMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MikroAnagrupKod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MikroAltgrupKod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MikroMarkaKod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    BrandId = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    MikroGrupAciklama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MikroCategoryMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MikroCategoryMappings_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MikroCategoryMappings_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MikroSyncStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SyncType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastSyncTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncCount = table.Column<int>(type: "int", nullable: false),
                    LastSyncDurationMs = table.Column<long>(type: "bigint", nullable: false),
                    LastSyncSuccess = table.Column<bool>(type: "bit", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MikroSyncStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MikroCategoryMappings_BrandId",
                table: "MikroCategoryMappings",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_MikroCategoryMappings_CategoryId",
                table: "MikroCategoryMappings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MikroCategoryMappings_Unique",
                table: "MikroCategoryMappings",
                columns: new[] { "MikroAnagrupKod", "MikroAltgrupKod", "MikroMarkaKod" },
                unique: true,
                filter: "[MikroAltgrupKod] IS NOT NULL AND [MikroMarkaKod] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MikroSyncStates_SyncType_Direction",
                table: "MikroSyncStates",
                columns: new[] { "SyncType", "Direction" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MikroCategoryMappings");

            migrationBuilder.DropTable(
                name: "MikroSyncStates");
        }
    }
}
