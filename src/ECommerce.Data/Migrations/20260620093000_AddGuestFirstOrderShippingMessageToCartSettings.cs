using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <summary>
    /// Misafir kullanıcı sepet promosyon mesajını admin panelinden yönetebilmek için
    /// CartSettings tablosuna opsiyonel metin alanı ekler.
    /// </summary>
    public partial class AddGuestFirstOrderShippingMessageToCartSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuestFirstOrderShippingMessage",
                table: "CartSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Hesap oluştur, ilk alışverişinde kargo bedava!");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestFirstOrderShippingMessage",
                table: "CartSettings");
        }
    }
}
