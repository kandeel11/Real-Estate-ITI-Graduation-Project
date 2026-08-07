using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitPostgres : Migration
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalUsers = table.Column<int>(type: "integer", nullable: false),
                    TotalCompanies = table.Column<int>(type: "integer", nullable: false),
                    TotalProperties = table.Column<int>(type: "integer", nullable: false),
                    TotalActiveLeases = table.Column<int>(type: "integer", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NewSubscriptions = table.Column<int>(type: "integer", nullable: false),
                    ChurnedSubscriptions = table.Column<int>(type: "integer", nullable: false),
                    TotalAiRequests = table.Column<int>(type: "integer", nullable: false),
                    MostActiveCities = table.Column<string>(type: "text", nullable: false),
                    MostPopularPlans = table.Column<string>(type: "text", nullable: false),
                    MostActiveCompanies = table.Column<string>(type: "text", nullable: false),
                    VerificationStats = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_analytics_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_description_generations",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptText = table.Column<string>(type: "text", nullable: false),
                    GeneratedTextEn = table.Column<string>(type: "text", nullable: false),
                    GeneratedTextAr = table.Column<string>(type: "text", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    IsAccepted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimatedPriceMin = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedPriceMax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ComparableProperties = table.Column<string>(type: "text", nullable: false),
                    ModelVersion = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RawQuery = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ParsedFilters = table.Column<string>(type: "text", nullable: false),
                    ResultCount = table.Column<int>(type: "integer", nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    ParsingConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    RequestPayload = table.Column<string>(type: "text", nullable: false),
                    ResponseSummary = table.Column<string>(type: "text", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_usage_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiChatbotSessions",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiChatbotSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "broker_performance_scores",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    ClosedDeals = table.Column<int>(type: "integer", nullable: false),
                    AvgRating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    AvgResponseTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    ConversionRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TenantSatisfactionScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AiPerformanceScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AiScoreBreakdown = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_broker_performance_scores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatbotSessions",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "company_analytics",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalProperties = table.Column<int>(type: "integer", nullable: false),
                    TotalInquiries = table.Column<int>(type: "integer", nullable: false),
                    TotalLeads = table.Column<int>(type: "integer", nullable: false),
                    ClosedDeals = table.Column<int>(type: "integer", nullable: false),
                    ConversionRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AvgResponseTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    AvgTenantSatisfaction = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    MostViewedProperties = table.Column<string>(type: "text", nullable: false),
                    MostRentedAreas = table.Column<string>(type: "text", nullable: false),
                    AiGeneratedInsights = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NewPropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SimilarPropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageSimilarityScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    LocationSimilarityScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DescriptionSimilarityScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    LayoutSimilarityScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    OverallConfidence = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AdminReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duplicate_detection_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaseExtractionRequests",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    OverallConfidence = table.Column<decimal>(type: "numeric", nullable: true),
                    RawAiResponse = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaseExtractionRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ownership_verification_requests",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    AiConfidenceScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    AiNameMatchScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    AiAuthenticityScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    AiFakeDetectionFlag = table.Column<bool>(type: "boolean", nullable: false),
                    AiOcrExtractedName = table.Column<string>(type: "text", nullable: true),
                    AiOcrExtractedId = table.Column<string>(type: "text", nullable: true),
                    ExtractedCountry = table.Column<string>(type: "text", nullable: true),
                    ExtractedGovernorate = table.Column<string>(type: "text", nullable: true),
                    ExtractedCity = table.Column<string>(type: "text", nullable: true),
                    ExtractedDistrict = table.Column<string>(type: "text", nullable: true),
                    ExtractedAddress = table.Column<string>(type: "text", nullable: true),
                    LocationMatchScore = table.Column<decimal>(type: "numeric", nullable: true),
                    LocationMismatchFlag = table.Column<bool>(type: "boolean", nullable: true),
                    LocationMatchNotes = table.Column<string>(type: "text", nullable: true),
                    AiOcrExtractedIdExpiry = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AiOcrExtractedOwnerName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AdminReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdminReviewNotes = table.Column<string>(type: "text", nullable: true),
                    AdminReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OwnerFullName = table.Column<string>(type: "text", nullable: false),
                    NationalIdBlobPath = table.Column<string>(type: "text", nullable: false),
                    OwnershipProofBlobPath = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RawQuery = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ParsedFilters = table.Column<string>(type: "text", nullable: false),
                    NotifyOnMatch = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_searches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiChatbotMessages",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiChatbotMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiChatbotMessages_AiChatbotSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "ai",
                        principalTable: "AiChatbotSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatbotMessages",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatbotMessages_ChatbotSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "ai",
                        principalTable: "ChatbotSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_search_queries_date",
                schema: "ai",
                table: "ai_search_queries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_ai_search_queries_user",
                schema: "ai",
                table: "ai_search_queries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiChatbotMessages_SessionId",
                schema: "ai",
                table: "AiChatbotMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AiChatbotSessions_SessionToken",
                schema: "ai",
                table: "AiChatbotSessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiChatbotSessions_UserId",
                schema: "ai",
                table: "AiChatbotSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotMessages_SessionId",
                schema: "ai",
                table: "ChatbotMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "ix_saved_searches_active_notify",
                schema: "ai",
                table: "saved_searches",
                columns: new[] { "IsActive", "NotifyOnMatch" });

            migrationBuilder.CreateIndex(
                name: "ix_saved_searches_user",
                schema: "ai",
                table: "saved_searches",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_analytics_snapshots",
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
                name: "AiChatbotMessages",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "broker_performance_scores",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "ChatbotMessages",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "company_analytics",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "duplicate_detection_logs",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "LeaseExtractionRequests",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "ownership_verification_requests",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "saved_searches",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "AiChatbotSessions",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "ChatbotSessions",
                schema: "ai");
        }
    }
}
