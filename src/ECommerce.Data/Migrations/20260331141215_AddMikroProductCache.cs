using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMikroProductCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MikroProductCache",
                columns: table => new
                {
                    StokKod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StokAd = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Barkod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GrupKod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Birim = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SatisFiyati = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FiyatListesiNo = table.Column<int>(type: "int", nullable: false),
                    DepoMiktari = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SatilabilirMiktar = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DepoNo = table.Column<int>(type: "int", nullable: false),
                    TumFiyatlarJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TumDepolarJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MikroGuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    LocalProductId = table.Column<int>(type: "int", nullable: true),
                    SyncStatus = table.Column<int>(type: "int", nullable: false),
                    DataHash = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MikroProductCache", x => x.StokKod);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MikroProductCache_Aktif",
                table: "MikroProductCache",
                column: "Aktif");

            migrationBuilder.CreateIndex(
                name: "IX_MikroProductCache_Aktif_GrupKod",
                table: "MikroProductCache",
                columns: new[] { "Aktif", "GrupKod" });

            migrationBuilder.CreateIndex(
                name: "IX_MikroProductCache_Barkod",
                table: "MikroProductCache",
                column: "Barkod");

            migrationBuilder.CreateIndex(
                name: "IX_MikroProductCache_GrupKod",
                table: "MikroProductCache",
                column: "GrupKod");

            migrationBuilder.CreateIndex(
                name: "IX_MikroProductCache_GuncellemeTarihi",
                table: "MikroProductCache",
                column: "GuncellemeTarihi");

            migrationBuilder.CreateIndex(
                name: "IX_MikroProductCache_LocalProductId",
                table: "MikroProductCache",
                column: "LocalProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MikroProductCache_StokAd",
                table: "MikroProductCache",
                column: "StokAd");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MikroProductCache");
        }
    }
}
