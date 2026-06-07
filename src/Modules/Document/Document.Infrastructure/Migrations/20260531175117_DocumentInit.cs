using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentInit : Migration
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AcceptedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaintenanceCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RatingValue = table.Column<int>(type: "int", nullable: false),
                    ReviewText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModerationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModeratedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModeratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerificationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSizeKb = table.Column<int>(type: "int", nullable: false),
                    AiProcessed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AiProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InquiryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrokerOrOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InquiryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InquiryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DealValue = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "leads",
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
                name: "conversations",
                schema: "document");

            migrationBuilder.DropTable(
                name: "inquiries",
                schema: "document");
        }
    }
}
