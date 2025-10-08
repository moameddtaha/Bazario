using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bazario.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationBasedShippingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcludedCities",
                table: "StoreShippingConfigurations");

            migrationBuilder.DropColumn(
                name: "ExcludedCitiesList",
                table: "StoreShippingConfigurations");

            migrationBuilder.DropColumn(
                name: "SupportedCities",
                table: "StoreShippingConfigurations");

            migrationBuilder.DropColumn(
                name: "SupportedCitiesList",
                table: "StoreShippingConfigurations");

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId1",
                table: "StoreShippingConfigurations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NameArabic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SupportsPostalCodes = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.CountryId);
                });

            migrationBuilder.CreateTable(
                name: "Governorates",
                columns: table => new
                {
                    GovernorateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameArabic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SupportsSameDayDelivery = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Governorates", x => x.GovernorateId);
                    table.ForeignKey(
                        name: "FK_Governorates_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "CountryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GovernorateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameArabic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SupportsSameDayDelivery = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.CityId);
                    table.ForeignKey(
                        name: "FK_Cities_Governorates_GovernorateId",
                        column: x => x.GovernorateId,
                        principalTable: "Governorates",
                        principalColumn: "GovernorateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoreGovernorateSupports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GovernorateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsSupported = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StoreId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreGovernorateSupports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreGovernorateSupports_Governorates_GovernorateId",
                        column: x => x.GovernorateId,
                        principalTable: "Governorates",
                        principalColumn: "GovernorateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreGovernorateSupports_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreGovernorateSupports_Stores_StoreId1",
                        column: x => x.StoreId1,
                        principalTable: "Stores",
                        principalColumn: "StoreId");
                });

            // ========== SEED DATA: Egypt Location System ==========

            // 1. Seed Egypt Country
            var egyptId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "CountryId", "Name", "Code", "NameArabic", "IsActive", "SupportsPostalCodes", "CreatedAt", "UpdatedAt" },
                values: new object[] { egyptId, "Egypt", "EG", "مصر", true, false, DateTime.UtcNow, DateTime.UtcNow });

            // 2. Seed Egyptian Governorates (27 governorates)
            var cairoId = Guid.Parse("22222222-2222-2222-2222-222222222201");
            var gizaId = Guid.Parse("22222222-2222-2222-2222-222222222202");
            var alexandriaId = Guid.Parse("22222222-2222-2222-2222-222222222203");

            migrationBuilder.InsertData(
                table: "Governorates",
                columns: new[] { "GovernorateId", "CountryId", "Name", "NameArabic", "Code", "IsActive", "SupportsSameDayDelivery", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    // Greater Cairo Region - Same-Day Delivery
                    { cairoId, egyptId, "Cairo", "القاهرة", "C", true, true, DateTime.UtcNow, DateTime.UtcNow },
                    { gizaId, egyptId, "Giza", "الجيزة", "GZ", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222204"), egyptId, "Qalyubia", "القليوبية", "KB", true, false, DateTime.UtcNow, DateTime.UtcNow },

                    // Alexandria and Delta Region
                    { alexandriaId, egyptId, "Alexandria", "الإسكندرية", "ALX", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222205"), egyptId, "Beheira", "البحيرة", "BH", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222206"), egyptId, "Gharbia", "الغربية", "GH", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222207"), egyptId, "Kafr El Sheikh", "كفر الشيخ", "KFS", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222208"), egyptId, "Dakahlia", "الدقهلية", "DK", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222209"), egyptId, "Damietta", "دمياط", "DT", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222210"), egyptId, "Monufia", "المنوفية", "MN", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222211"), egyptId, "Sharqia", "الشرقية", "SH", true, false, DateTime.UtcNow, DateTime.UtcNow },

                    // Canal Region
                    { Guid.Parse("22222222-2222-2222-2222-222222222212"), egyptId, "Port Said", "بورسعيد", "PTS", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222213"), egyptId, "Ismailia", "الإسماعيلية", "IS", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222214"), egyptId, "Suez", "السويس", "SUZ", true, false, DateTime.UtcNow, DateTime.UtcNow },

                    // Sinai Region
                    { Guid.Parse("22222222-2222-2222-2222-222222222215"), egyptId, "North Sinai", "شمال سيناء", "SIN", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222216"), egyptId, "South Sinai", "جنوب سيناء", "JS", true, false, DateTime.UtcNow, DateTime.UtcNow },

                    // Upper Egypt Region
                    { Guid.Parse("22222222-2222-2222-2222-222222222217"), egyptId, "Beni Suef", "بني سويف", "BNS", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222218"), egyptId, "Fayoum", "الفيوم", "FYM", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222219"), egyptId, "Minya", "المنيا", "MNY", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222220"), egyptId, "Asyut", "أسيوط", "AST", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222221"), egyptId, "Sohag", "سوهاج", "SHG", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222222"), egyptId, "Qena", "قنا", "KN", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222223"), egyptId, "Luxor", "الأقصر", "LXR", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222224"), egyptId, "Aswan", "أسوان", "ASN", true, false, DateTime.UtcNow, DateTime.UtcNow },

                    // Red Sea and Western Desert
                    { Guid.Parse("22222222-2222-2222-2222-222222222225"), egyptId, "Red Sea", "البحر الأحمر", "BA", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222226"), egyptId, "New Valley", "الوادي الجديد", "WAD", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222227"), egyptId, "Matrouh", "مطروح", "MT", true, false, DateTime.UtcNow, DateTime.UtcNow }
                });

            // 3. Seed Major Cities (Cairo, Giza, Alexandria)
            migrationBuilder.InsertData(
                table: "Cities",
                columns: new[] { "CityId", "GovernorateId", "Name", "NameArabic", "Code", "IsActive", "SupportsSameDayDelivery", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    // Cairo Governorate Cities - Same-Day Delivery Enabled
                    { Guid.Parse("33333333-3333-3333-3333-333333330101"), cairoId, "Nasr City", "مدينة نصر", "NC", true, true, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330102"), cairoId, "Heliopolis", "مصر الجديدة", "HLP", true, true, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330103"), cairoId, "Maadi", "المعادي", "MD", true, true, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330104"), cairoId, "Zamalek", "الزمالك", "ZM", true, true, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330105"), cairoId, "Dokki", "الدقي", "DK", true, true, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330106"), cairoId, "Mohandessin", "المهندسين", "MH", true, true, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330107"), cairoId, "New Cairo", "القاهرة الجديدة", "NCR", true, true, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330108"), cairoId, "Shorouk", "الشروق", "SHR", true, true, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330109"), cairoId, "Fifth Settlement", "التجمع الخامس", "5TH", true, true, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330110"), cairoId, "Rehab City", "مدينة الرحاب", "RHB", true, true, DateTime.UtcNow, DateTime.UtcNow },

                    // Giza Governorate Cities - National Delivery Only
                    { Guid.Parse("33333333-3333-3333-3333-333333330201"), gizaId, "6th of October City", "مدينة 6 أكتوبر", "6OCT", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330202"), gizaId, "Sheikh Zayed", "الشيخ زايد", "SZ", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330203"), gizaId, "Haram", "الهرم", "HRM", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330204"), gizaId, "Faisal", "فيصل", "FS", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330205"), gizaId, "Imbaba", "إمبابة", "IMB", true, false, DateTime.UtcNow, DateTime.UtcNow },

                    // Alexandria Governorate Cities - National Delivery Only
                    { Guid.Parse("33333333-3333-3333-3333-333333330301"), alexandriaId, "Miami", "ميامي", "MIA", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330302"), alexandriaId, "Sidi Gaber", "سيدي جابر", "SG", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330303"), alexandriaId, "Stanley", "ستانلي", "STN", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330304"), alexandriaId, "Smouha", "سموحة", "SMH", true, false, DateTime.UtcNow, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333330305"), alexandriaId, "Montaza", "المنتزه", "MTZ", true, false, DateTime.UtcNow, DateTime.UtcNow }
                });

            // ========== END SEED DATA ==========

            migrationBuilder.CreateIndex(
                name: "IX_StoreShippingConfigurations_StoreId1",
                table: "StoreShippingConfigurations",
                column: "StoreId1",
                unique: true,
                filter: "[StoreId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_GovernorateId",
                table: "Cities",
                column: "GovernorateId");

            migrationBuilder.CreateIndex(
                name: "IX_Governorates_CountryId",
                table: "Governorates",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreGovernorateSupports_GovernorateId",
                table: "StoreGovernorateSupports",
                column: "GovernorateId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreGovernorateSupports_StoreId_GovernorateId",
                table: "StoreGovernorateSupports",
                columns: new[] { "StoreId", "GovernorateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreGovernorateSupports_StoreId1",
                table: "StoreGovernorateSupports",
                column: "StoreId1");

            migrationBuilder.AddForeignKey(
                name: "FK_StoreShippingConfigurations_Stores_StoreId1",
                table: "StoreShippingConfigurations",
                column: "StoreId1",
                principalTable: "Stores",
                principalColumn: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreShippingConfigurations_Stores_StoreId1",
                table: "StoreShippingConfigurations");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "StoreGovernorateSupports");

            migrationBuilder.DropTable(
                name: "Governorates");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_StoreShippingConfigurations_StoreId1",
                table: "StoreShippingConfigurations");

            migrationBuilder.DropColumn(
                name: "StoreId1",
                table: "StoreShippingConfigurations");

            migrationBuilder.AddColumn<string>(
                name: "ExcludedCities",
                table: "StoreShippingConfigurations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExcludedCitiesList",
                table: "StoreShippingConfigurations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupportedCities",
                table: "StoreShippingConfigurations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupportedCitiesList",
                table: "StoreShippingConfigurations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
