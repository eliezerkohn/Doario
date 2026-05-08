using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    public partial class AddDocumentSourceTypeId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add column without FK first — nullable so existing rows get null
            migrationBuilder.AddColumn<int>(
                name: "SourceTypeId",
                table: "Documents",
                type: "int",
                nullable: true);

            // Backfill all existing rows to 12 (Scanner)
            migrationBuilder.Sql(
                "UPDATE [Documents] SET [SourceTypeId] = 12 WHERE [SourceTypeId] IS NULL");

            // Make it non-nullable now all rows have a valid value
            migrationBuilder.AlterColumn<int>(
                name: "SourceTypeId",
                table: "Documents",
                type: "int",
                nullable: false,
                defaultValue: 12,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Now safe to add FK and index
            migrationBuilder.CreateIndex(
                name: "IX_Documents_SourceTypeId",
                table: "Documents",
                column: "SourceTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_SourceTypes_SourceTypeId",
                table: "Documents",
                column: "SourceTypeId",
                principalTable: "SourceTypes",
                principalColumn: "SourceTypeId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_SourceTypes_SourceTypeId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_SourceTypeId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SourceTypeId",
                table: "Documents");
        }
    }
}