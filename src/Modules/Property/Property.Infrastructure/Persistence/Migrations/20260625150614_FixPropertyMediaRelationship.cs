using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPropertyMediaRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_property_media_properties_PropertyId1",
                schema: "property",
                table: "property_media");

            migrationBuilder.DropIndex(
                name: "IX_property_media_PropertyId1",
                schema: "property",
                table: "property_media");

            migrationBuilder.DropColumn(
                name: "PropertyId1",
                schema: "property",
                table: "property_media");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PropertyId1",
                schema: "property",
                table: "property_media",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_property_media_PropertyId1",
                schema: "property",
                table: "property_media",
                column: "PropertyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_property_media_properties_PropertyId1",
                schema: "property",
                table: "property_media",
                column: "PropertyId1",
                principalSchema: "property",
                principalTable: "properties",
                principalColumn: "Id");
        }
    }
}
