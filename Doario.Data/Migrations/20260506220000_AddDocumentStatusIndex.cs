using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentStatusIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index for fast folder queries — filters by TenantId + StatusId, sorted by UploadedAt
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes 
                    WHERE name = 'IX_Documents_TenantId_StatusId_UploadedAt' 
                    AND object_id = OBJECT_ID('Documents')
                )
                BEGIN
                    CREATE INDEX IX_Documents_TenantId_StatusId_UploadedAt 
                    ON Documents (TenantId, DocumentStatusId, UploadedAt DESC)
                    INCLUDE (OriginalFileName, SharePointUrl, SenderTypeId, SenderId, UploadedByStaffId, SourceTypeId)
                END
            ");

            // Index for fast unviewed count in counts endpoint
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes 
                    WHERE name = 'IX_DocumentVieweds_TenantId_DocumentId' 
                    AND object_id = OBJECT_ID('DocumentVieweds')
                )
                BEGIN
                    CREATE INDEX IX_DocumentVieweds_TenantId_DocumentId 
                    ON DocumentVieweds (TenantId, DocumentId)
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Documents_TenantId_StatusId_UploadedAt ON Documents");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_DocumentVieweds_TenantId_DocumentId ON DocumentVieweds");
        }
    }
}