using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subscription.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InquiriesUsed",
                schema: "subscription",
                table: "subscription_usage",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SavedListingsUsed",
                schema: "subscription",
                table: "subscription_usage",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SavedSearchesUsed",
                schema: "subscription",
                table: "subscription_usage",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasEarlyAccessListings",
                schema: "subscription",
                table: "subscription_plans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasPriorityBrokerResponse",
                schema: "subscription",
                table: "subscription_plans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxInquiriesPerMonth",
                schema: "subscription",
                table: "subscription_plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxSavedListings",
                schema: "subscription",
                table: "subscription_plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxSavedSearches",
                schema: "subscription",
                table: "subscription_plans",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InquiriesUsed",
                schema: "subscription",
                table: "subscription_usage");

            migrationBuilder.DropColumn(
                name: "SavedListingsUsed",
                schema: "subscription",
                table: "subscription_usage");

            migrationBuilder.DropColumn(
                name: "SavedSearchesUsed",
                schema: "subscription",
                table: "subscription_usage");

            migrationBuilder.DropColumn(
                name: "HasEarlyAccessListings",
                schema: "subscription",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "HasPriorityBrokerResponse",
                schema: "subscription",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "MaxInquiriesPerMonth",
                schema: "subscription",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "MaxSavedListings",
                schema: "subscription",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "MaxSavedSearches",
                schema: "subscription",
                table: "subscription_plans");
        }
    }
}
