using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantOwnerConfirmationAndRejection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OwnerConfirmedAt",
                schema: "document",
                table: "leases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "document",
                table: "leases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TenantConfirmedAt",
                schema: "document",
                table: "leases",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerConfirmedAt",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "TenantConfirmedAt",
                schema: "document",
                table: "leases");
        }
    }
}
