using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bazario.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAlertPreferencesPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryAlertPreferences",
                columns: table => new
                {
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    EnableLowStockAlerts = table.Column<bool>(type: "bit", nullable: false),
                    EnableOutOfStockAlerts = table.Column<bool>(type: "bit", nullable: false),
                    EnableRestockRecommendations = table.Column<bool>(type: "bit", nullable: false),
                    EnableDeadStockAlerts = table.Column<bool>(type: "bit", nullable: false),
                    DefaultLowStockThreshold = table.Column<int>(type: "int", nullable: false),
                    DeadStockDays = table.Column<int>(type: "int", nullable: false),
                    SendDailySummary = table.Column<bool>(type: "bit", nullable: false),
                    SendWeeklySummary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAlertPreferences", x => x.StoreId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlertPreferences_AlertEmail",
                table: "InventoryAlertPreferences",
                column: "AlertEmail");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlertPreferences_CreatedAt",
                table: "InventoryAlertPreferences",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlertPreferences_UpdatedAt",
                table: "InventoryAlertPreferences",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryAlertPreferences");
        }
    }
}
