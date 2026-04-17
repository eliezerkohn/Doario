using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class SomeGuids : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_SenderTypes_SenderTypeId1",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_SenderTypeId1",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SenderTypeId1",
                table: "Documents");

            migrationBuilder.AddColumn<Guid>(
                name: "UnknownSenderTypeId",
                table: "Tenants",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // SQL Server cannot cast int to uniqueidentifier directly
            // Drop old int column and add fresh as uniqueidentifier
            migrationBuilder.DropColumn(
                name: "SenderTypeId",
                table: "Documents");

            migrationBuilder.AddColumn<Guid>(
                name: "SenderTypeId",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SenderTypeId",
                table: "Documents",
                column: "SenderTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_SenderTypes_SenderTypeId",
                table: "Documents",
                column: "SenderTypeId",
                principalTable: "SenderTypes",
                principalColumn: "SenderTypeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_SenderTypes_SenderTypeId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_SenderTypeId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SenderTypeId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "UnknownSenderTypeId",
                table: "Tenants");

            migrationBuilder.AddColumn<int>(
                name: "SenderTypeId",
                table: "Documents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SenderTypeId1",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SenderTypeId1",
                table: "Documents",
                column: "SenderTypeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_SenderTypes_SenderTypeId1",
                table: "Documents",
                column: "SenderTypeId1",
                principalTable: "SenderTypes",
                principalColumn: "SenderTypeId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}