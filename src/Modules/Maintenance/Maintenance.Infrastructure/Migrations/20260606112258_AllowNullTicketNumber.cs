using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maintenance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowNullTicketNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_maintenance_tickets_TicketNumber",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.AlterColumn<string>(
                name: "TicketNumber",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_tickets_TicketNumber",
                schema: "maintenance",
                table: "maintenance_tickets",
                column: "TicketNumber",
                unique: true,
                filter: "[TicketNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_maintenance_tickets_TicketNumber",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.AlterColumn<string>(
                name: "TicketNumber",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_tickets_TicketNumber",
                schema: "maintenance",
                table: "maintenance_tickets",
                column: "TicketNumber",
                unique: true);
        }
    }
}
