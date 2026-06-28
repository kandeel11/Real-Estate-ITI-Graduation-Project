using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maintenance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "maintenance");

            migrationBuilder.CreateTable(
                name: "maintenance_tickets",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TicketNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValue: "Draft"),
                    PropertyAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TenantPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResponsibleParty = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolutionMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_tickets", x => x.Id);
                });

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
                name: "maintenance_ticket_attachments",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeKb = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_ticket_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_ticket_attachments_maintenance_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "maintenance",
                        principalTable: "maintenance_tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_ticket_comments",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsInternal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_ticket_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_ticket_comments_maintenance_tickets_TicketId",
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
                name: "IX_maintenance_ticket_attachments_TicketId",
                schema: "maintenance",
                table: "maintenance_ticket_attachments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_ticket_comments_TicketId",
                schema: "maintenance",
                table: "maintenance_ticket_comments",
                column: "TicketId");

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

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_tickets_OwnerId",
                schema: "maintenance",
                table: "maintenance_tickets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_tickets_PropertyId",
                schema: "maintenance",
                table: "maintenance_tickets",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_tickets_Status",
                schema: "maintenance",
                table: "maintenance_tickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_tickets_TenantId",
                schema: "maintenance",
                table: "maintenance_tickets",
                column: "TenantId");

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
            migrationBuilder.DropTable(
                name: "maintenance_payments",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "maintenance_ticket_ai_evaluations",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "maintenance_ticket_attachments",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "maintenance_ticket_comments",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "maintenance_ticket_status_history",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "maintenance_ticket_vendor_assignments",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "maintenance_tickets",
                schema: "maintenance");
        }
    }
}
