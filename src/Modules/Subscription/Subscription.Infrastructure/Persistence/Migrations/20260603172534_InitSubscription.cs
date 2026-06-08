using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subscription.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "subscription");

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PriceMonthly = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PriceAnnual = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MaxProperties = table.Column<int>(type: "int", nullable: false),
                    MaxBrokers = table.Column<int>(type: "int", nullable: false),
                    MaxAiRequestsPerMonth = table.Column<int>(type: "int", nullable: false),
                    MaxStorageGb = table.Column<int>(type: "int", nullable: false),
                    HasAdvancedAnalytics = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasFeaturedListings = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasAiPriceEstimation = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasAiChatbot = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasAiLeadPrioritization = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasAiDescriptionGenerator = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "plan_features",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FeatureLabel = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FeatureValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_features", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plan_features_subscription_plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "subscription",
                        principalTable: "subscription_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BillingCycle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrialEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAutoRenew = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpgradeFromPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DowngradeToPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscriptions_subscription_plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "subscription",
                        principalTable: "subscription_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_payments",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvoiceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscription_payments_subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "subscription",
                        principalTable: "subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscription_usage",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PropertiesUsed = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    BrokersUsed = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AiRequestsUsed = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    StorageUsedGb = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_usage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscription_usage_subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "subscription",
                        principalTable: "subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plan_features_PlanId",
                schema: "subscription",
                table: "plan_features",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payments_Status",
                schema: "subscription",
                table: "subscription_payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payments_SubscriptionId",
                schema: "subscription",
                table: "subscription_payments",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payments_TransactionId",
                schema: "subscription",
                table: "subscription_payments",
                column: "TransactionId",
                unique: true,
                filter: "[TransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_IsActive",
                schema: "subscription",
                table: "subscription_plans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_Tier",
                schema: "subscription",
                table: "subscription_plans",
                column: "Tier");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_usage_SubscriptionId",
                schema: "subscription",
                table: "subscription_usage",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_CompanyId",
                schema: "subscription",
                table: "subscriptions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_PlanId",
                schema: "subscription",
                table: "subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_Status",
                schema: "subscription",
                table: "subscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId",
                schema: "subscription",
                table: "subscriptions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_features",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "subscription_payments",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "subscription_usage",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "subscription_plans",
                schema: "subscription");
        }
    }
}
