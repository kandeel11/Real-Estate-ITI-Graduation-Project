using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "document");

            migrationBuilder.CreateTable(
                name: "inquiries",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AcceptedById = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inquiries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ratings",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaintenanceCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    RatingValue = table.Column<int>(type: "integer", nullable: false),
                    ReviewText = table.Column<string>(type: "text", nullable: true),
                    ModerationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModeratedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ModeratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ratings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "verification_documents",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSizeKb = table.Column<int>(type: "integer", nullable: false),
                    AiProcessed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AiProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "conversations",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InquiryId = table.Column<Guid>(type: "uuid", nullable: true),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerOrOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversations_inquiries_InquiryId",
                        column: x => x.InquiryId,
                        principalSchema: "document",
                        principalTable: "inquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "inquiry_assignments",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InquiryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedById = table.Column<Guid>(type: "uuid", nullable: false),
                    IsAccepted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inquiry_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inquiry_assignments_inquiries_InquiryId",
                        column: x => x.InquiryId,
                        principalSchema: "document",
                        principalTable: "inquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InquiryId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DealValue = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NoManagementPendingAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NoManagementManagerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leads_inquiries_InquiryId",
                        column: x => x.InquiryId,
                        principalSchema: "document",
                        principalTable: "inquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_messages_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "document",
                        principalTable: "conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leases",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: true),
                    InquiryId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstallmentAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    PaymentFrequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TotalRentAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    DurationInMonths = table.Column<int>(type: "integer", nullable: true),
                    SecurityDeposit = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaymentDayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    ContractFileUrl = table.Column<string>(type: "text", nullable: false),
                    TenantSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OwnerSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExtractionStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExtractionConfidence = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ConfirmedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TerminatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminationReason = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OwnerConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    RenewedFromLeaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RenewalRequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminationRequestedById = table.Column<Guid>(type: "uuid", nullable: true),
                    TerminationRequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminationEffectiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsFullManagement = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ManagementOptions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ManagerApprovalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManagerApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ManagerRejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ManagerNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
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
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_leases_leads_LeadId",
                        column: x => x.LeadId,
                        principalSchema: "document",
                        principalTable: "leads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_conversations_InquiryId",
                schema: "document",
                table: "conversations",
                column: "InquiryId");

            migrationBuilder.CreateIndex(
                name: "IX_inquiries_PropertyId",
                schema: "document",
                table: "inquiries",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_inquiries_Status",
                schema: "document",
                table: "inquiries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_inquiries_TenantId",
                schema: "document",
                table: "inquiries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inquiry_assignments_InquiryId",
                schema: "document",
                table: "inquiry_assignments",
                column: "InquiryId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_InquiryId",
                schema: "document",
                table: "leads",
                column: "InquiryId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_Status",
                schema: "document",
                table: "leads",
                column: "Status");

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

            migrationBuilder.CreateIndex(
                name: "IX_messages_ConversationId",
                schema: "document",
                table: "messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_IsRead",
                schema: "document",
                table: "notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_ratings_ModerationStatus",
                schema: "document",
                table: "ratings",
                column: "ModerationStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inquiry_assignments",
                schema: "document");

            migrationBuilder.DropTable(
                name: "leases",
                schema: "document");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "document");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "document");

            migrationBuilder.DropTable(
                name: "ratings",
                schema: "document");

            migrationBuilder.DropTable(
                name: "verification_documents",
                schema: "document");

            migrationBuilder.DropTable(
                name: "leads",
                schema: "document");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "document");

            migrationBuilder.DropTable(
                name: "inquiries",
                schema: "document");
        }
    }
}
