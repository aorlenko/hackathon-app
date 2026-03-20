using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TradingPets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PetBreeds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LifespanYears = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BaselineDesirability = table.Column<int>(type: "int", nullable: false),
                    MaintenanceCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RetailPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetBreeds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PetTraders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExternalUserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AvailableCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LockedCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetTraders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PetBreedSupply",
                columns: table => new
                {
                    BreedId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RemainingCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetBreedSupply", x => x.BreedId);
                    table.ForeignKey(
                        name: "FK_PetBreedSupply_PetBreeds_BreedId",
                        column: x => x.BreedId,
                        principalTable: "PetBreeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PetNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CounterpartyTraderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CounterpartyDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Correlation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PetNotifications_PetTraders_TraderId",
                        column: x => x.TraderId,
                        principalTable: "PetTraders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BreedId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerTraderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgeYears = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Health = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentDesirability = table.Column<int>(type: "int", nullable: false),
                    IsExpired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pets_PetBreeds_BreedId",
                        column: x => x.BreedId,
                        principalTable: "PetBreeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pets_PetTraders_OwnerTraderId",
                        column: x => x.OwnerTraderId,
                        principalTable: "PetTraders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PetListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerTraderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AskingPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    WithdrawnAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PetListings_PetTraders_SellerTraderId",
                        column: x => x.SellerTraderId,
                        principalTable: "PetTraders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PetListings_Pets_PetId",
                        column: x => x.PetId,
                        principalTable: "Pets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PetListingBids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuyerTraderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetListingBids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PetListingBids_PetListings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "PetListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PetListingBids_PetTraders_BuyerTraderId",
                        column: x => x.BuyerTraderId,
                        principalTable: "PetTraders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PetTrades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuyerTraderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerTraderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetTrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PetTrades_PetListings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "PetListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PetTrades_PetTraders_BuyerTraderId",
                        column: x => x.BuyerTraderId,
                        principalTable: "PetTraders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PetTrades_PetTraders_SellerTraderId",
                        column: x => x.SellerTraderId,
                        principalTable: "PetTraders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PetTrades_Pets_PetId",
                        column: x => x.PetId,
                        principalTable: "Pets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PetListingBids_BuyerTraderId",
                table: "PetListingBids",
                column: "BuyerTraderId");

            migrationBuilder.CreateIndex(
                name: "IX_PetListingBids_ListingId_Status",
                table: "PetListingBids",
                columns: new[] { "ListingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PetListings_CreatedAt",
                table: "PetListings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PetListings_PetId_WithdrawnAt",
                table: "PetListings",
                columns: new[] { "PetId", "WithdrawnAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PetListings_SellerTraderId",
                table: "PetListings",
                column: "SellerTraderId");

            migrationBuilder.CreateIndex(
                name: "IX_PetNotifications_TraderId_CreatedAt",
                table: "PetNotifications",
                columns: new[] { "TraderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Pets_BreedId",
                table: "Pets",
                column: "BreedId");

            migrationBuilder.CreateIndex(
                name: "IX_Pets_OwnerTraderId",
                table: "Pets",
                column: "OwnerTraderId");

            migrationBuilder.CreateIndex(
                name: "IX_PetTraders_ExternalUserId",
                table: "PetTraders",
                column: "ExternalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PetTrades_BuyerTraderId",
                table: "PetTrades",
                column: "BuyerTraderId");

            migrationBuilder.CreateIndex(
                name: "IX_PetTrades_ExecutedAt",
                table: "PetTrades",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PetTrades_ListingId",
                table: "PetTrades",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_PetTrades_PetId",
                table: "PetTrades",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_PetTrades_SellerTraderId",
                table: "PetTrades",
                column: "SellerTraderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PetBreedSupply");

            migrationBuilder.DropTable(
                name: "PetListingBids");

            migrationBuilder.DropTable(
                name: "PetNotifications");

            migrationBuilder.DropTable(
                name: "PetTrades");

            migrationBuilder.DropTable(
                name: "PetListings");

            migrationBuilder.DropTable(
                name: "Pets");

            migrationBuilder.DropTable(
                name: "PetBreeds");

            migrationBuilder.DropTable(
                name: "PetTraders");
        }
    }
}
