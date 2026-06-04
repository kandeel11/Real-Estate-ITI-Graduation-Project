using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EditTableDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_leases_leads_LeadId",
                schema: "document",
                table: "leases");

            migrationBuilder.AddForeignKey(
                name: "FK_leases_leads_LeadId",
                schema: "document",
                table: "leases",
                column: "LeadId",
                principalSchema: "document",
                principalTable: "leads",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_leases_leads_LeadId",
                schema: "document",
                table: "leases");

            migrationBuilder.AddForeignKey(
                name: "FK_leases_leads_LeadId",
                schema: "document",
                table: "leases",
                column: "LeadId",
                principalSchema: "document",
                principalTable: "leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
