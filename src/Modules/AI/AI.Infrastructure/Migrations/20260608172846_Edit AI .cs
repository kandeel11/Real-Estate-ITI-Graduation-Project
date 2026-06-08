using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EditAI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedAddress",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedCity",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedCountry",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedDistrict",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedGovernorate",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationMatchNotes",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LocationMatchScore",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LocationMismatchFlag",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalIdBlobPath",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerFullName",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnershipProofBlobPath",
                schema: "ai",
                table: "ownership_verification_requests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                schema: "ai",
                table: "ownership_verification_requests");

            migrationBuilder.DropColumn(
                name: "ExtractedAddress",
                schema: "ai",
                table: "ownership_verification_requests");

            migrationBuilder.DropColumn(
                name: "ExtractedCity",
                schema: "ai",
                table: "ownership_verification_requests");

            migrationBuilder.DropColumn(
                name: "ExtractedCountry",
                schema: "ai",
                table: "ownership_verification_requests");

            migrationBuilder.DropColumn(
                name: "ExtractedDistrict",
                schema: "ai",
                table: "ownership_verification_requests");

            migrationBuilder.DropColumn(
                name: "ExtractedGovernorate",
                schema: "ai",
                table: "ownership_verification_requests");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                schema: "ai",
                table: "ownership_verification_requests");

            migrationBuilder.DropColumn(
                name: "LocationMatchNotes",
                schema: "ai",
                table: "ownership_verification_requests");

            migrationBuilder.DropColumn(
                name: "LocationMatchScore",
                schema: "ai",
                table: "ownership_verification_requests");

            migrationBuilder.DropColumn(
                name: "LocationMismatchFlag",
                schema: "ai",
                table: "ownership_verification_requests");

            migrationBuilder.DropColumn(
                name: "NationalIdBlobPath",
                schema: "ai",
                table: "ownership_verification_requests");

            migrationBuilder.DropColumn(
                name: "OwnerFullName",
                schema: "ai",
                table: "ownership_verification_requests");

            migrationBuilder.DropColumn(
                name: "OwnershipProofBlobPath",
                schema: "ai",
                table: "ownership_verification_requests");
        }
    }
}
