using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class WhiteList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemStatuses",
                keyColumn: "SystemStatusId",
                keyValue: 9);

            migrationBuilder.CreateTable(
                name: "TenantWhitelistedSenders",
                columns: table => new
                {
                    TenantWhitelistedSenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderIdentifier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantWhitelistedSenders", x => x.TenantWhitelistedSenderId);
                    table.ForeignKey(
                        name: "FK_TenantWhitelistedSenders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantWhitelistedSenders_TenantId",
                table: "TenantWhitelistedSenders",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantWhitelistedSenders");

            migrationBuilder.InsertData(
                table: "SystemStatuses",
                columns: new[] { "SystemStatusId", "Description", "EndDate", "Name", "SortOrder", "StartDate" },
                values: new object[] { 9, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "PermanentFail", 900, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }
    }
}
