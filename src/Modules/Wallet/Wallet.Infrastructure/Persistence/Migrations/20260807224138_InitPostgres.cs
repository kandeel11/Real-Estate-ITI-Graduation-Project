using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "wallet");

            migrationBuilder.CreateTable(
                name: "wallet_balances",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_balances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wallet_ledger_entries",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_ledger_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wallet_withdrawal_requests",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    NetAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    BankAccountDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_withdrawal_requests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wallet_balances_CompanyId",
                schema: "wallet",
                table: "wallet_balances",
                column: "CompanyId",
                unique: true,
                filter: "\"CompanyId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_balances_OwnerId",
                schema: "wallet",
                table: "wallet_balances",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_ledger_entries_PaymentId",
                schema: "wallet",
                table: "wallet_ledger_entries",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_ledger_entries_WalletId",
                schema: "wallet",
                table: "wallet_ledger_entries",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_withdrawal_requests_CompanyId",
                schema: "wallet",
                table: "wallet_withdrawal_requests",
                column: "CompanyId",
                filter: "\"CompanyId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_withdrawal_requests_OwnerId",
                schema: "wallet",
                table: "wallet_withdrawal_requests",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wallet_balances",
                schema: "wallet");

            migrationBuilder.DropTable(
                name: "wallet_ledger_entries",
                schema: "wallet");

            migrationBuilder.DropTable(
                name: "wallet_withdrawal_requests",
                schema: "wallet");
        }
    }
}
