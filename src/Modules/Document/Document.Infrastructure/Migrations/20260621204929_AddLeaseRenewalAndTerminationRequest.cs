using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseRenewalAndTerminationRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RenewalRequestedAt",
                schema: "document",
                table: "leases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RenewedFromLeaseId",
                schema: "document",
                table: "leases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TerminationEffectiveDate",
                schema: "document",
                table: "leases",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TerminationRequestedAt",
                schema: "document",
                table: "leases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TerminationRequestedById",
                schema: "document",
                table: "leases",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RenewalRequestedAt",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "RenewedFromLeaseId",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "TerminationEffectiveDate",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "TerminationRequestedAt",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "TerminationRequestedById",
                schema: "document",
                table: "leases");
        }
    }
}
