using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subscription.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "subscription");

            // ── subscription_plans ────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[subscription].[subscription_plans]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [subscription].[subscription_plans] (
                        [Id]                       uniqueidentifier NOT NULL,
                        [Name]                     nvarchar(100)    NOT NULL,
                        [Tier]                     nvarchar(450)    NOT NULL,
                        [PriceMonthly]             decimal(10,2)    NOT NULL,
                        [PriceAnnual]              decimal(10,2)    NOT NULL,
                        [MaxProperties]            int              NOT NULL,
                        [MaxBrokers]               int              NOT NULL,
                        [MaxAiRequestsPerMonth]    int              NOT NULL,
                        [MaxStorageGb]             int              NOT NULL,
                        [HasAdvancedAnalytics]     bit              NOT NULL DEFAULT CAST(0 AS bit),
                        [HasFeaturedListings]      bit              NOT NULL DEFAULT CAST(0 AS bit),
                        [HasAiPriceEstimation]     bit              NOT NULL DEFAULT CAST(0 AS bit),
                        [HasAiChatbot]             bit              NOT NULL DEFAULT CAST(0 AS bit),
                        [HasAiLeadPrioritization]  bit              NOT NULL DEFAULT CAST(0 AS bit),
                        [HasAiDescriptionGenerator] bit             NOT NULL DEFAULT CAST(0 AS bit),
                        [IsActive]                 bit              NOT NULL DEFAULT CAST(1 AS bit),
                        [MaxSavedListings]         int              NOT NULL DEFAULT 0,
                        [MaxSavedSearches]         int              NOT NULL DEFAULT 0,
                        [MaxInquiriesPerMonth]     int              NOT NULL DEFAULT 0,
                        [HasEarlyAccessListings]   bit              NOT NULL DEFAULT CAST(0 AS bit),
                        [HasPriorityBrokerResponse] bit             NOT NULL DEFAULT CAST(0 AS bit),
                        [CreatedAt]                datetime2        NOT NULL,
                        [UpdatedAt]                datetime2        NULL,
                        [DeletedAt]                datetime2        NULL,
                        CONSTRAINT [PK_subscription_plans] PRIMARY KEY ([Id])
                    );
                END
            ");

            // ── plan_features ─────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[subscription].[plan_features]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [subscription].[plan_features] (
                        [Id]           uniqueidentifier NOT NULL,
                        [PlanId]       uniqueidentifier NOT NULL,
                        [FeatureKey]   nvarchar(100)    NOT NULL,
                        [FeatureLabel] nvarchar(255)    NOT NULL,
                        [FeatureValue] nvarchar(255)    NOT NULL,
                        [CreatedAt]    datetime2        NOT NULL,
                        [UpdatedAt]    datetime2        NULL,
                        [DeletedAt]    datetime2        NULL,
                        CONSTRAINT [PK_plan_features] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_plan_features_subscription_plans_PlanId]
                            FOREIGN KEY ([PlanId]) REFERENCES [subscription].[subscription_plans] ([Id])
                            ON DELETE CASCADE
                    );
                END
            ");

            // ── subscriptions ─────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[subscription].[subscriptions]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [subscription].[subscriptions] (
                        [Id]                  uniqueidentifier NOT NULL,
                        [PlanId]              uniqueidentifier NOT NULL,
                        [UserId]              uniqueidentifier NOT NULL,
                        [CompanyId]           uniqueidentifier NULL,
                        [BillingCycle]        nvarchar(max)    NOT NULL,
                        [Status]              nvarchar(450)    NOT NULL,
                        [StartDate]           datetime2        NOT NULL,
                        [EndDate]             datetime2        NOT NULL,
                        [TrialEndDate]        datetime2        NULL,
                        [IsAutoRenew]         bit              NOT NULL DEFAULT CAST(1 AS bit),
                        [CancelledAt]         datetime2        NULL,
                        [CancellationReason]  nvarchar(max)    NULL,
                        [UpgradeFromPlanId]   uniqueidentifier NULL,
                        [DowngradeToPlanId]   uniqueidentifier NULL,
                        [CreatedAt]           datetime2        NOT NULL,
                        [UpdatedAt]           datetime2        NULL,
                        [DeletedAt]           datetime2        NULL,
                        CONSTRAINT [PK_subscriptions] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_subscriptions_subscription_plans_PlanId]
                            FOREIGN KEY ([PlanId]) REFERENCES [subscription].[subscription_plans] ([Id])
                            ON DELETE NO ACTION
                    );
                END
            ");

            // ── subscription_usage ────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[subscription].[subscription_usage]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [subscription].[subscription_usage] (
                        [Id]               uniqueidentifier NOT NULL,
                        [SubscriptionId]   uniqueidentifier NOT NULL,
                        [PeriodStart]      datetime2        NOT NULL,
                        [PeriodEnd]        datetime2        NOT NULL,
                        [PropertiesUsed]   int              NOT NULL DEFAULT 0,
                        [BrokersUsed]      int              NOT NULL DEFAULT 0,
                        [AiRequestsUsed]   int              NOT NULL DEFAULT 0,
                        [StorageUsedGb]    decimal(10,4)    NOT NULL DEFAULT 0,
                        [SavedListingsUsed]  int            NOT NULL DEFAULT 0,
                        [SavedSearchesUsed]  int            NOT NULL DEFAULT 0,
                        [InquiriesUsed]    int              NOT NULL DEFAULT 0,
                        [CreatedAt]        datetime2        NOT NULL,
                        [UpdatedAt]        datetime2        NULL,
                        [DeletedAt]        datetime2        NULL,
                        CONSTRAINT [PK_subscription_usage] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_subscription_usage_subscriptions_SubscriptionId]
                            FOREIGN KEY ([SubscriptionId]) REFERENCES [subscription].[subscriptions] ([Id])
                            ON DELETE CASCADE
                    );
                END
            ");

            // ── Indexes (all guarded) ─────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_plan_features_PlanId'
                                 AND object_id = OBJECT_ID(N'[subscription].[plan_features]'))
                    CREATE INDEX [IX_plan_features_PlanId]
                        ON [subscription].[plan_features] ([PlanId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_subscription_plans_IsActive'
                                 AND object_id = OBJECT_ID(N'[subscription].[subscription_plans]'))
                    CREATE INDEX [IX_subscription_plans_IsActive]
                        ON [subscription].[subscription_plans] ([IsActive]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_subscription_plans_Tier'
                                 AND object_id = OBJECT_ID(N'[subscription].[subscription_plans]'))
                    CREATE INDEX [IX_subscription_plans_Tier]
                        ON [subscription].[subscription_plans] ([Tier]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_subscription_usage_SubscriptionId'
                                 AND object_id = OBJECT_ID(N'[subscription].[subscription_usage]'))
                    CREATE INDEX [IX_subscription_usage_SubscriptionId]
                        ON [subscription].[subscription_usage] ([SubscriptionId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_subscriptions_CompanyId'
                                 AND object_id = OBJECT_ID(N'[subscription].[subscriptions]'))
                    CREATE INDEX [IX_subscriptions_CompanyId]
                        ON [subscription].[subscriptions] ([CompanyId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_subscriptions_PlanId'
                                 AND object_id = OBJECT_ID(N'[subscription].[subscriptions]'))
                    CREATE INDEX [IX_subscriptions_PlanId]
                        ON [subscription].[subscriptions] ([PlanId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_subscriptions_Status'
                                 AND object_id = OBJECT_ID(N'[subscription].[subscriptions]'))
                    CREATE INDEX [IX_subscriptions_Status]
                        ON [subscription].[subscriptions] ([Status]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_subscriptions_UserId'
                                 AND object_id = OBJECT_ID(N'[subscription].[subscriptions]'))
                    CREATE INDEX [IX_subscriptions_UserId]
                        ON [subscription].[subscriptions] ([UserId]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "plan_features",        schema: "subscription");
            migrationBuilder.DropTable(name: "subscription_usage",   schema: "subscription");
            migrationBuilder.DropTable(name: "subscriptions",        schema: "subscription");
            migrationBuilder.DropTable(name: "subscription_plans",   schema: "subscription");
        }
    }
}
