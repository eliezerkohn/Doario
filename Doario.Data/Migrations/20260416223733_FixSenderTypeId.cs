using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSenderTypeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportedSenders_SenderTypes_SenderTypeId1",
                table: "ImportedSenders");

            migrationBuilder.DropIndex(
                name: "IX_ImportedSenders_SenderTypeId1",
                table: "ImportedSenders");

            migrationBuilder.DropColumn(
                name: "SenderTypeId1",
                table: "ImportedSenders");

            // SQL Server cannot cast int to uniqueidentifier directly
            // Drop the old int column and add it fresh as uniqueidentifier
            migrationBuilder.DropColumn(
                name: "SenderTypeId",
                table: "ImportedSenders");

            migrationBuilder.AddColumn<Guid>(
                name: "SenderTypeId",
                table: "ImportedSenders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedSenders_SenderTypeId",
                table: "ImportedSenders",
                column: "SenderTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportedSenders_SenderTypes_SenderTypeId",
                table: "ImportedSenders",
                column: "SenderTypeId",
                principalTable: "SenderTypes",
                principalColumn: "SenderTypeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportedSenders_SenderTypes_SenderTypeId",
                table: "ImportedSenders");

            migrationBuilder.DropIndex(
                name: "IX_ImportedSenders_SenderTypeId",
                table: "ImportedSenders");

            migrationBuilder.DropColumn(
                name: "SenderTypeId",
                table: "ImportedSenders");

            migrationBuilder.AddColumn<int>(
                name: "SenderTypeId",
                table: "ImportedSenders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SenderTypeId1",
                table: "ImportedSenders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedSenders_SenderTypeId1",
                table: "ImportedSenders",
                column: "SenderTypeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportedSenders_SenderTypes_SenderTypeId1",
                table: "ImportedSenders",
                column: "SenderTypeId1",
                principalTable: "SenderTypes",
                principalColumn: "SenderTypeId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}