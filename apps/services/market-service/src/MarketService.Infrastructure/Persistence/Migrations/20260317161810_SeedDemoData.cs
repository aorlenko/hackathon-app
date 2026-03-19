using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MarketService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemoData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DemoAccounts",
                columns: new[] { "UserId", "CashAvailable", "DisplayName", "Email" },
                values: new object[,]
                {
                    { "user-1", 250000m, "Buyer One", "user1@example.com" },
                    { "user-2", 150000m, "Seller Two", "user2@example.com" },
                    { "user-3", 50000m, "Observer Three", "user3@example.com" }
                });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "ItemId", "Category", "CreatedAtUtc", "IsTradable", "Name", "ReferencePrice", "Symbol" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Equity", new DateTimeOffset(new DateTime(2026, 3, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Acme Beverage Co", 100m, "ABC" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Equity", new DateTimeOffset(new DateTime(2026, 3, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Xylophone Yield Zone", 80m, "XYZ" }
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DemoHoldings",
                keyColumns: new[] { "Symbol", "UserId" },
                keyValues: new object[] { "ABC", "user-1" });

            migrationBuilder.DeleteData(
                table: "DemoHoldings",
                keyColumns: new[] { "Symbol", "UserId" },
                keyValues: new object[] { "ABC", "user-2" });

            migrationBuilder.DeleteData(
                table: "DemoHoldings",
                keyColumns: new[] { "Symbol", "UserId" },
                keyValues: new object[] { "XYZ", "user-3" });

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "DemoAccounts",
                keyColumn: "UserId",
                keyValue: "user-1");

            migrationBuilder.DeleteData(
                table: "DemoAccounts",
                keyColumn: "UserId",
                keyValue: "user-2");

            migrationBuilder.DeleteData(
                table: "DemoAccounts",
                keyColumn: "UserId",
                keyValue: "user-3");
        }
    }
}
