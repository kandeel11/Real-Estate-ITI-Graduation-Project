using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateAiMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ai");

            migrationBuilder.CreateTable(
                name: "admin_analytics_snapshots",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalUsers = table.Column<int>(type: "int", nullable: false),
                    TotalCompanies = table.Column<int>(type: "int", nullable: false),
                    TotalProperties = table.Column<int>(type: "int", nullable: false),
                    TotalActiveLeases = table.Column<int>(type: "int", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewSubscriptions = table.Column<int>(type: "int", nullable: false),
                    ChurnedSubscriptions = table.Column<int>(type: "int", nullable: false),
                    TotalAiRequests = table.Column<int>(type: "int", nullable: false),
                    MostActiveCities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MostPopularPlans = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MostActiveCompanies = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerificationStats = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_analytics_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_chatbot_sessions",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_chatbot_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_description_generations",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromptText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedTextEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedTextAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: false),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_description_generations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_price_estimations",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstimatedPriceMin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedPriceMax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ComparableProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_price_estimations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_search_queries",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RawQuery = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParsedFilters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultsCount = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_search_queries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_usage_logs",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: false),
                    RequestPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_usage_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "broker_performance_scores",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    ClosedDeals = table.Column<int>(type: "int", nullable: false),
                    AvgRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    AvgResponseTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    ConversionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TenantSatisfactionScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AiPerformanceScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AiScoreBreakdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_broker_performance_scores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "company_analytics",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalProperties = table.Column<int>(type: "int", nullable: false),
                    TotalInquiries = table.Column<int>(type: "int", nullable: false),
                    TotalLeads = table.Column<int>(type: "int", nullable: false),
                    ClosedDeals = table.Column<int>(type: "int", nullable: false),
                    ConversionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AvgResponseTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    AvgTenantSatisfaction = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    MostViewedProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MostRentedAreas = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AiGeneratedInsights = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_analytics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "duplicate_detection_logs",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewPropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SimilarPropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageSimilarityScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    LocationSimilarityScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DescriptionSimilarityScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    LayoutSimilarityScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OverallConfidence = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdminReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duplicate_detection_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ownership_verification_requests",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AiConfidenceScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    AiNameMatchScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    AiAuthenticityScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    AiFakeDetectionFlag = table.Column<bool>(type: "bit", nullable: false),
                    AiOcrExtractedName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiOcrExtractedId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AdminReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ownership_verification_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "saved_searches",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Filters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotifyOnMatch = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastMatchedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_searches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_chatbot_messages",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_chatbot_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_chatbot_messages_ai_chatbot_sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "ai",
                        principalTable: "ai_chatbot_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_chatbot_messages_SessionId",
                schema: "ai",
                table: "ai_chatbot_messages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_chatbot_sessions_SessionToken",
                schema: "ai",
                table: "ai_chatbot_sessions",
                column: "SessionToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_analytics_snapshots",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "ai_chatbot_messages",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "ai_description_generations",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "ai_price_estimations",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "ai_search_queries",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "ai_usage_logs",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "broker_performance_scores",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "company_analytics",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "duplicate_detection_logs",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "ownership_verification_requests",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "saved_searches",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "ai_chatbot_sessions",
                schema: "ai");
        }
    }
}
