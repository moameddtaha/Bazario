using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bazario.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameImageUrlToImageAndLogoUrlToLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LogoUrl",
                table: "Stores",
                newName: "Logo");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Products",
                newName: "Image");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Logo",
                table: "Stores",
                newName: "LogoUrl");

            migrationBuilder.RenameColumn(
                name: "Image",
                table: "Products",
                newName: "ImageUrl");
        }
    }
}
