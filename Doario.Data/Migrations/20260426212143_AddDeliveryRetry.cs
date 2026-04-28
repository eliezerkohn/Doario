using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryRetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportedStaff_SourceTypes_SourceTypeId",
                table: "ImportedStaff");

            migrationBuilder.DropIndex(
                name: "IX_ImportedStaff_SourceTypeId",
                table: "ImportedStaff");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "ImportedStaff");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "ImportedStaff");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "ImportedStaff");

            migrationBuilder.DropColumn(
                name: "OfficeLocation",
                table: "ImportedStaff");

            migrationBuilder.DropColumn(
                name: "SourceTypeId",
                table: "ImportedStaff");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "ImportedStaff",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "LastSyncedAt",
                table: "ImportedStaff",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "IsAdminOverridden",
                table: "ImportedStaff",
                newName: "IsActive");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "ImportedStaff",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "ImportedStaff",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ImportedStaff",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRetryAt",
                table: "DocumentDeliveries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    ErrorLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErrorType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.ErrorLogId);
                });

            migrationBuilder.InsertData(
                table: "SystemStatuses",
                columns: new[] { "SystemStatusId", "Description", "EndDate", "Name", "SortOrder", "StartDate" },
                values: new object[] { 9, null, new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "PermanentFail", 900, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DeleteData(
                table: "SystemStatuses",
                keyColumn: "SystemStatusId",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ImportedStaff");

            migrationBuilder.DropColumn(
                name: "LastRetryAt",
                table: "DocumentDeliveries");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ImportedStaff",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "ImportedStaff",
                newName: "IsAdminOverridden");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ImportedStaff",
                newName: "LastSyncedAt");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "ImportedStaff",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "ImportedStaff",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "ImportedStaff",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "ImportedStaff",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "ImportedStaff",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeLocation",
                table: "ImportedStaff",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceTypeId",
                table: "ImportedStaff",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedStaff_SourceTypeId",
                table: "ImportedStaff",
                column: "SourceTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportedStaff_SourceTypes_SourceTypeId",
                table: "ImportedStaff",
                column: "SourceTypeId",
                principalTable: "SourceTypes",
                principalColumn: "SourceTypeId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
