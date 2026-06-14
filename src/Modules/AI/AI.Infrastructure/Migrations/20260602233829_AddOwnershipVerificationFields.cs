using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnershipVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiOcrExtractedIdExpiry",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiOcrExtractedOwnerName",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiOcrExtractedIdExpiry",
                schema: "ai",
                table: "ownership_verification_requests");

            migrationBuilder.DropColumn(
                name: "AiOcrExtractedOwnerName",
                schema: "ai",
                table: "ownership_verification_requests");
        }
    }
}
