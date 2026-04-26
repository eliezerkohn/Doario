using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class seedAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DocumentStatuses",
                columns: new[] { "DocumentStatusId", "EndDate", "Name", "SortOrder", "StartDate" },
                values: new object[,]
                {
                    { 5, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "OcrFailed", 500, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "EmailReceived", 600, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "MessageTypes",
                columns: new[] { "MessageTypeId", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 9, "FaxReceived", 900 },
                    { 10, "EmailReceived", 1000 }
                });

            migrationBuilder.InsertData(
                table: "SourceTypes",
                columns: new[] { "SourceTypeId", "Description", "EndDate", "Name", "SortOrder", "StartDate" },
                values: new object[,]
                {
                    { 10, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Fax", 1000, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Email", 1100, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DocumentStatuses",
                keyColumn: "DocumentStatusId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "DocumentStatuses",
                keyColumn: "DocumentStatusId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "MessageTypes",
                keyColumn: "MessageTypeId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "MessageTypes",
                keyColumn: "MessageTypeId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "SourceTypes",
                keyColumn: "SourceTypeId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "SourceTypes",
                keyColumn: "SourceTypeId",
                keyValue: 11);
        }
    }
}
