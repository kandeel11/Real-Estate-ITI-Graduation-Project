using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "property");

            migrationBuilder.CreateTable(
                name: "amenities",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amenities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "properties",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ListingBrokerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TitleAr = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DescriptionAr = table.Column<string>(type: "text", nullable: false),
                    PropertyType = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Bedrooms = table.Column<int>(type: "integer", nullable: false),
                    Bathrooms = table.Column<int>(type: "integer", nullable: false),
                    AreaSqm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    FloorNumber = table.Column<int>(type: "integer", nullable: false),
                    TotalFloors = table.Column<int>(type: "integer", nullable: false),
                    YearBuilt = table.Column<int>(type: "integer", nullable: false),
                    IsFurnished = table.Column<bool>(type: "boolean", nullable: false),
                    PetsAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    Governorate = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ApartmentNumber = table.Column<int>(type: "integer", nullable: true),
                    City = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    District = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerFullName = table.Column<string>(type: "text", nullable: false),
                    NationalIdBlobPath = table.Column<string>(type: "text", nullable: false),
                    OwnershipProofBlobPath = table.Column<string>(type: "text", nullable: false),
                    OwnerShipStatus = table.Column<string>(type: "text", nullable: false),
                    ListingSource = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Draft"),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FeaturedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AiDuplicateConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    VerificationStatus = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    OwnershipGate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    DuplicateGate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ViewsCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    InquiriesCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsFullManagement = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsMaintenanceEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsPaymentEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "saved_properties",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "property_amenities",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmenityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_amenities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_property_amenities_amenities_AmenityId",
                        column: x => x.AmenityId,
                        principalSchema: "property",
                        principalTable: "amenities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_property_amenities_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalSchema: "property",
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "property_analytics",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ViewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_analytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_property_analytics_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalSchema: "property",
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "property_media",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MediaType = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FileSizeKb = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_media", x => x.Id);
                    table.ForeignKey(
                        name: "FK_property_media_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalSchema: "property",
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_properties_City",
                schema: "property",
                table: "properties",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_properties_IsFeatured",
                schema: "property",
                table: "properties",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_properties_OwnerId",
                schema: "property",
                table: "properties",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_properties_Status",
                schema: "property",
                table: "properties",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_property_amenities_AmenityId",
                schema: "property",
                table: "property_amenities",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_property_amenities_PropertyId_AmenityId",
                schema: "property",
                table: "property_amenities",
                columns: new[] { "PropertyId", "AmenityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_property_analytics_PropertyId",
                schema: "property",
                table: "property_analytics",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_property_analytics_ViewedAt",
                schema: "property",
                table: "property_analytics",
                column: "ViewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_property_media_PropertyId",
                schema: "property",
                table: "property_media",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_properties_UserId",
                schema: "property",
                table: "saved_properties",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_properties_UserId_PropertyId",
                schema: "property",
                table: "saved_properties",
                columns: new[] { "UserId", "PropertyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "property_amenities",
                schema: "property");

            migrationBuilder.DropTable(
                name: "property_analytics",
                schema: "property");

            migrationBuilder.DropTable(
                name: "property_media",
                schema: "property");

            migrationBuilder.DropTable(
                name: "saved_properties",
                schema: "property");

            migrationBuilder.DropTable(
                name: "amenities",
                schema: "property");

            migrationBuilder.DropTable(
                name: "properties",
                schema: "property");
        }
    }
}
