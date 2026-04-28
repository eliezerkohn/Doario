using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentFeedbacks",
                columns: table => new
                {
                    DocumentFeedbackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiClassification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CorrectedClassification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DocumentSnippet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentFeedbacks", x => x.DocumentFeedbackId);
                    table.ForeignKey(
                        name: "FK_DocumentFeedbacks_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentFeedbacks_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFeedbacks_DocumentId",
                table: "DocumentFeedbacks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFeedbacks_TenantId",
                table: "DocumentFeedbacks",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentFeedbacks");
        }
    }
}
