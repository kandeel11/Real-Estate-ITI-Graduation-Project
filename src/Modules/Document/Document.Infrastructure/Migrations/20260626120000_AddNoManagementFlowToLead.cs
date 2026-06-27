using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNoManagementFlowToLead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NoManagementManagerId",
                schema: "document",
                table: "leads",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NoManagementPendingAt",
                schema: "document",
                table: "leads",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoManagementManagerId",
                schema: "document",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "NoManagementPendingAt",
                schema: "document",
                table: "leads");
        }
    }
}
