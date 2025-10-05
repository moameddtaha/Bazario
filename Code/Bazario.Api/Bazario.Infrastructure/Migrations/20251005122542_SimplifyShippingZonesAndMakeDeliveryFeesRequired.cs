using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bazario.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyShippingZonesAndMakeDeliveryFeesRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreShippingRates");

            migrationBuilder.CreateTable(
                name: "StoreShippingConfigurations",
                columns: table => new
                {
                    ConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultShippingZone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OffersSameDayDelivery = table.Column<bool>(type: "bit", nullable: false),
                    OffersStandardDelivery = table.Column<bool>(type: "bit", nullable: false),
                    SameDayCutoffHour = table.Column<int>(type: "int", nullable: true),
                    ShippingNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SameDayDeliveryFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StandardDeliveryFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NationalDeliveryFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SupportedCities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExcludedCities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SupportedCitiesList = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExcludedCitiesList = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreShippingConfigurations", x => x.ConfigurationId);
                    table.ForeignKey(
                        name: "FK_StoreShippingConfigurations_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreShippingConfigurations_ConfigurationId",
                table: "StoreShippingConfigurations",
                column: "ConfigurationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreShippingConfigurations_StoreId",
                table: "StoreShippingConfigurations",
                column: "StoreId",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreShippingConfigurations");

            migrationBuilder.CreateTable(
                name: "StoreShippingRates",
                columns: table => new
                {
                    StoreShippingRateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FreeShippingThreshold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ShippingCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingZone = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreShippingRates", x => x.StoreShippingRateId);
                    table.ForeignKey(
                        name: "FK_StoreShippingRates_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreShippingRates_ShippingZone",
                table: "StoreShippingRates",
                column: "ShippingZone");

            migrationBuilder.CreateIndex(
                name: "IX_StoreShippingRates_StoreId",
                table: "StoreShippingRates",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreShippingRates_StoreId_ShippingZone",
                table: "StoreShippingRates",
                columns: new[] { "StoreId", "ShippingZone" },
                unique: true);
        }
    }
}
