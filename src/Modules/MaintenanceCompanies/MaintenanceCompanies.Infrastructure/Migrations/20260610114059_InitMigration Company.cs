using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaintenanceCompanies.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitMigrationCompany : Migration
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceTypes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServiceRadiusKm = table.Column<int>(type: "int", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AverageRating = table.Column<double>(type: "float", nullable: false),
                    CompletedTicketsCount = table.Column<int>(type: "int", nullable: false),
                    ActiveTicketsCount = table.Column<int>(type: "int", nullable: false),
                    CreatedByAdminId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyReply = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RepliedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Day = table.Column<int>(type: "int", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false)
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
