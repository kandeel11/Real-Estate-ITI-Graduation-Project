using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeasePaymentFrequencyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RentAmount",
                schema: "document",
                table: "leases",
                newName: "InstallmentAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalRentAmount",
                schema: "document",
                table: "leases",
                type: "decimal(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationInMonths",
                schema: "document",
                table: "leases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentFrequency",
                schema: "document",
                table: "leases",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalRentAmount",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "DurationInMonths",
                schema: "document",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "PaymentFrequency",
                schema: "document",
                table: "leases");

            migrationBuilder.RenameColumn(
                name: "InstallmentAmount",
                schema: "document",
                table: "leases",
                newName: "RentAmount");
        }
    }
}
