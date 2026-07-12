using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIsInventoryCommitted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInventoryCommitted",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Eski siparişler checkout'ta commit ediliyordu.
            // New(-1), Pending(0), PaymentFailed(13) hariç hepsi committed kabul edilir.
            migrationBuilder.Sql(@"
UPDATE Orders
SET IsInventoryCommitted = 1
WHERE Status NOT IN (-1, 0, 13);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInventoryCommitted",
                table: "Orders");
        }
    }
}
