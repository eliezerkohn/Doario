using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class fetchTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MonitoredInboxId",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_MonitoredInboxId",
                table: "Documents",
                column: "MonitoredInboxId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_TenantMonitoredInboxes_MonitoredInboxId",
                table: "Documents",
                column: "MonitoredInboxId",
                principalTable: "TenantMonitoredInboxes",
                principalColumn: "TenantMonitoredInboxId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_TenantMonitoredInboxes_MonitoredInboxId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_MonitoredInboxId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "MonitoredInboxId",
                table: "Documents");
        }
    }
}
