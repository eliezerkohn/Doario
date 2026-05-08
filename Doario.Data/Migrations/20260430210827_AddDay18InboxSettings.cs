using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDay18InboxSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantInboxSettings",
                columns: table => new
                {
                    TenantInboxSettingsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FaxSenderAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InboxPollingIntervalSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    StaffSyncIntervalHours = table.Column<int>(type: "int", nullable: false, defaultValue: 24),
                    LastEmailProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastStaffSyncAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInboxSettings", x => x.TenantInboxSettingsId);
                    table.ForeignKey(
                        name: "FK_TenantInboxSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInboxSettings_TenantId",
                table: "TenantInboxSettings",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantInboxSettings");
        }
    }
}
