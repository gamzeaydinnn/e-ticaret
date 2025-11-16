using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLogDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PerformedByUserId",
                table: "InventoryLogs");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "InventoryLogs",
                newName: "ReferenceId");

            migrationBuilder.RenameColumn(
                name: "ChangeType",
                table: "InventoryLogs",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "ChangeQuantity",
                table: "InventoryLogs",
                newName: "OldStock");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "InventoryLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NewStock",
                table: "InventoryLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Action",
                table: "InventoryLogs");

            migrationBuilder.DropColumn(
                name: "NewStock",
                table: "InventoryLogs");

            migrationBuilder.RenameColumn(
                name: "ReferenceId",
                table: "InventoryLogs",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "InventoryLogs",
                newName: "ChangeType");

            migrationBuilder.RenameColumn(
                name: "OldStock",
                table: "InventoryLogs",
                newName: "ChangeQuantity");

            migrationBuilder.AddColumn<int>(
                name: "PerformedByUserId",
                table: "InventoryLogs",
                type: "int",
                nullable: true);
        }
    }
}
