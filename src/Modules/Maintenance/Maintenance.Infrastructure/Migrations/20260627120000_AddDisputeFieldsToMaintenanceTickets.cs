using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maintenance.Infrastructure.Migrations
{
    public partial class AddDisputeFieldsToMaintenanceTickets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisputeDate",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisputeReason",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DisputedById",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisputedByRole",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNotes",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "DisputeDate",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "DisputeReason",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "DisputedById",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "DisputedByRole",
                schema: "maintenance",
                table: "maintenance_tickets");
        }
    }
}
