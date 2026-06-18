using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Infrastructure.Persistence.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF SCHEMA_ID(N'wallet') IS NULL
                    EXEC(N'CREATE SCHEMA [wallet]');
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[wallet].[wallet_balances]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [wallet].[wallet_balances] (
                        [Id]        uniqueidentifier NOT NULL,
                        [OwnerId]   uniqueidentifier NOT NULL,
                        [Balance]   decimal(18,4) NOT NULL DEFAULT 0,
                        [Currency]  nvarchar(3) NOT NULL DEFAULT N'AED',
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_wallet_balances] PRIMARY KEY ([Id])
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_wallet_balances_OwnerId'
                                 AND object_id = OBJECT_ID(N'[wallet].[wallet_balances]'))
                    CREATE UNIQUE INDEX [IX_wallet_balances_OwnerId]
                        ON [wallet].[wallet_balances] ([OwnerId]);
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[wallet].[wallet_ledger_entries]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [wallet].[wallet_ledger_entries] (
                        [Id]          uniqueidentifier NOT NULL,
                        [WalletId]    uniqueidentifier NOT NULL,
                        [PaymentId]   uniqueidentifier NOT NULL,
                        [Type]        nvarchar(20) NOT NULL,
                        [Amount]      decimal(18,4) NOT NULL,
                        [Description] nvarchar(500) NOT NULL,
                        [OccurredAt]  datetime2 NOT NULL,
                        CONSTRAINT [PK_wallet_ledger_entries] PRIMARY KEY ([Id])
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_wallet_ledger_entries_PaymentId'
                                 AND object_id = OBJECT_ID(N'[wallet].[wallet_ledger_entries]'))
                    CREATE UNIQUE INDEX [IX_wallet_ledger_entries_PaymentId]
                        ON [wallet].[wallet_ledger_entries] ([PaymentId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_wallet_ledger_entries_WalletId'
                                 AND object_id = OBJECT_ID(N'[wallet].[wallet_ledger_entries]'))
                    CREATE INDEX [IX_wallet_ledger_entries_WalletId]
                        ON [wallet].[wallet_ledger_entries] ([WalletId]);
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[wallet].[wallet_withdrawal_requests]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [wallet].[wallet_withdrawal_requests] (
                        [Id]                 uniqueidentifier NOT NULL,
                        [OwnerId]            uniqueidentifier NOT NULL,
                        [Amount]             decimal(18,4) NOT NULL,
                        [Currency]           nvarchar(3) NOT NULL DEFAULT N'AED',
                        [BankAccountDetails] nvarchar(1000) NOT NULL,
                        [Status]             nvarchar(20) NOT NULL DEFAULT N'Pending',
                        [RequestedAt]        datetime2 NOT NULL,
                        [ProcessedAt]        datetime2 NULL,
                        [Notes]              nvarchar(500) NULL,
                        CONSTRAINT [PK_wallet_withdrawal_requests] PRIMARY KEY ([Id])
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_wallet_withdrawal_requests_OwnerId'
                                 AND object_id = OBJECT_ID(N'[wallet].[wallet_withdrawal_requests]'))
                    CREATE INDEX [IX_wallet_withdrawal_requests_OwnerId]
                        ON [wallet].[wallet_withdrawal_requests] ([OwnerId]);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [wallet].[wallet_withdrawal_requests];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [wallet].[wallet_ledger_entries];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [wallet].[wallet_balances];");
        }
    }
}
