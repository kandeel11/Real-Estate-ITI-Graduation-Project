using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "leases",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InquiryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RentAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    SecurityDeposit = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaymentDayOfMonth = table.Column<int>(type: "int", nullable: true),
                    ContractFileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantSignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OwnerSignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtractionStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExtractionConfidence = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ConfirmedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TerminatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TerminationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leases_inquiries_InquiryId",
                        column: x => x.InquiryId,
                        principalSchema: "document",
                        principalTable: "inquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_leases_leads_LeadId",
                        column: x => x.LeadId,
                        principalSchema: "document",
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_leases_InquiryId",
                schema: "document",
                table: "leases",
                column: "InquiryId");

            migrationBuilder.CreateIndex(
                name: "IX_leases_LeadId",
                schema: "document",
                table: "leases",
                column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leases",
                schema: "document");
        }
    }
}
