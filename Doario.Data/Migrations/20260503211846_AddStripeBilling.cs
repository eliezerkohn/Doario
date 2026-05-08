using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionItemId",
                table: "TenantSubscriptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Tenants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantBillingUsage",
                columns: table => new
                {
                    TenantBillingUsageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportedToStripe = table.Column<bool>(type: "bit", nullable: false),
                    StripeUsageRecordId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantBillingUsage", x => x.TenantBillingUsageId);
                    table.ForeignKey(
                        name: "FK_TenantBillingUsage_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantBillingUsage_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantPromo",
                columns: table => new
                {
                    TenantPromoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromoCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FlatDiscountPerDoc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FreeDocCount = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantPromo", x => x.TenantPromoId);
                    table.ForeignKey(
                        name: "FK_TenantPromo_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantBillingUsage_DocumentId",
                table: "TenantBillingUsage",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantBillingUsage_TenantId",
                table: "TenantBillingUsage",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPromo_TenantId",
                table: "TenantPromo",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantBillingUsage");

            migrationBuilder.DropTable(
                name: "TenantPromo");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionItemId",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Tenants");
        }
    }
}
