using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationGates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DuplicateGate",
                schema: "property",
                table: "properties",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "OwnershipGate",
                schema: "property",
                table: "properties",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuplicateGate",
                schema: "property",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "OwnershipGate",
                schema: "property",
                table: "properties");
        }
    }
}
