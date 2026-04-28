using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class ViewdDoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentVieweds",
                columns: table => new
                {
                    DocumentViewedId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVieweds", x => x.DocumentViewedId);
                    table.ForeignKey(
                        name: "FK_DocumentVieweds_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentVieweds_ImportedStaff_ViewedByStaffId",
                        column: x => x.ViewedByStaffId,
                        principalTable: "ImportedStaff",
                        principalColumn: "ImportedStaffId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentVieweds_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVieweds_DocumentId",
                table: "DocumentVieweds",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVieweds_TenantId",
                table: "DocumentVieweds",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVieweds_ViewedByStaffId",
                table: "DocumentVieweds",
                column: "ViewedByStaffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentVieweds");
        }
    }
}
