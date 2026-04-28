using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Doario.Data.Models.Mail;
using Doario.Data.Models.SaaS;
using Doario.Data.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Doario.Web.Services
{
    /// <summary>
    /// Pulls all users from Microsoft 365 (Azure AD) via Graph API
    /// and upserts them into ImportedStaff for the given tenant.
    /// Never deletes manually-added staff — upsert only.
    /// </summary>
    public class StaffSyncService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly IStaffRepository _staffRepo;
        private readonly ITenantRepository _tenantRepo;
        private readonly IErrorLogRepository _errorLogRepo;
        private readonly ILogger<StaffSyncService> _logger;

        // Reuse the same seeded SenderTypeId for staff
        private static readonly Guid StaffSyncSourceId = new Guid("a1b2c3d4-0003-0003-0003-000000000003"); // SystemStaffId placeholder

        public StaffSyncService(
            GraphServiceClient graphClient,
            IStaffRepository staffRepo,
            ITenantRepository tenantRepo,
            IErrorLogRepository errorLogRepo,
            ILogger<StaffSyncService> logger)
        {
            _graphClient = graphClient;
            _staffRepo = staffRepo;
            _tenantRepo = tenantRepo;
            _errorLogRepo = errorLogRepo;
            _logger = logger;
        }

        /// <summary>
        /// Syncs Azure AD users into ImportedStaff for the given tenant.
        /// Returns a SyncResult with added/updated counts.
        /// </summary>
        public async Task<StaffSyncResult> SyncAsync(Guid tenantId)
        {
            var result = new StaffSyncResult();

            try
            {
                var tenant = await _tenantRepo.GetByIdAsync(tenantId);
                if (tenant == null)
                    throw new InvalidOperationException($"Tenant {tenantId} not found.");

                // Pull all users from Azure AD — select only what we need
                var users = new List<User>();
                var page = await _graphClient.Users.GetAsync(config =>
                {
                    config.QueryParameters.Select = new[]
                    {
                        "id", "displayName", "givenName", "surname",
                        "mail", "userPrincipalName", "accountEnabled", "jobTitle", "department"
                    };
                    config.QueryParameters.Top = 999;
                    config.QueryParameters.Filter = "accountEnabled eq true";
                });

                while (page != null)
                {
                    if (page.Value != null)
                        users.AddRange(page.Value);

                    if (page.OdataNextLink == null)
                        break;

                    page = await _graphClient.Users
                        .WithUrl(page.OdataNextLink)
                        .GetAsync();
                }

                _logger.LogInformation("StaffSync: {Count} active users pulled from Azure AD for tenant {TenantId}.", users.Count, tenantId);

                foreach (var user in users)
                {
                    // Resolve email — prefer mail, fall back to userPrincipalName
                    var email = string.IsNullOrWhiteSpace(user.Mail)
                        ? user.UserPrincipalName
                        : user.Mail;

                    if (string.IsNullOrWhiteSpace(email))
                        continue;

                    // Skip service accounts and non-human accounts
                    if (email.StartsWith("admin@", StringComparison.OrdinalIgnoreCase) ||
                        email.Contains("#EXT#", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Skip the tenant's own mailbox — it's a system account, not a staff member
                    if (email.Equals(tenant.MailboxAddress, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Only sync staff on the tenant's own domain — skip Gmail, guests, externals
                    if (!email.EndsWith("@" + tenant.Domain, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var existing = await _staffRepo.GetByEmailAsync(email, tenantId);

                    if (existing != null)
                    {
                        // Update display name if changed — never overwrite IsActive=false set by admin
                        existing.FirstName = user.GivenName ?? existing.FirstName;
                        existing.LastName = user.Surname ?? existing.LastName;
                        existing.JobTitle = user.JobTitle;
                        existing.Department = user.Department;
                        existing.UpdatedAt = DateTime.UtcNow;
                        result.Updated++;
                    }
                    else
                    {
                        var staff = new ImportedStaff
                        {
                            ImportedStaffId = Guid.NewGuid(),
                            TenantId = tenantId,
                            FirstName = user.GivenName ?? string.Empty,
                            LastName = user.Surname ?? string.Empty,
                            Email = email,
                            JobTitle = user.JobTitle,
                            Department = user.Department,
                            IsActive = true,
                            Source = "M365Sync",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await _staffRepo.AddAsync(staff);
                        result.Added++;
                    }
                }

                await _staffRepo.SaveAsync();

                result.Success = true;
                result.TotalPulled = users.Count;

                _logger.LogInformation(
                    "StaffSync complete for tenant {TenantId}: {Added} added, {Updated} updated.",
                    tenantId, result.Added, result.Updated);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;

                _logger.LogError(ex, "StaffSync failed for tenant {TenantId}.", tenantId);

                await _errorLogRepo.AddAsync(new ErrorLog
                {
                    ErrorLogId = Guid.NewGuid(),
                    TenantId = tenantId,
                    DocumentId = null,
                    ErrorType = "StaffSync",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    CreatedAt = DateTime.UtcNow
                });
                await _errorLogRepo.SaveAsync();
            }

            return result;
        }
    }

    public class StaffSyncResult
    {
        public bool Success { get; set; }
        public int TotalPulled { get; set; }
        public int Added { get; set; }
        public int Updated { get; set; }
        public string ErrorMessage { get; set; }
    }
}