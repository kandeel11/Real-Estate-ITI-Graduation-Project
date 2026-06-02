using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maintenance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMaintenance : Migration
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
                    MaintenanceCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Medium"),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValue: "Open"),
                    AiPredictedSla = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maintenance_ticket_attachments",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "maintenance_ticket_comments",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "maintenance_tickets",
                schema: "maintenance");
        }
    }
}
