using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaintenanceCompanies.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "maintenanceComp");

            migrationBuilder.CreateTable(
                name: "MaintenanceCategories",
                schema: "maintenanceComp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceCompanies",
                schema: "maintenanceComp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceTypes = table.Column<string>(type: "text", nullable: false),
                    ServiceRadiusKm = table.Column<int>(type: "integer", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: false),
                    CompletedTicketsCount = table.Column<int>(type: "integer", nullable: false),
                    ActiveTicketsCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedByAdminId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceCompanies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyCategories",
                schema: "maintenanceComp",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyCategories", x => new { x.CompanyId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_CompanyCategories_MaintenanceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "maintenanceComp",
                        principalTable: "MaintenanceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyCategories_MaintenanceCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "maintenanceComp",
                        principalTable: "MaintenanceCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyReviews",
                schema: "maintenanceComp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    CompanyReply = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RepliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyReviews_MaintenanceCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "maintenanceComp",
                        principalTable: "MaintenanceCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyWorkingHours",
                schema: "maintenanceComp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    CloseTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyWorkingHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyWorkingHours_MaintenanceCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "maintenanceComp",
                        principalTable: "MaintenanceCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCategories_CategoryId",
                schema: "maintenanceComp",
                table: "CompanyCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyReviews_CompanyId",
                schema: "maintenanceComp",
                table: "CompanyReviews",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyReviews_TicketId",
                schema: "maintenanceComp",
                table: "CompanyReviews",
                column: "TicketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyWorkingHours_CompanyId_Day",
                schema: "maintenanceComp",
                table: "CompanyWorkingHours",
                columns: new[] { "CompanyId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceCategories_Name",
                schema: "maintenanceComp",
                table: "MaintenanceCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceCompanies_Email",
                schema: "maintenanceComp",
                table: "MaintenanceCompanies",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceCompanies_UserId",
                schema: "maintenanceComp",
                table: "MaintenanceCompanies",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyCategories",
                schema: "maintenanceComp");

            migrationBuilder.DropTable(
                name: "CompanyReviews",
                schema: "maintenanceComp");

            migrationBuilder.DropTable(
                name: "CompanyWorkingHours",
                schema: "maintenanceComp");

            migrationBuilder.DropTable(
                name: "MaintenanceCategories",
                schema: "maintenanceComp");

            migrationBuilder.DropTable(
                name: "MaintenanceCompanies",
                schema: "maintenanceComp");
        }
    }
}
