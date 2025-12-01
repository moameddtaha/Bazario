using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bazario.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToProductForOptimisticConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Products",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Products");
        }
    }
}
