using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MarketService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropDemoHoldings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoHoldings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoHoldings",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoHoldings", x => new { x.UserId, x.Symbol });
                    table.ForeignKey(
                        name: "FK_DemoHoldings_DemoAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "DemoAccounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DemoHoldings",
                columns: new[] { "Symbol", "UserId", "Quantity" },
                values: new object[,]
                {
                    { "ABC", "user-1", 10 },
                    { "ABC", "user-2", 200 },
                    { "XYZ", "user-3", 25 }
                });
        }
    }
}
