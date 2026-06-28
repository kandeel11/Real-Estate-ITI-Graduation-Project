using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maintenance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutingAndCheckoutToMaintenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── maintenance_tickets: routing columns ──────────────────────────
            migrationBuilder.AddColumn<Guid>(
                name: "RoutedToCompanyId",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentTicketId",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_tickets_RoutedToCompanyId",
                schema: "maintenance",
                table: "maintenance_tickets",
                column: "RoutedToCompanyId");

            // ── maintenance_payments: Stripe Checkout columns ─────────────────
            migrationBuilder.AddColumn<string>(
                name: "CheckoutSessionId",
                schema: "maintenance",
                table: "maintenance_payments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutUrl",
                schema: "maintenance",
                table: "maintenance_payments",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_maintenance_tickets_RoutedToCompanyId",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "RoutedToCompanyId",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "ParentTicketId",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "CheckoutSessionId",
                schema: "maintenance",
                table: "maintenance_payments");

            migrationBuilder.DropColumn(
                name: "CheckoutUrl",
                schema: "maintenance",
                table: "maintenance_payments");
        }
    }
}
