using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WalletOnTraderDropDemoCash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE t
                SET t.AvailableCash = t.AvailableCash + da.CashAvailable
                FROM PetTraders AS t
                INNER JOIN DemoAccounts AS da ON t.ExternalUserId = da.UserId;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO PetTraders (Id, DisplayName, ExternalUserId, AvailableCash, LockedCash, CreatedAt)
                SELECT NEWID(), da.DisplayName, da.UserId, da.CashAvailable, 0, SYSUTCDATETIME()
                FROM DemoAccounts AS da
                WHERE NOT EXISTS (
                    SELECT 1 FROM PetTraders AS t WHERE t.ExternalUserId = da.UserId);
                """);

            migrationBuilder.DropColumn(
                name: "CashAvailable",
                table: "DemoAccounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CashAvailable",
                table: "DemoAccounts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "DemoAccounts",
                keyColumn: "UserId",
                keyValue: "user-1",
                column: "CashAvailable",
                value: 250000m);

            migrationBuilder.UpdateData(
                table: "DemoAccounts",
                keyColumn: "UserId",
                keyValue: "user-2",
                column: "CashAvailable",
                value: 150000m);

            migrationBuilder.UpdateData(
                table: "DemoAccounts",
                keyColumn: "UserId",
                keyValue: "user-3",
                column: "CashAvailable",
                value: 50000m);
        }
    }
}
