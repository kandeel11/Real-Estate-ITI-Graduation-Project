using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maintenance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitPostgres : Migration
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoutedToCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentTicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    TicketNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Draft"),
                    PropertyAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TenantPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ResponsibleParty = table.Column<string>(type: "text", nullable: true),
                    ResolutionMethod = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisputeReason = table.Column<string>(type: "text", nullable: true),
                    DisputedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DisputedByRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DisputeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "SAR"),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    PaymentMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TransactionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CommissionAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    NetAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    CheckoutSessionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CheckoutUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponsibleParty = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ConfidenceScore = table.Column<int>(type: "integer", nullable: false),
                    Reasoning = table.Column<string>(type: "text", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RawRequestJson = table.Column<string>(type: "text", nullable: true),
                    RawResponseJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_ticket_ai_evaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_ticket_ai_evaluations_maintenance_tickets_Ticke~",
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeKb = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRole = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "text", nullable: false),
                    ToStatus = table.Column<string>(type: "text", nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_ticket_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_ticket_status_history_maintenance_tickets_Ticke~",
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_ticket_vendor_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_ticket_vendor_assignments_maintenance_tickets_T~",
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
                name: "IX_maintenance_tickets_RoutedToCompanyId",
                schema: "maintenance",
                table: "maintenance_tickets",
                column: "RoutedToCompanyId");

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
