using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class addseeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DocumentStatuses",
                columns: new[] { "DocumentStatusId", "EndDate", "Name", "SortOrder", "StartDate" },
                values: new object[,]
                {
                    { 7, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Spam", 700, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Promotion", 800, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DocumentStatuses",
                keyColumn: "DocumentStatusId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "DocumentStatuses",
                keyColumn: "DocumentStatusId",
                keyValue: 8);
        }
    }
}
