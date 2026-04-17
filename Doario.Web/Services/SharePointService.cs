using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Options;
using Doario.Data;
using Doario.Data.Models.SaaS;
using Microsoft.EntityFrameworkCore;

namespace Doario.Web.Services;

public class SharePointService
{
    private readonly GraphServiceClient _graph;
    private readonly SharePointOptions _options;
    private readonly DoarioDataContext _db;
    private readonly ILogger<SharePointService> _logger;

    public SharePointService(
        GraphServiceClient graph,
        IOptions<SharePointOptions> options,
        DoarioDataContext db,
        ILogger<SharePointService> logger)
    {
        _graph = graph;
        _options = options.Value;
        _db = db;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // Public entry point — called by the controller
    // -------------------------------------------------------------------------

    public async Task<string> UploadDocumentAsync(
        Guid tenantId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);

        if (tenant is null)
            throw new InvalidOperationException(
                $"Tenant {tenantId} not found.");

        // Resolve Site ID — uses DB cache or calls Graph once
        var siteId = await ResolveSiteIdAsync(tenant, cancellationToken);

        // Find or create "Doario Mail Room" library
        var driveId = await GetOrCreateDriveAsync(siteId, cancellationToken);

        // Unique file name — timestamp + GUID prevents any collision
        var uniqueFileName =
            $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}-{fileName}";

        // Year/month folder — Graph creates folders automatically if missing
        var year = DateTime.UtcNow.ToString("yyyy");
        var month = DateTime.UtcNow.ToString("MM - MMMM");
        var filePath = $"Doario Mail Room/{year}/{month}/{uniqueFileName}";

        string sharePointUrl;

        if (fileStream.Length <= 4 * 1024 * 1024)
            sharePointUrl = await SimpleUploadAsync(
                driveId, filePath, fileStream, cancellationToken);
        else
            sharePointUrl = await LargeFileUploadAsync(
                driveId, filePath, fileStream, cancellationToken);

        _logger.LogInformation(
            "Tenant {TenantId} — uploaded {FileName} → {Url}",
            tenantId, fileName, sharePointUrl);

        return sharePointUrl;
    }

    // -------------------------------------------------------------------------
    // Site ID — resolve once, save to DB, never call Graph again for this tenant
    // -------------------------------------------------------------------------

    private async Task<string> ResolveSiteIdAsync(
     Tenant tenant,
     CancellationToken cancellationToken)
    {
        // Already in DB — return immediately, no Graph call
        if (tenant.SharePointSiteId is not null)
            return tenant.SharePointSiteId;

        if (string.IsNullOrWhiteSpace(tenant.SharePointSiteUrl))
            throw new InvalidOperationException(
                $"Tenant {tenant.TenantId} has no SharePointSiteUrl configured. " +
                $"SharePoint must be set up before documents can be uploaded.");

        _logger.LogInformation(
            "Tenant {TenantId} — resolving SharePoint Site ID for the first time.",
            tenant.TenantId);

        var uri = new Uri(tenant.SharePointSiteUrl);
        var hostname = uri.Host;
        var sitePath = uri.AbsolutePath.TrimStart('/');

        // For root site (no path) use hostname only
        // For site collections (with path) use hostname:/path: format
        var siteKey = string.IsNullOrEmpty(sitePath)
            ? hostname
            : $"{hostname}:/{sitePath}:";

        var site = await _graph.Sites[siteKey]
            .GetAsync(cancellationToken: cancellationToken);

        if (site?.Id is null)
            throw new InvalidOperationException(
                $"SharePoint site not found for tenant {tenant.TenantId}. " +
                $"Check SharePointSiteUrl: {tenant.SharePointSiteUrl}");

        // Save to DB — this Graph call never happens again for this tenant
        tenant.SharePointSiteId = site.Id;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant {TenantId} — Site ID resolved and saved: {SiteId}",
            tenant.TenantId, site.Id);

        return site.Id;
    }

    // -------------------------------------------------------------------------
    // Drive — find "Doario Mail Room", create it if this is a brand new tenant
    // -------------------------------------------------------------------------

    private async Task<string> GetOrCreateDriveAsync(
        string siteId,
        CancellationToken cancellationToken)
    {
        var library = _options.DocumentLibrary; // "Doario Mail Room"

        var drivesResponse = await _graph.Sites[siteId].Drives
            .GetAsync(cancellationToken: cancellationToken);

        var drive = drivesResponse?.Value?
            .FirstOrDefault(d =>
                string.Equals(d.Name, library,
                    StringComparison.OrdinalIgnoreCase));

        // Found — return immediately
        if (drive?.Id is not null)
            return drive.Id;

        // Not found — create it
        // This runs exactly once per tenant on their very first upload
        _logger.LogInformation(
            "Library '{Library}' not found on site {SiteId} — creating it.",
            library, siteId);

        // Graph v5 — ListInfo is a nested property, Template uses the BaseType string
        // ListTemplate enum does not exist in v5 — use the string value directly
        await _graph.Sites[siteId].Lists
            .PostAsync(new Microsoft.Graph.Models.List
            {
                DisplayName = library,
                AdditionalData = new Dictionary<string, object>
                {
            { "template", "documentLibrary" }
                }
            },
            cancellationToken: cancellationToken);
        // Fetch drives again — newly created library now appears
        var updatedDrives = await _graph.Sites[siteId].Drives
            .GetAsync(cancellationToken: cancellationToken);

        var createdDrive = updatedDrives?.Value?
            .FirstOrDefault(d =>
                string.Equals(d.Name, library,
                    StringComparison.OrdinalIgnoreCase));

        if (createdDrive?.Id is null)
            throw new InvalidOperationException(
                $"Created library '{library}' on site {siteId} " +
                $"but could not resolve its Drive ID.");

        _logger.LogInformation(
            "Library '{Library}' created successfully on site {SiteId}.",
            library, siteId);

        return createdDrive.Id;
    }

    // -------------------------------------------------------------------------
    // Simple upload — one request, for files 4 MB and under
    // -------------------------------------------------------------------------

    private async Task<string> SimpleUploadAsync(
        string driveId,
        string filePath,
        Stream fileStream,
        CancellationToken cancellationToken)
    {
        // filePath example: "2026/04 - April/20260416-143022-{guid}-letter.pdf"
        // Graph creates year and month folders automatically if they do not exist

        var item = await _graph.Drives[driveId]
            .Root
            .ItemWithPath(filePath)
            .Content
            .PutAsync(fileStream,
                requestConfiguration: config =>
                {
                    // Safety net — if name exists rename new file, never overwrite
                    config.Headers.Add(
                        "@microsoft.graph.conflictBehavior", "rename");
                },
                cancellationToken: cancellationToken);

        if (item?.WebUrl is null)
            throw new InvalidOperationException(
                "Upload completed but SharePoint returned no WebUrl.");

        return item.WebUrl;
    }

    // -------------------------------------------------------------------------
    // Large file upload — chunked, for files over 4 MB
    // -------------------------------------------------------------------------

    private async Task<string> LargeFileUploadAsync(
        string driveId,
        string filePath,
        Stream fileStream,
        CancellationToken cancellationToken)
    {
        // Graph v5 — upload session lives under Drives.Item.Items.Item namespace
        var uploadSessionRequestBody =
            new Microsoft.Graph.Drives.Item.Items.Item
                .CreateUploadSession.CreateUploadSessionPostRequestBody
            {
                Item = new DriveItemUploadableProperties
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        // Consistent with simple upload — rename on conflict
                        { "@microsoft.graph.conflictBehavior", "rename" }
                    }
                }
            };

        // Stage 1 — create upload session via Root.ItemWithPath
        var uploadSession = await _graph.Drives[driveId]
            .Root
            .ItemWithPath(filePath)
            .CreateUploadSession
            .PostAsync(uploadSessionRequestBody,
                cancellationToken: cancellationToken);

        if (uploadSession?.UploadUrl is null)
            throw new InvalidOperationException(
                "Failed to create a SharePoint upload session.");

        // Stage 2 — SDK cuts file into 320 KB slices (Microsoft's minimum)
        // and handles sequencing, retries, and final assembly automatically
        var fileUploadTask = new LargeFileUploadTask<DriveItem>(
            uploadSession, fileStream, maxSliceSize: 320 * 1024);

        // Stage 3 — upload all slices
        var result = await fileUploadTask.UploadAsync();

        if (!result.UploadSucceeded || result.ItemResponse?.WebUrl is null)
            throw new InvalidOperationException(
                "Large file upload to SharePoint failed.");

        return result.ItemResponse.WebUrl;
    }
}