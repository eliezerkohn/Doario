using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ExtraDocumentPrice",
                table: "TenantSubscriptions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionPlanId",
                table: "TenantSubscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IncludedDocuments = table.Column<int>(type: "int", nullable: false),
                    ExtraDocumentPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StripePriceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.SubscriptionPlanId);
                });

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "SubscriptionPlanId", "Description", "EndDate", "ExtraDocumentPrice", "IncludedDocuments", "IsActive", "IsPublic", "MonthlyPrice", "Name", "SortOrder", "StartDate", "StripePriceId" },
                values: new object[,]
                {
                    { new Guid("b1000000-0001-0001-0001-000000000001"), "Perfect for small offices with low mail volume.", new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 1.00m, 50, true, true, 49.00m, "Starter", 100, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "" },
                    { new Guid("b1000000-0002-0002-0002-000000000002"), "For growing teams handling moderate mail volume.", new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 0.70m, 300, true, true, 129.00m, "Growth", 200, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "" },
                    { new Guid("b1000000-0003-0003-0003-000000000003"), "For high-volume mail rooms processing hundreds of documents.", new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 0.50m, 600, true, true, 249.00m, "Business", 300, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "" }
                });

            migrationBuilder.InsertData(
                table: "SystemStatuses",
                columns: new[] { "SystemStatusId", "Description", "EndDate", "Name", "SortOrder", "StartDate" },
                values: new object[] { 9, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "PermanentFail", 900, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_SubscriptionPlanId",
                table: "TenantSubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_TenantSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                table: "TenantSubscriptions",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "SubscriptionPlanId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                table: "TenantSubscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_TenantSubscriptions_SubscriptionPlanId",
                table: "TenantSubscriptions");

            migrationBuilder.DeleteData(
                table: "SystemStatuses",
                keyColumn: "SystemStatusId",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                table: "TenantSubscriptions");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExtraDocumentPrice",
                table: "TenantSubscriptions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);
        }
    }
}
