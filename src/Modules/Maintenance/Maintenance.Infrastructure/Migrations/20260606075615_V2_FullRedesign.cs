using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maintenance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2_FullRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiPredictedSla",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "Draft",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldDefaultValue: "Open");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "PropertyAddress",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionMethod",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleParty",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantEmail",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantPhone",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TicketNumber",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserRole",
                schema: "maintenance",
                table: "maintenance_ticket_comments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "maintenance_payments",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "SAR"),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Pending"),
                    PaymentMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_payments_maintenance_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "maintenance",
                        principalTable: "maintenance_tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_ticket_ai_evaluations",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibleParty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: false),
                    Reasoning = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RawRequestJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_ticket_ai_evaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_ticket_ai_evaluations_maintenance_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "maintenance",
                        principalTable: "maintenance_tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_ticket_status_history",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_ticket_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_ticket_status_history_maintenance_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "maintenance",
                        principalTable: "maintenance_tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_ticket_vendor_assignments",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Pending"),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_ticket_vendor_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_ticket_vendor_assignments_maintenance_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "maintenance",
                        principalTable: "maintenance_tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_tickets_OwnerId",
                schema: "maintenance",
                table: "maintenance_tickets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_tickets_TicketNumber",
                schema: "maintenance",
                table: "maintenance_tickets",
                column: "TicketNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_payments_TicketId",
                schema: "maintenance",
                table: "maintenance_payments",
                column: "TicketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_ticket_ai_evaluations_TicketId",
                schema: "maintenance",
                table: "maintenance_ticket_ai_evaluations",
                column: "TicketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_ticket_status_history_TicketId",
                schema: "maintenance",
                table: "maintenance_ticket_status_history",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_ticket_vendor_assignments_TicketId",
                schema: "maintenance",
                table: "maintenance_ticket_vendor_assignments",
                column: "TicketId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maintenance_payments",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "maintenance_ticket_ai_evaluations",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "maintenance_ticket_status_history",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "maintenance_ticket_vendor_assignments",
                schema: "maintenance");

            migrationBuilder.DropIndex(
                name: "IX_maintenance_tickets_OwnerId",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropIndex(
                name: "IX_maintenance_tickets_TicketNumber",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "PropertyAddress",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "ResolutionMethod",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "ResponsibleParty",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "TenantEmail",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "TenantPhone",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "TicketNumber",
                schema: "maintenance",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "UserRole",
                schema: "maintenance",
                table: "maintenance_ticket_comments");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "Open",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldDefaultValue: "Draft");

            migrationBuilder.AddColumn<string>(
                name: "AiPredictedSla",
                schema: "maintenance",
                table: "maintenance_tickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
