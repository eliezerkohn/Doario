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
    // Upload — called by UploadController on scan/fax/email ingestion
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
            throw new InvalidOperationException($"Tenant {tenantId} not found.");

        var siteId = await ResolveSiteIdAsync(tenant, cancellationToken);
        var driveId = await GetOrCreateDriveAsync(siteId, cancellationToken);

        var uniqueFileName =
            $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}-{fileName}";

        var year = DateTime.UtcNow.ToString("yyyy");
        var month = DateTime.UtcNow.ToString("MM - MMMM");
        var filePath = $"Doario Mail Room/{year}/{month}/{uniqueFileName}";

        string sharePointUrl;

        if (fileStream.Length <= 4 * 1024 * 1024)
            sharePointUrl = await SimpleUploadAsync(driveId, filePath, fileStream, cancellationToken);
        else
            sharePointUrl = await LargeFileUploadAsync(driveId, filePath, fileStream, cancellationToken);

        _logger.LogInformation(
            "Tenant {TenantId} — uploaded {FileName} → {Url}",
            tenantId, fileName, sharePointUrl);

        return sharePointUrl;
    }

    // -------------------------------------------------------------------------
    // Download — called by EmailDeliveryService to attach file to email
    // Downloads into memory only — never written to disk
    // Returns (bytes, contentType) or throws on failure
    // -------------------------------------------------------------------------

    public async Task<(byte[] Bytes, string ContentType)> DownloadFileAsync(
        Guid tenantId,
        string sharePointUrl,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);

        if (tenant is null)
            throw new InvalidOperationException($"Tenant {tenantId} not found.");

        var siteId = await ResolveSiteIdAsync(tenant, cancellationToken);
        var driveId = await GetOrCreateDriveAsync(siteId, cancellationToken);

        // Resolve the DriveItem ID from the WebUrl
        // Graph v5: search by webUrl filter to get item ID
        var encodedUrl = Uri.EscapeDataString(sharePointUrl);
        var itemRequest = await _graph.Drives[driveId]
            .Root
            .ItemWithPath(ExtractPathFromUrl(sharePointUrl))
            .GetAsync(cancellationToken: cancellationToken);

        if (itemRequest?.Id is null)
            throw new InvalidOperationException(
                $"Could not resolve SharePoint item for URL: {sharePointUrl}");

        // Download content stream into memory
        var stream = await _graph.Drives[driveId]
            .Items[itemRequest.Id]
            .Content
            .GetAsync(cancellationToken: cancellationToken);

        if (stream is null)
            throw new InvalidOperationException(
                $"SharePoint returned no content for item {itemRequest.Id}");

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        // Determine content type from file extension
        var ext = Path.GetExtension(sharePointUrl).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".tif" => "image/tiff",
            ".tiff" => "image/tiff",
            _ => "application/octet-stream"
        };

        _logger.LogInformation(
            "Tenant {TenantId} — downloaded {Bytes} bytes from SharePoint for email attachment.",
            tenantId, bytes.Length);

        return (bytes, contentType);
    }

    // -------------------------------------------------------------------------
    // Extracts the relative file path from a SharePoint WebUrl
    // e.g. https://tenant.sharepoint.com/sites/X/Shared%20Documents/Doario%20Mail%20Room/...
    //   -> Doario Mail Room/2026/04 - April/filename.pdf
    // -------------------------------------------------------------------------

    private static string ExtractPathFromUrl(string webUrl)
    {
        // Decode and find the path after "Shared Documents/"
        var decoded = Uri.UnescapeDataString(webUrl);
        var marker = "Shared Documents/";
        var idx = decoded.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (idx < 0)
        {
            // Fallback — try "Documents/"
            marker = "Documents/";
            idx = decoded.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        }

        if (idx < 0)
            throw new InvalidOperationException(
                $"Cannot extract SharePoint path from URL: {webUrl}");

        return decoded[(idx + marker.Length)..];
    }

    // -------------------------------------------------------------------------
    // Site ID resolution — cached in DB after first call
    // -------------------------------------------------------------------------

    private async Task<string> ResolveSiteIdAsync(
        Tenant tenant,
        CancellationToken cancellationToken)
    {
        if (tenant.SharePointSiteId is not null)
            return tenant.SharePointSiteId;

        if (string.IsNullOrWhiteSpace(tenant.SharePointSiteUrl))
            throw new InvalidOperationException(
                $"Tenant {tenant.TenantId} has no SharePointSiteUrl configured.");

        _logger.LogInformation(
            "Tenant {TenantId} — resolving SharePoint Site ID for the first time.",
            tenant.TenantId);

        var uri = new Uri(tenant.SharePointSiteUrl);
        var hostname = uri.Host;
        var sitePath = uri.AbsolutePath.TrimStart('/');

        var siteKey = string.IsNullOrEmpty(sitePath)
            ? hostname
            : $"{hostname}:/{sitePath}:";

        var site = await _graph.Sites[siteKey]
            .GetAsync(cancellationToken: cancellationToken);

        if (site?.Id is null)
            throw new InvalidOperationException(
                $"SharePoint site not found for tenant {tenant.TenantId}. " +
                $"Check SharePointSiteUrl: {tenant.SharePointSiteUrl}");

        tenant.SharePointSiteId = site.Id;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant {TenantId} — Site ID resolved and saved: {SiteId}",
            tenant.TenantId, site.Id);

        return site.Id;
    }

    // -------------------------------------------------------------------------
    // Drive resolution — find or create "Doario Mail Room" library
    // -------------------------------------------------------------------------

    private async Task<string> GetOrCreateDriveAsync(
        string siteId,
        CancellationToken cancellationToken)
    {
        var library = _options.DocumentLibrary;

        var drivesResponse = await _graph.Sites[siteId].Drives
            .GetAsync(cancellationToken: cancellationToken);

        var drive = drivesResponse?.Value?
            .FirstOrDefault(d =>
                string.Equals(d.Name, library, StringComparison.OrdinalIgnoreCase));

        if (drive?.Id is not null)
            return drive.Id;

        _logger.LogInformation(
            "Library '{Library}' not found on site {SiteId} — creating it.",
            library, siteId);

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

        var updatedDrives = await _graph.Sites[siteId].Drives
            .GetAsync(cancellationToken: cancellationToken);

        var createdDrive = updatedDrives?.Value?
            .FirstOrDefault(d =>
                string.Equals(d.Name, library, StringComparison.OrdinalIgnoreCase));

        if (createdDrive?.Id is null)
            throw new InvalidOperationException(
                $"Created library '{library}' on site {siteId} but could not resolve its Drive ID.");

        _logger.LogInformation(
            "Library '{Library}' created successfully on site {SiteId}.",
            library, siteId);

        return createdDrive.Id;
    }

    // -------------------------------------------------------------------------
    // Simple upload — files 4 MB and under
    // -------------------------------------------------------------------------

    private async Task<string> SimpleUploadAsync(
        string driveId,
        string filePath,
        Stream fileStream,
        CancellationToken cancellationToken)
    {
        var item = await _graph.Drives[driveId]
            .Root
            .ItemWithPath(filePath)
            .Content
            .PutAsync(fileStream,
                requestConfiguration: config =>
                {
                    config.Headers.Add("@microsoft.graph.conflictBehavior", "rename");
                },
                cancellationToken: cancellationToken);

        if (item?.WebUrl is null)
            throw new InvalidOperationException(
                "Upload completed but SharePoint returned no WebUrl.");

        return item.WebUrl;
    }

    // -------------------------------------------------------------------------
    // Large file upload — chunked, files over 4 MB
    // -------------------------------------------------------------------------

    private async Task<string> LargeFileUploadAsync(
        string driveId,
        string filePath,
        Stream fileStream,
        CancellationToken cancellationToken)
    {
        var uploadSessionRequestBody =
            new Microsoft.Graph.Drives.Item.Items.Item
                .CreateUploadSession.CreateUploadSessionPostRequestBody
            {
                Item = new DriveItemUploadableProperties
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "@microsoft.graph.conflictBehavior", "rename" }
                    }
                }
            };

        var uploadSession = await _graph.Drives[driveId]
            .Root
            .ItemWithPath(filePath)
            .CreateUploadSession
            .PostAsync(uploadSessionRequestBody,
                cancellationToken: cancellationToken);

        if (uploadSession?.UploadUrl is null)
            throw new InvalidOperationException(
                "Failed to create a SharePoint upload session.");

        var fileUploadTask = new LargeFileUploadTask<DriveItem>(
            uploadSession, fileStream, maxSliceSize: 320 * 1024);

        var result = await fileUploadTask.UploadAsync();

        if (!result.UploadSucceeded || result.ItemResponse?.WebUrl is null)
            throw new InvalidOperationException(
                "Large file upload to SharePoint failed.");

        return result.ItemResponse.WebUrl;
    }
}