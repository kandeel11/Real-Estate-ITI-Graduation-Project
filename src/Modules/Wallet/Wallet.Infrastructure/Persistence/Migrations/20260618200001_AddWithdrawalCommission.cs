using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Infrastructure.Persistence.Migrations
{
    public partial class AddWithdrawalCommission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CommissionAmount",
                schema: "wallet",
                table: "wallet_withdrawal_requests",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetAmount",
                schema: "wallet",
                table: "wallet_withdrawal_requests",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommissionAmount",
                schema: "wallet",
                table: "wallet_withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "NetAmount",
                schema: "wallet",
                table: "wallet_withdrawal_requests");
        }
    }
}
