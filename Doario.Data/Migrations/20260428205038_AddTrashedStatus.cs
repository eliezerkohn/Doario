using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrashedStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DocumentStatuses",
                columns: new[] { "DocumentStatusId", "EndDate", "Name", "SortOrder", "StartDate" },
                values: new object[] { 9, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Trashed", 900, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DocumentStatuses",
                keyColumn: "DocumentStatusId",
                keyValue: 9);
        }
    }
}
