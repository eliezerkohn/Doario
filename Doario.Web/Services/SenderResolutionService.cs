using Doario.Data.Models.Lookups;
using Doario.Data.Repositories;

namespace Doario.Web.Services;

/// <summary>
/// After AI extracts a sender name/email from a document, this service
/// resolves it to a row in the Sender table and updates Document.SenderId.
///
/// Resolution order:
///   1. Match by email (exact, case-insensitive) — most reliable
///   2. Match by display name (exact, case-insensitive) — fallback
///   3. No match → create new Sender row
///   4. AI found nothing → leave Document.SenderId as UnknownSenderId
/// </summary>
public class SenderResolutionService
{
    private readonly ISenderRepository _senders;
    private readonly IDocumentRepository _documents;
    private readonly ITenantRepository _tenants;
    private readonly ILogger<SenderResolutionService> _logger;

    public SenderResolutionService(
        ISenderRepository senders,
        IDocumentRepository documents,
        ITenantRepository tenants,
        ILogger<SenderResolutionService> logger)
    {
        _senders = senders;
        _documents = documents;
        _tenants = tenants;
        _logger = logger;
    }

    /// <summary>
    /// Called after AI summary runs and SenderDisplayName/SenderEmail
    /// have been written to the Document.
    /// Resolves the sender and updates Document.SenderId.
    /// </summary>
    public async Task ResolveAsync(Guid documentId, Guid tenantId,
        string senderDisplayName, string senderEmail)
    {
        // Nothing to resolve if AI found neither name nor email
        if (string.IsNullOrWhiteSpace(senderDisplayName) &&
            string.IsNullOrWhiteSpace(senderEmail))
        {
            _logger.LogDebug(
                "SenderResolutionService: no sender info for document {Id} — leaving Unknown.",
                documentId);
            return;
        }

        try
        {
            Sender sender = null;

            // ── Step 1: Match by email ────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(senderEmail))
                sender = await _senders.GetByEmailAsync(tenantId, senderEmail);

            // ── Step 2: Match by name ─────────────────────────────────────────
            if (sender is null && !string.IsNullOrWhiteSpace(senderDisplayName))
                sender = await _senders.GetByNameAsync(tenantId, senderDisplayName);

            // ── Step 3: Create new sender ─────────────────────────────────────
            if (sender is null)
            {
                var tenant = await _tenants.GetByIdAsync(tenantId);
                if (tenant is null)
                {
                    _logger.LogWarning(
                        "SenderResolutionService: tenant {TenantId} not found.", tenantId);
                    return;
                }

                sender = new Sender
                {
                    SenderId = Guid.NewGuid(),
                    TenantId = tenantId,
                    SenderTypeId = tenant.UnknownSenderTypeId,
                    DisplayName = !string.IsNullOrWhiteSpace(senderDisplayName)
                                        ? senderDisplayName
                                        : senderEmail,
                    Email = senderEmail ?? string.Empty,
                    Address = string.Empty,
                    Phone = string.Empty,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.MaxValue,
                };

                await _senders.CreateAsync(sender);

                _logger.LogInformation(
                    "SenderResolutionService: created new Sender {SenderId} " +
                    "'{Name}' for tenant {TenantId}.",
                    sender.SenderId, sender.DisplayName, tenantId);
            }
            else
            {
                // ── Step 4: Enrich existing sender if we have new info ────────
                // e.g. we knew the name before but now have the email too
                var needsUpdate =
                    (!string.IsNullOrWhiteSpace(senderEmail) &&
                     string.IsNullOrWhiteSpace(sender.Email)) ||
                    (!string.IsNullOrWhiteSpace(senderDisplayName) &&
                     sender.DisplayName == "Unknown Sender");

                if (needsUpdate)
                {
                    await _senders.UpdateAsync(
                        sender.SenderId,
                        string.IsNullOrWhiteSpace(senderDisplayName) ? sender.DisplayName : senderDisplayName,
                        string.IsNullOrWhiteSpace(senderEmail) ? sender.Email : senderEmail);

                    _logger.LogInformation(
                        "SenderResolutionService: enriched Sender {SenderId} with new info.",
                        sender.SenderId);
                }

                _logger.LogDebug(
                    "SenderResolutionService: matched document {DocId} to " +
                    "existing Sender {SenderId} '{Name}'.",
                    documentId, sender.SenderId, sender.DisplayName);
            }

            // ── Step 5: Update Document.SenderId ─────────────────────────────
            await _documents.UpdateSenderIdAsync(documentId, sender.SenderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SenderResolutionService: failed for document {Id}.", documentId);
        }
    }
}