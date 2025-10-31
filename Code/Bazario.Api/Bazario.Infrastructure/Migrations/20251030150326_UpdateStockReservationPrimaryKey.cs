using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bazario.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStockReservationPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StockReservations",
                table: "StockReservations");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "StockReservations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockReservations",
                table: "StockReservations",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_ReservationId",
                table: "StockReservations",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_ReservationId_Status",
                table: "StockReservations",
                columns: new[] { "ReservationId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StockReservations",
                table: "StockReservations");

            migrationBuilder.DropIndex(
                name: "IX_StockReservations_ReservationId",
                table: "StockReservations");

            migrationBuilder.DropIndex(
                name: "IX_StockReservations_ReservationId_Status",
                table: "StockReservations");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "StockReservations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockReservations",
                table: "StockReservations",
                column: "ReservationId");
        }
    }
}
