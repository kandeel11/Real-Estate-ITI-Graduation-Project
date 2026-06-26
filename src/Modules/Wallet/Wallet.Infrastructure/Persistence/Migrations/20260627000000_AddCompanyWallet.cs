using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                schema: "wallet",
                table: "wallet_balances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                schema: "wallet",
                table: "wallet_withdrawal_requests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_balances_CompanyId",
                schema: "wallet",
                table: "wallet_balances",
                column: "CompanyId",
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_withdrawal_requests_CompanyId",
                schema: "wallet",
                table: "wallet_withdrawal_requests",
                column: "CompanyId",
                filter: "[CompanyId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wallet_balances_CompanyId",
                schema: "wallet",
                table: "wallet_balances");

            migrationBuilder.DropIndex(
                name: "IX_wallet_withdrawal_requests_CompanyId",
                schema: "wallet",
                table: "wallet_withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "wallet",
                table: "wallet_balances");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "wallet",
                table: "wallet_withdrawal_requests");
        }
    }
}
