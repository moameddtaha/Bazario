using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bazario.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToDiscountReviewShippingConfigAndAlertPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StoreShippingConfigurations",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Reviews",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InventoryAlertPreferences",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Discounts",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StoreShippingConfigurations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InventoryAlertPreferences");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Discounts");
        }
    }
}
