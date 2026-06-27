using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maintenance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketRoutingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentTicketId",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoutedToCompanyId",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_tickets_RoutedToCompanyId",
                schema: "maintenance",
                table: "maintenance_tickets",
                column: "RoutedToCompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_maintenance_tickets_RoutedToCompanyId",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "ParentTicketId",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "RoutedToCompanyId",
                schema: "maintenance",
                table: "maintenance_tickets");
        }
    }
}
