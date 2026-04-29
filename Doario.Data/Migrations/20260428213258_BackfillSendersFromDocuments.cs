using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doario.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillSendersFromDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Step 1: Create Sender rows for all unique AI-extracted senders ──
            // For each unique (TenantId, SenderEmail) or (TenantId, SenderDisplayName)
            // on Documents that still point to UnknownSenderId, create a Sender row
            // if one does not already exist.

            migrationBuilder.Sql(@"
                INSERT INTO Senders (
                    SenderId,
                    TenantId,
                    SenderTypeId,
                    DisplayName,
                    Email,
                    Address,
                    Phone,
                    StartDate,
                    EndDate
                )
                SELECT
                    NEWID(),
                    d.TenantId,
                    t.UnknownSenderTypeId,
                    CASE
                        WHEN d.SenderDisplayName != '' THEN d.SenderDisplayName
                        ELSE d.SenderEmail
                    END,
                    ISNULL(d.SenderEmail, ''),
                    '',
                    '',
                    GETUTCDATE(),
                    '9999-12-31 00:00:00'
                FROM (
                    SELECT DISTINCT TenantId, SenderDisplayName, SenderEmail
                    FROM Documents
                    WHERE (SenderDisplayName != '' OR SenderEmail != '')
                      AND SenderId = (
                          SELECT UnknownSenderId
                          FROM Tenants
                          WHERE TenantId = Documents.TenantId
                      )
                ) d
                JOIN Tenants t ON t.TenantId = d.TenantId
                WHERE NOT EXISTS (
                    SELECT 1 FROM Senders s
                    WHERE s.TenantId = d.TenantId
                      AND (
                          (d.SenderEmail != '' AND s.Email = d.SenderEmail)
                          OR
                          (d.SenderEmail = '' AND s.DisplayName = d.SenderDisplayName)
                      )
                );
            ");

            // ── Step 2: Update Document.SenderId to point to resolved Sender rows ──
            // Match by email first (more reliable), then by name for email-less senders.

            migrationBuilder.Sql(@"
                -- Match by email
                UPDATE d
                SET d.SenderId = s.SenderId
                FROM Documents d
                JOIN Senders s
                    ON  s.TenantId = d.TenantId
                    AND s.Email    = d.SenderEmail
                    AND s.Email   != ''
                WHERE d.SenderEmail != ''
                  AND d.SenderId = (
                      SELECT UnknownSenderId
                      FROM Tenants
                      WHERE TenantId = d.TenantId
                  );
            ");

            migrationBuilder.Sql(@"
                -- Match by name for documents with no email
                UPDATE d
                SET d.SenderId = s.SenderId
                FROM Documents d
                JOIN Senders s
                    ON  s.TenantId    = d.TenantId
                    AND s.DisplayName = d.SenderDisplayName
                WHERE d.SenderEmail = ''
                  AND d.SenderDisplayName != ''
                  AND d.SenderId = (
                      SELECT UnknownSenderId
                      FROM Tenants
                      WHERE TenantId = d.TenantId
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert: move all documents whose Sender was created by this migration
            // back to UnknownSenderId, then delete the auto-created Sender rows.
            // We identify auto-created rows by checking they have no Phone/Address
            // and were created after the migration ran — but since we cannot know
            // the exact timestamp, Down() is a best-effort reset for dev environments.
            // In production, restore from backup rather than running Down().

            migrationBuilder.Sql(@"
                -- Reset Document.SenderId back to UnknownSenderId
                -- for documents that were matched by this migration
                UPDATE d
                SET d.SenderId = t.UnknownSenderId
                FROM Documents d
                JOIN Tenants t ON t.TenantId = d.TenantId
                JOIN Senders s ON s.SenderId = d.SenderId
                WHERE s.SenderId != t.UnknownSenderId
                  AND s.Address = ''
                  AND s.Phone   = ''
                  AND (d.SenderDisplayName != '' OR d.SenderEmail != '');
            ");

            migrationBuilder.Sql(@"
                -- Delete auto-created Sender rows (no address, no phone,
                -- not the UnknownSender placeholder)
                DELETE s
                FROM Senders s
                JOIN Tenants t ON t.TenantId = s.TenantId
                WHERE s.SenderId != t.UnknownSenderId
                  AND s.Address  = ''
                  AND s.Phone    = '';
            ");
        }
    }
}