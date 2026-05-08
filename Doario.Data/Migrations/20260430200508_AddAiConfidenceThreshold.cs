using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiConfidenceThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AiConfidenceThreshold",
                table: "TenantAiSettings",
                type: "int",
                nullable: false,
                defaultValue: 8);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiConfidenceThreshold",
                table: "TenantAiSettings");
        }
    }
}
