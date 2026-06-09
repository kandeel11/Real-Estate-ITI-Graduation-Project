using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EditProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "property",
                table: "properties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Governorate",
                schema: "property",
                table: "properties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NationalIdBlobPath",
                schema: "property",
                table: "properties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerFullName",
                schema: "property",
                table: "properties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerShipStatus",
                schema: "property",
                table: "properties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnershipProofBlobPath",
                schema: "property",
                table: "properties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                schema: "property",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "Governorate",
                schema: "property",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "NationalIdBlobPath",
                schema: "property",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "OwnerFullName",
                schema: "property",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "OwnerShipStatus",
                schema: "property",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "OwnershipProofBlobPath",
                schema: "property",
                table: "properties");
        }
    }
}
