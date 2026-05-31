using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentExtractionResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentExtractionResults",
                columns: table => new
                {
                    DocumentExtractionResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FieldValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PageNumber = table.Column<int>(type: "int", nullable: true),
                    BoundingBox = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: true),
                    CorrectedValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExtractedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentExtractionResults", x => x.DocumentExtractionResultId);
                    table.ForeignKey(
                        name: "FK_DocumentExtractionResults_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentExtractionResults_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "SubscriptionPlanId",
                keyValue: new Guid("b1000000-0001-0001-0001-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 14, 19, 51, 16, 955, DateTimeKind.Utc).AddTicks(8400));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "SubscriptionPlanId",
                keyValue: new Guid("b1000000-0002-0002-0002-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 14, 19, 51, 16, 956, DateTimeKind.Utc).AddTicks(922));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "SubscriptionPlanId",
                keyValue: new Guid("b1000000-0003-0003-0003-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 14, 19, 51, 16, 956, DateTimeKind.Utc).AddTicks(941));

            migrationBuilder.CreateIndex(
                name: "IX_DocumentExtractionResults_DocumentId",
                table: "DocumentExtractionResults",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentExtractionResults_TenantId",
                table: "DocumentExtractionResults",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentExtractionResults");

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "SubscriptionPlanId",
                keyValue: new Guid("b1000000-0001-0001-0001-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 12, 20, 49, 40, 100, DateTimeKind.Utc).AddTicks(4198));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "SubscriptionPlanId",
                keyValue: new Guid("b1000000-0002-0002-0002-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 12, 20, 49, 40, 100, DateTimeKind.Utc).AddTicks(5949));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "SubscriptionPlanId",
                keyValue: new Guid("b1000000-0003-0003-0003-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 12, 20, 49, 40, 100, DateTimeKind.Utc).AddTicks(5959));
        }
    }
}
