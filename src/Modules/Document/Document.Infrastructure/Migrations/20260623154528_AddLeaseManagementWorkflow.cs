using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseManagementWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFullManagement",
                schema: "document",
                table: "leases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ManagementOptions",
                schema: "document",
                table: "leases",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerApprovalStatus",
                schema: "document",
                table: "leases",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManagerApprovedAt",
                schema: "document",
                table: "leases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerId",
                schema: "document",
                table: "leases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerNotes",
                schema: "document",
                table: "leases",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManagerRejectedAt",
                schema: "document",
                table: "leases",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFullManagement",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "ManagementOptions",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "ManagerApprovalStatus",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "ManagerApprovedAt",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "ManagerNotes",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "ManagerRejectedAt",
                schema: "document",
                table: "leases");
        }
    }
}
