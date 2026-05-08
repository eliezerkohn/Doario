using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class seedDay17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantAiSettings_TenantId",
                table: "TenantAiSettings");

            migrationBuilder.AlterColumn<string>(
                name: "AiAssignmentMode",
                table: "TenantAiSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "AutoAssign",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "FeedbackTypeId",
                table: "DocumentFeedbacks",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.InsertData(
                table: "SuggestionStatuses",
                columns: new[] { "SuggestionStatusId", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Pending", 100 },
                    { 2, "Approved", 200 },
                    { 3, "Overwritten", 300 },
                    { 4, "AutoAssigned", 400 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantAiSettings_TenantId",
                table: "TenantAiSettings",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantAiSettings_TenantId",
                table: "TenantAiSettings");

            migrationBuilder.DeleteData(
                table: "SuggestionStatuses",
                keyColumn: "SuggestionStatusId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SuggestionStatuses",
                keyColumn: "SuggestionStatusId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "SuggestionStatuses",
                keyColumn: "SuggestionStatusId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "SuggestionStatuses",
                keyColumn: "SuggestionStatusId",
                keyValue: 4);

            migrationBuilder.AlterColumn<string>(
                name: "AiAssignmentMode",
                table: "TenantAiSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "AutoAssign");

            migrationBuilder.AlterColumn<int>(
                name: "FeedbackTypeId",
                table: "DocumentFeedbacks",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_TenantAiSettings_TenantId",
                table: "TenantAiSettings",
                column: "TenantId");
        }
    }
}
