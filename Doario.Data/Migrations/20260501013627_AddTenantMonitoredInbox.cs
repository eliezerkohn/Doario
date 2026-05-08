using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantMonitoredInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaxSenderAddress",
                table: "TenantInboxSettings");

            migrationBuilder.DropColumn(
                name: "LastEmailProcessedAt",
                table: "TenantInboxSettings");

            migrationBuilder.CreateTable(
                name: "TenantMonitoredInboxes",
                columns: table => new
                {
                    TenantMonitoredInboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    IsFaxInbox = table.Column<bool>(type: "bit", nullable: false),
                    PollingIntervalSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    LastProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValue: new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999)),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMonitoredInboxes", x => x.TenantMonitoredInboxId);
                    table.ForeignKey(
                        name: "FK_TenantMonitoredInboxes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantMonitoredInboxes_TenantId",
                table: "TenantMonitoredInboxes",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantMonitoredInboxes");

            migrationBuilder.AddColumn<string>(
                name: "FaxSenderAddress",
                table: "TenantInboxSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEmailProcessedAt",
                table: "TenantInboxSettings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
