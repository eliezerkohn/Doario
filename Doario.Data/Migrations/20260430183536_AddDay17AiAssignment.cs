using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDay17AiAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FeedbackTypeId",
                table: "DocumentFeedbacks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SuggestionStatuses",
                columns: table => new
                {
                    SuggestionStatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionStatuses", x => x.SuggestionStatusId);
                });

            migrationBuilder.CreateTable(
                name: "TenantAiSettings",
                columns: table => new
                {
                    TenantAiSettingsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiAssignmentMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAiSettings", x => x.TenantAiSettingsId);
                    table.ForeignKey(
                        name: "FK_TenantAiSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAiSuggestions",
                columns: table => new
                {
                    DocumentAiSuggestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestedStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestedEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Confidence = table.Column<int>(type: "int", nullable: false),
                    SuggestionStatusId = table.Column<int>(type: "int", nullable: false),
                    ReviewedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAiSuggestions", x => x.DocumentAiSuggestionId);
                    table.ForeignKey(
                        name: "FK_DocumentAiSuggestions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAiSuggestions_ImportedStaff_SuggestedStaffId",
                        column: x => x.SuggestedStaffId,
                        principalTable: "ImportedStaff",
                        principalColumn: "ImportedStaffId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAiSuggestions_SuggestionStatuses_SuggestionStatusId",
                        column: x => x.SuggestionStatusId,
                        principalTable: "SuggestionStatuses",
                        principalColumn: "SuggestionStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAiSuggestions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAiSuggestions_DocumentId",
                table: "DocumentAiSuggestions",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAiSuggestions_SuggestedStaffId",
                table: "DocumentAiSuggestions",
                column: "SuggestedStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAiSuggestions_SuggestionStatusId",
                table: "DocumentAiSuggestions",
                column: "SuggestionStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAiSuggestions_TenantId",
                table: "DocumentAiSuggestions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantAiSettings_TenantId",
                table: "TenantAiSettings",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentAiSuggestions");

            migrationBuilder.DropTable(
                name: "TenantAiSettings");

            migrationBuilder.DropTable(
                name: "SuggestionStatuses");

            migrationBuilder.DropColumn(
                name: "FeedbackTypeId",
                table: "DocumentFeedbacks");
        }
    }
}
