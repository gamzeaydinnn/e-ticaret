using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentWebhookEventsAndCaptureFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AuthorizationExpiresAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorizationReference",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AuthorizedAmount",
                table: "Payments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "AuthorizedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaptureFailureReason",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CaptureStatus",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CapturedAmount",
                table: "Payments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CapturedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TolerancePercentage",
                table: "Payments",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.10m);

            migrationBuilder.AddColumn<decimal>(
                name: "AuthorizedAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CaptureStatus",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CapturedAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CapturedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CourierWeightAdjustment",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryProblemAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryProblemReason",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OutForDeliveryAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TolerancePercentage",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PaymentWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderEventId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PaymentIntentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Signature = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    SignatureValid = table.Column<bool>(type: "bit", nullable: false),
                    SourceIpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HttpHeaders = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentWebhookEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentWebhookEvents_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentWebhookEvents_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_OrderId",
                table: "PaymentWebhookEvents",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_PaymentId",
                table: "PaymentWebhookEvents",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_PaymentIntentId",
                table: "PaymentWebhookEvents",
                column: "PaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_ProcessingStatus",
                table: "PaymentWebhookEvents",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_Provider_Status",
                table: "PaymentWebhookEvents",
                columns: new[] { "Provider", "ProcessingStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_ReceivedAt",
                table: "PaymentWebhookEvents",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "UQ_PaymentWebhookEvents_Provider_EventId",
                table: "PaymentWebhookEvents",
                columns: new[] { "Provider", "ProviderEventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentWebhookEvents");

            migrationBuilder.DropColumn(
                name: "AuthorizationExpiresAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AuthorizationReference",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AuthorizedAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AuthorizedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CaptureFailureReason",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CaptureStatus",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CapturedAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CapturedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TolerancePercentage",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AuthorizedAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CaptureStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CapturedAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CapturedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CourierWeightAdjustment",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryProblemAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryProblemReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OutForDeliveryAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TolerancePercentage",
                table: "Orders");
        }
    }
}
