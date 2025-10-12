using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bazario.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Stores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Stores",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Stores");
        }
    }
}
