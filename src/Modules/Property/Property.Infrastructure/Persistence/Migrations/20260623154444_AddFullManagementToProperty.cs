using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFullManagementToProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFullManagement",
                schema: "property",
                table: "properties",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMaintenanceEnabled",
                schema: "property",
                table: "properties",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaymentEnabled",
                schema: "property",
                table: "properties",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFullManagement",
                schema: "property",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "IsMaintenanceEnabled",
                schema: "property",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "IsPaymentEnabled",
                schema: "property",
                table: "properties");
        }
    }
}
