using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScannerSourceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SourceTypes",
                columns: new[] { "SourceTypeId", "Description", "EndDate", "Name", "SortOrder", "StartDate" },
                values: new object[] { 12, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Scanner", 1200, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SourceTypes",
                keyColumn: "SourceTypeId",
                keyValue: 12);
        }
    }
}
