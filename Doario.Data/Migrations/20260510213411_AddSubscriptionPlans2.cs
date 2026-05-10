using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlans2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocsPerStaff",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "ExtraDocumentPrice",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "IncludedDocuments",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "MonthlyPrice",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "PlanName",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "PricePerStaff",
                table: "TenantSubscriptions");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubscriptionPlanId",
                table: "TenantSubscriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StripeSubscriptionId",
                table: "TenantSubscriptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "StripePlanId",
                table: "TenantSubscriptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SubscriptionPlans",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "SubscriptionPlanId",
                keyValue: new Guid("b1000000-0001-0001-0001-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 10, 21, 34, 11, 201, DateTimeKind.Utc).AddTicks(9105));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "SubscriptionPlanId",
                keyValue: new Guid("b1000000-0002-0002-0002-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 10, 21, 34, 11, 202, DateTimeKind.Utc).AddTicks(759));

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "SubscriptionPlanId",
                keyValue: new Guid("b1000000-0003-0003-0003-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 10, 21, 34, 11, 202, DateTimeKind.Utc).AddTicks(768));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SubscriptionPlans");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubscriptionPlanId",
                table: "TenantSubscriptions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "StripeSubscriptionId",
                table: "TenantSubscriptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StripePlanId",
                table: "TenantSubscriptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocsPerStaff",
                table: "TenantSubscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraDocumentPrice",
                table: "TenantSubscriptions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "IncludedDocuments",
                table: "TenantSubscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPrice",
                table: "TenantSubscriptions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PlanName",
                table: "TenantSubscriptions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerStaff",
                table: "TenantSubscriptions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
