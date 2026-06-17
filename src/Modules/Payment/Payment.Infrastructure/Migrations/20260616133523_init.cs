using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payment");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CheckoutSessionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CheckoutUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaymentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Rent"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LateFee = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 0m),
                    AiPredictedLate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReceiptUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rent_payment_details",
                schema: "payment",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LandlordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rent_payment_details", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_rent_payment_details_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "payment",
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscription_payment_details",
                schema: "payment",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingCycle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_payment_details", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_subscription_payment_details_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "payment",
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transfer_payment_details",
                schema: "payment",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfer_payment_details", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_transfer_payment_details_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "payment",
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rent_payments",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LateFee = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 0m),
                    AiPredictedLate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReceiptUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Rent"),
                    CheckoutSessionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CheckoutUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubscriptionDetailsPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rent_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rent_payments_subscription_payment_details_SubscriptionDetailsPaymentId",
                        column: x => x.SubscriptionDetailsPaymentId,
                        principalSchema: "payment",
                        principalTable: "subscription_payment_details",
                        principalColumn: "PaymentId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_unprocessed",
                schema: "payment",
                table: "outbox_messages",
                column: "ProcessedOn",
                filter: "[ProcessedOn] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payments_due_date",
                schema: "payment",
                table: "payments",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_payments_payment_type",
                schema: "payment",
                table: "payments",
                column: "PaymentType");

            migrationBuilder.CreateIndex(
                name: "IX_payments_status",
                schema: "payment",
                table: "payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_rent_payment_details_lease_id",
                schema: "payment",
                table: "rent_payment_details",
                column: "LeaseId");

            migrationBuilder.CreateIndex(
                name: "IX_rent_payment_details_lease_payment",
                schema: "payment",
                table: "rent_payment_details",
                columns: new[] { "LeaseId", "PaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_rent_payments_lease_duedate",
                schema: "payment",
                table: "rent_payments",
                columns: new[] { "LeaseId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_rent_payments_lease_id",
                schema: "payment",
                table: "rent_payments",
                column: "LeaseId");

            migrationBuilder.CreateIndex(
                name: "IX_rent_payments_payment_type",
                schema: "payment",
                table: "rent_payments",
                column: "PaymentType");

            migrationBuilder.CreateIndex(
                name: "IX_rent_payments_status",
                schema: "payment",
                table: "rent_payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_rent_payments_SubscriptionDetailsPaymentId",
                schema: "payment",
                table: "rent_payments",
                column: "SubscriptionDetailsPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_details_subscription_id",
                schema: "payment",
                table: "subscription_payment_details",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_payment_details_receiver_id",
                schema: "payment",
                table: "transfer_payment_details",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_payment_details_sender_id",
                schema: "payment",
                table: "transfer_payment_details",
                column: "SenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "rent_payment_details",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "rent_payments",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "transfer_payment_details",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "subscription_payment_details",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "payment");
        }
    }
}
