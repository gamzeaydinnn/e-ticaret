using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Kampanya isimlerine unique constraint ekler.
    /// Sadece aktif kampanyalar için unique olacak (IsActive = 1).
    /// Bu sayede iki aynı isimde aktif kampanya oluşturulamaz,
    /// ancak pasif/silinmiş kampanyalarda aynı isim kullanılabilir.
/// </summary>
    public partial class AddUniqueCampaignNameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Unique index oluştur - sadece aktif kampanyalar için (IsActive = 1)
            // Filter sayesinde pasif/silinmiş kampanyalarda aynı isim kullanılabilir
            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_Name_Active",
                table: "Campaigns",
                column: "Name",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Index'i sil
            migrationBuilder.DropIndex(
                name: "IX_Campaigns_Name_Active",
                table: "Campaigns");
        }
    }
}
