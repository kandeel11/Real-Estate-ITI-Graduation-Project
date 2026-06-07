using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmailOtpAttempts",
                schema: "identity",
                table: "users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailOtpExpiresAt",
                schema: "identity",
                table: "users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailOtpHash",
                schema: "identity",
                table: "users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOtpRequestedAt",
                schema: "identity",
                table: "users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailOtpAttempts",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailOtpExpiresAt",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailOtpHash",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastOtpRequestedAt",
                schema: "identity",
                table: "users");
        }
    }
}
