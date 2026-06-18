using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "payment");

            // ── outbox_messages ───────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[payment].[outbox_messages]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [payment].[outbox_messages] (
                        [Id]          uniqueidentifier  NOT NULL,
                        [Type]        nvarchar(200)     NOT NULL,
                        [Content]     nvarchar(max)     NOT NULL,
                        [OccurredOn]  datetime2         NOT NULL,
                        [ProcessedOn] datetime2         NULL,
                        [Error]       nvarchar(2000)    NULL,
                        CONSTRAINT [PK_outbox_messages] PRIMARY KEY ([Id])
                    );
                END
            ");

            // ── payments ──────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[payment].[payments]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [payment].[payments] (
                        [Id]                uniqueidentifier NOT NULL,
                        [Amount]            decimal(12,2)   NOT NULL,
                        [Currency]          nvarchar(10)    NOT NULL,
                        [Status]            nvarchar(20)    NOT NULL,
                        [PaymentMethod]     nvarchar(20)    NOT NULL,
                        [TransactionId]     nvarchar(200)   NULL,
                        [CheckoutSessionId] nvarchar(200)   NULL,
                        [CheckoutUrl]       nvarchar(1000)  NULL,
                        [PaymentType]       nvarchar(20)    NOT NULL DEFAULT N'Rent',
                        [CreatedAt]         datetime2       NOT NULL,
                        [UpdatedAt]         datetime2       NOT NULL,
                        [DueDate]           date            NOT NULL,
                        [PaidAt]            datetime2       NULL,
                        [LateFee]           decimal(10,2)   NOT NULL DEFAULT 0.0,
                        [AiPredictedLate]   bit             NOT NULL DEFAULT CAST(0 AS bit),
                        [ReceiptUrl]        nvarchar(500)   NULL,
                        [Notes]             nvarchar(1000)  NULL,
                        CONSTRAINT [PK_payments] PRIMARY KEY ([Id])
                    );
                END
            ");

            // ── rent_payment_details ──────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[payment].[rent_payment_details]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [payment].[rent_payment_details] (
                        [PaymentId]  uniqueidentifier NOT NULL,
                        [LeaseId]    uniqueidentifier NOT NULL,
                        [PropertyId] uniqueidentifier NOT NULL,
                        [TenantId]   uniqueidentifier NOT NULL,
                        [LandlordId] uniqueidentifier NOT NULL,
                        CONSTRAINT [PK_rent_payment_details] PRIMARY KEY ([PaymentId]),
                        CONSTRAINT [FK_rent_payment_details_payments_PaymentId]
                            FOREIGN KEY ([PaymentId]) REFERENCES [payment].[payments] ([Id])
                            ON DELETE CASCADE
                    );
                END
            ");

            // ── subscription_payment_details ──────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[payment].[subscription_payment_details]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [payment].[subscription_payment_details] (
                        [PaymentId]      uniqueidentifier NOT NULL,
                        [SubscriptionId] uniqueidentifier NOT NULL,
                        [PlanId]         uniqueidentifier NOT NULL,
                        [BillingCycle]   nvarchar(20)     NOT NULL,
                        CONSTRAINT [PK_subscription_payment_details] PRIMARY KEY ([PaymentId]),
                        CONSTRAINT [FK_subscription_payment_details_payments_PaymentId]
                            FOREIGN KEY ([PaymentId]) REFERENCES [payment].[payments] ([Id])
                            ON DELETE CASCADE
                    );
                END
            ");

            // ── transfer_payment_details ──────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[payment].[transfer_payment_details]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [payment].[transfer_payment_details] (
                        [PaymentId]  uniqueidentifier NOT NULL,
                        [SenderId]   uniqueidentifier NOT NULL,
                        [ReceiverId] uniqueidentifier NOT NULL,
                        [Purpose]    nvarchar(255)    NOT NULL,
                        CONSTRAINT [PK_transfer_payment_details] PRIMARY KEY ([PaymentId]),
                        CONSTRAINT [FK_transfer_payment_details_payments_PaymentId]
                            FOREIGN KEY ([PaymentId]) REFERENCES [payment].[payments] ([Id])
                            ON DELETE CASCADE
                    );
                END
            ");

            // ── rent_payments ─────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[payment].[rent_payments]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [payment].[rent_payments] (
                        [Id]                           uniqueidentifier NOT NULL,
                        [LeaseId]                      uniqueidentifier NOT NULL,
                        [Amount]                       decimal(12,2)    NOT NULL,
                        [DueDate]                      date             NOT NULL,
                        [PaidAt]                       datetime2        NULL,
                        [Status]                       nvarchar(20)     NOT NULL,
                        [PaymentMethod]                nvarchar(50)     NULL,
                        [TransactionId]                nvarchar(200)    NULL,
                        [LateFee]                      decimal(10,2)    NOT NULL DEFAULT 0.0,
                        [AiPredictedLate]              bit              NOT NULL DEFAULT CAST(0 AS bit),
                        [ReceiptUrl]                   nvarchar(500)    NULL,
                        [Notes]                        nvarchar(1000)   NULL,
                        [CreatedAt]                    datetime2        NOT NULL,
                        [UpdatedAt]                    datetime2        NOT NULL,
                        [PaymentType]                  nvarchar(20)     NOT NULL DEFAULT N'Rent',
                        [CheckoutSessionId]            nvarchar(200)    NULL,
                        [CheckoutUrl]                  nvarchar(1000)   NULL,
                        [SubscriptionDetailsPaymentId] uniqueidentifier NULL,
                        CONSTRAINT [PK_rent_payments] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_rent_payments_subscription_payment_details_SubscriptionDetailsPaymentId]
                            FOREIGN KEY ([SubscriptionDetailsPaymentId])
                            REFERENCES [payment].[subscription_payment_details] ([PaymentId])
                    );
                END
            ");

            // ── Schema evolution: add columns that may be missing from older DB ─
            // rent_payments may exist from a prior migration without these columns.

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[payment].[rent_payments]', N'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID(N'[payment].[rent_payments]')
                                     AND name = N'PaymentType')
                        ALTER TABLE [payment].[rent_payments]
                            ADD [PaymentType] nvarchar(20) NOT NULL DEFAULT N'Rent';

                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID(N'[payment].[rent_payments]')
                                     AND name = N'CheckoutSessionId')
                        ALTER TABLE [payment].[rent_payments]
                            ADD [CheckoutSessionId] nvarchar(200) NULL;

                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID(N'[payment].[rent_payments]')
                                     AND name = N'CheckoutUrl')
                        ALTER TABLE [payment].[rent_payments]
                            ADD [CheckoutUrl] nvarchar(1000) NULL;

                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID(N'[payment].[rent_payments]')
                                     AND name = N'SubscriptionDetailsPaymentId')
                        ALTER TABLE [payment].[rent_payments]
                            ADD [SubscriptionDetailsPaymentId] uniqueidentifier NULL;
                END
            ");

            // Add the FK from rent_payments -> subscription_payment_details if missing
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[payment].[rent_payments]', N'U') IS NOT NULL
                   AND OBJECT_ID(N'[payment].[subscription_payment_details]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1 FROM sys.foreign_keys
                       WHERE name = N'FK_rent_payments_subscription_payment_details_SubscriptionDetailsPaymentId'
                         AND parent_object_id = OBJECT_ID(N'[payment].[rent_payments]'))
                BEGIN
                    ALTER TABLE [payment].[rent_payments]
                        ADD CONSTRAINT [FK_rent_payments_subscription_payment_details_SubscriptionDetailsPaymentId]
                        FOREIGN KEY ([SubscriptionDetailsPaymentId])
                        REFERENCES [payment].[subscription_payment_details] ([PaymentId]);
                END
            ");

            // ── Indexes (all guarded) ─────────────────────────────────────────

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_outbox_messages_unprocessed'
                                 AND object_id = OBJECT_ID(N'[payment].[outbox_messages]'))
                    CREATE INDEX [IX_outbox_messages_unprocessed]
                        ON [payment].[outbox_messages] ([ProcessedOn])
                        WHERE [ProcessedOn] IS NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_payments_due_date'
                                 AND object_id = OBJECT_ID(N'[payment].[payments]'))
                    CREATE INDEX [IX_payments_due_date]
                        ON [payment].[payments] ([DueDate]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_payments_payment_type'
                                 AND object_id = OBJECT_ID(N'[payment].[payments]'))
                    CREATE INDEX [IX_payments_payment_type]
                        ON [payment].[payments] ([PaymentType]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_payments_status'
                                 AND object_id = OBJECT_ID(N'[payment].[payments]'))
                    CREATE INDEX [IX_payments_status]
                        ON [payment].[payments] ([Status]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_rent_payment_details_lease_id'
                                 AND object_id = OBJECT_ID(N'[payment].[rent_payment_details]'))
                    CREATE INDEX [IX_rent_payment_details_lease_id]
                        ON [payment].[rent_payment_details] ([LeaseId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_rent_payment_details_lease_payment'
                                 AND object_id = OBJECT_ID(N'[payment].[rent_payment_details]'))
                    CREATE INDEX [IX_rent_payment_details_lease_payment]
                        ON [payment].[rent_payment_details] ([LeaseId], [PaymentId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_rent_payments_lease_duedate'
                                 AND object_id = OBJECT_ID(N'[payment].[rent_payments]'))
                    CREATE INDEX [IX_rent_payments_lease_duedate]
                        ON [payment].[rent_payments] ([LeaseId], [DueDate]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_rent_payments_lease_id'
                                 AND object_id = OBJECT_ID(N'[payment].[rent_payments]'))
                    CREATE INDEX [IX_rent_payments_lease_id]
                        ON [payment].[rent_payments] ([LeaseId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_rent_payments_payment_type'
                                 AND object_id = OBJECT_ID(N'[payment].[rent_payments]'))
                    CREATE INDEX [IX_rent_payments_payment_type]
                        ON [payment].[rent_payments] ([PaymentType]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_rent_payments_status'
                                 AND object_id = OBJECT_ID(N'[payment].[rent_payments]'))
                    CREATE INDEX [IX_rent_payments_status]
                        ON [payment].[rent_payments] ([Status]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_rent_payments_SubscriptionDetailsPaymentId'
                                 AND object_id = OBJECT_ID(N'[payment].[rent_payments]'))
                    CREATE INDEX [IX_rent_payments_SubscriptionDetailsPaymentId]
                        ON [payment].[rent_payments] ([SubscriptionDetailsPaymentId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_subscription_payment_details_subscription_id'
                                 AND object_id = OBJECT_ID(N'[payment].[subscription_payment_details]'))
                    CREATE INDEX [IX_subscription_payment_details_subscription_id]
                        ON [payment].[subscription_payment_details] ([SubscriptionId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_transfer_payment_details_receiver_id'
                                 AND object_id = OBJECT_ID(N'[payment].[transfer_payment_details]'))
                    CREATE INDEX [IX_transfer_payment_details_receiver_id]
                        ON [payment].[transfer_payment_details] ([ReceiverId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_transfer_payment_details_sender_id'
                                 AND object_id = OBJECT_ID(N'[payment].[transfer_payment_details]'))
                    CREATE INDEX [IX_transfer_payment_details_sender_id]
                        ON [payment].[transfer_payment_details] ([SenderId]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "outbox_messages",              schema: "payment");
            migrationBuilder.DropTable(name: "rent_payment_details",         schema: "payment");
            migrationBuilder.DropTable(name: "rent_payments",                schema: "payment");
            migrationBuilder.DropTable(name: "transfer_payment_details",     schema: "payment");
            migrationBuilder.DropTable(name: "subscription_payment_details", schema: "payment");
            migrationBuilder.DropTable(name: "payments",                     schema: "payment");
        }
    }
}
