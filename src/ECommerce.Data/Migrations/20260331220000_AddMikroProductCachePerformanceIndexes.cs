using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <summary>
    /// MikroProductCache tablosu için performans optimizasyonu indexleri.
    /// 
    /// NEDEN: ERP Mikro sayfasında filtreleme ve sıralama sorgularını hızlandırmak için
    /// composite index'ler ekleniyor. Özellikle:
    /// - Aktif + GrupKod + StokAd kombinasyonu ile filtreleme
    /// - Aktif + SatisFiyati ile fiyat sıralaması
    /// - Aktif + DepoMiktari ile stok durumu filtreleme
    /// </summary>
    public partial class AddMikroProductCachePerformanceIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Aktif + GrupKod + StokAd üçlü composite index
            // Kullanım: WHERE Aktif = 1 AND GrupKod LIKE '%X%' ORDER BY StokAd
            migrationBuilder.CreateIndex(
                name: "IX_MikroProductCache_Aktif_GrupKod_StokAd",
                table: "MikroProductCache",
                columns: new[] { "Aktif", "GrupKod", "StokAd" });

            // Aktif + SatisFiyati composite index (fiyat sıralaması için)
            // Kullanım: WHERE Aktif = 1 ORDER BY SatisFiyati DESC
            migrationBuilder.CreateIndex(
                name: "IX_MikroProductCache_Aktif_SatisFiyati",
                table: "MikroProductCache",
                columns: new[] { "Aktif", "SatisFiyati" });

            // Aktif + DepoMiktari composite index (stoklu/stoksuz filtreleme için)
            // Kullanım: WHERE Aktif = 1 AND DepoMiktari > 0
            migrationBuilder.CreateIndex(
                name: "IX_MikroProductCache_Aktif_DepoMiktari",
                table: "MikroProductCache",
                columns: new[] { "Aktif", "DepoMiktari" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MikroProductCache_Aktif_GrupKod_StokAd",
                table: "MikroProductCache");

            migrationBuilder.DropIndex(
                name: "IX_MikroProductCache_Aktif_SatisFiyati",
                table: "MikroProductCache");

            migrationBuilder.DropIndex(
                name: "IX_MikroProductCache_Aktif_DepoMiktari",
                table: "MikroProductCache");
        }
    }
}
