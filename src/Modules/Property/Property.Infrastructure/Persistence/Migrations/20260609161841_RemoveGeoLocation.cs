using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGeoLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                schema: "property",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "Longitude",
                schema: "property",
                table: "properties");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                schema: "property",
                table: "properties",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                schema: "property",
                table: "properties",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
