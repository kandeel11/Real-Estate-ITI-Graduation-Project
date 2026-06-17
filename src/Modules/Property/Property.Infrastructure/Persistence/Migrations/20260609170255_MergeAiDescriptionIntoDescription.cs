using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MergeAiDescriptionIntoDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiGeneratedDescriptionAr",
                schema: "property",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "AiGeneratedDescriptionEn",
                schema: "property",
                table: "properties");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiGeneratedDescriptionAr",
                schema: "property",
                table: "properties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiGeneratedDescriptionEn",
                schema: "property",
                table: "properties",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
