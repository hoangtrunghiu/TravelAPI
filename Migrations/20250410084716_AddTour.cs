using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TourDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeTour = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NameTour = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PromotionallPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CountryFrom = table.Column<int>(type: "int", nullable: false),
                    CountryTo = table.Column<int>(type: "int", nullable: true),
                    Hotel = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Flight = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Timeline = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Promotion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Avatar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Creater = table.Column<int>(type: "int", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false),
                    IsHot = table.Column<bool>(type: "bit", nullable: false),
                    IsHide = table.Column<bool>(type: "bit", nullable: false),
                    MainCategoryTourId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourDetails_CategoryTours_MainCategoryTourId",
                        column: x => x.MainCategoryTourId,
                        principalTable: "CategoryTours",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TourCategoryMapping",
                columns: table => new
                {
                    TourDetailId = table.Column<int>(type: "int", nullable: false),
                    CategoryTourId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourCategoryMapping", x => new { x.TourDetailId, x.CategoryTourId });
                    table.ForeignKey(
                        name: "FK_TourCategoryMapping_CategoryTours_CategoryTourId",
                        column: x => x.CategoryTourId,
                        principalTable: "CategoryTours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TourCategoryMapping_TourDetails_TourDetailId",
                        column: x => x.TourDetailId,
                        principalTable: "TourDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TourDetailId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourDates_TourDetails_TourDetailId",
                        column: x => x.TourDetailId,
                        principalTable: "TourDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourDepartures",
                columns: table => new
                {
                    TourDetailId = table.Column<int>(type: "int", nullable: false),
                    DeparturePointId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourDepartures", x => new { x.DeparturePointId, x.TourDetailId });
                    table.ForeignKey(
                        name: "FK_TourDepartures_DeparturePoints_DeparturePointId",
                        column: x => x.DeparturePointId,
                        principalTable: "DeparturePoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TourDepartures_TourDetails_TourDetailId",
                        column: x => x.TourDetailId,
                        principalTable: "TourDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourDestinations",
                columns: table => new
                {
                    TourDetailId = table.Column<int>(type: "int", nullable: false),
                    DestinationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourDestinations", x => new { x.DestinationId, x.TourDetailId });
                    table.ForeignKey(
                        name: "FK_TourDestinations_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TourDestinations_TourDetails_TourDetailId",
                        column: x => x.TourDetailId,
                        principalTable: "TourDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TourCategoryMapping_CategoryTourId",
                table: "TourCategoryMapping",
                column: "CategoryTourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourDates_TourDetailId",
                table: "TourDates",
                column: "TourDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_TourDepartures_TourDetailId",
                table: "TourDepartures",
                column: "TourDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_TourDestinations_TourDetailId",
                table: "TourDestinations",
                column: "TourDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_TourDetails_MainCategoryTourId",
                table: "TourDetails",
                column: "MainCategoryTourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourDetails_Url",
                table: "TourDetails",
                column: "Url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourCategoryMapping");

            migrationBuilder.DropTable(
                name: "TourDates");

            migrationBuilder.DropTable(
                name: "TourDepartures");

            migrationBuilder.DropTable(
                name: "TourDestinations");

            migrationBuilder.DropTable(
                name: "TourDetails");
        }
    }
}
