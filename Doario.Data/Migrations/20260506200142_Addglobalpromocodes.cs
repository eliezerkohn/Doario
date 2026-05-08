using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class Addglobalpromocodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "TenantPromo");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "TenantPromo");

            migrationBuilder.DropColumn(
                name: "FlatDiscountPerDoc",
                table: "TenantPromo");

            migrationBuilder.DropColumn(
                name: "FreeDocCount",
                table: "TenantPromo");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "TenantPromo");

            migrationBuilder.DropColumn(
                name: "PromoCode",
                table: "TenantPromo");

            migrationBuilder.DropColumn(
                name: "StartsAt",
                table: "TenantPromo");

            migrationBuilder.AddColumn<Guid>(
                name: "PromoCodeId",
                table: "TenantPromo",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "PromoCodes",
                columns: table => new
                {
                    PromoCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FlatDiscountPerDoc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FreeDocCount = table.Column<int>(type: "int", nullable: false),
                    MaxRedemptions = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodes", x => x.PromoCodeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantPromo_PromoCodeId",
                table: "TenantPromo",
                column: "PromoCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TenantPromo_PromoCodes_PromoCodeId",
                table: "TenantPromo",
                column: "PromoCodeId",
                principalTable: "PromoCodes",
                principalColumn: "PromoCodeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantPromo_PromoCodes_PromoCodeId",
                table: "TenantPromo");

            migrationBuilder.DropTable(
                name: "PromoCodes");

            migrationBuilder.DropIndex(
                name: "IX_TenantPromo_PromoCodeId",
                table: "TenantPromo");

            migrationBuilder.DropColumn(
                name: "PromoCodeId",
                table: "TenantPromo");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "TenantPromo",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "TenantPromo",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "FlatDiscountPerDoc",
                table: "TenantPromo",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "FreeDocCount",
                table: "TenantPromo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "TenantPromo",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromoCode",
                table: "TenantPromo",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAt",
                table: "TenantPromo",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
