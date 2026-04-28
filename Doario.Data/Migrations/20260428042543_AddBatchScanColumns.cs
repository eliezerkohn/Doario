using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchScanColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatchPageEnd",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BatchPageStart",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BatchScanId",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchPageEnd",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "BatchPageStart",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "BatchScanId",
                table: "Documents");
        }
    }
}
