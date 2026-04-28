using CsvHelper;
using CsvHelper.Configuration;
using Doario.Data.Models.Mail;
using Doario.Data.Repositories;
using Doario.Web.Models;
using System.Globalization;

namespace Doario.Web.Services;

public class StaffCsvService
{
    private readonly IStaffRepository _staffRepo;
    private readonly IErrorLogRepository _errorLogRepo;

    public StaffCsvService(IStaffRepository staffRepo, IErrorLogRepository errorLogRepo)
    {
        _staffRepo = staffRepo;
        _errorLogRepo = errorLogRepo;
    }

    public async Task<CsvImportResult> ImportAsync(Stream csvStream, Guid tenantId, string tenantDomain)
    {
        var result = new CsvImportResult();
        var toUpsert = new List<ImportedStaff>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        int row = 2;
        while (await csv.ReadAsync())
        {
            var email = csv.GetField("Email")?.Trim().ToLower();
            var firstName = csv.GetField("FirstName")?.Trim();
            var lastName = csv.GetField("LastName")?.Trim();
            var jobTitle = csv.GetField("JobTitle")?.Trim();
            var department = csv.GetField("Department")?.Trim();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName))
            {
                result.Errors.Add(new CsvRowError
                {
                    Row = row,
                    Reason = "Missing required field(s): FirstName, LastName, or Email"
                });
                result.Skipped++;
                row++;
                continue;
            }

            if (!email.EndsWith("@" + tenantDomain, StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add(new CsvRowError
                {
                    Row = row,
                    Reason = $"Email does not match tenant domain (@{tenantDomain})"
                });
                result.Skipped++;
                row++;
                continue;
            }

            var existing = await _staffRepo.GetByEmailAsync(email, tenantId);

            toUpsert.Add(new ImportedStaff
            {
                ImportedStaffId = existing?.ImportedStaffId ?? Guid.NewGuid(),
                TenantId = tenantId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                JobTitle = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle,
                Department = string.IsNullOrWhiteSpace(department) ? null : department,
                Source = "CSVImport",
                IsActive = true,
                IsAdmin = existing?.IsAdmin ?? false,
                UpdatedAt = DateTime.UtcNow,
            });

            if (existing == null) result.Added++;
            else result.Updated++;

            row++;
        }

        if (toUpsert.Any())
            await _staffRepo.UpsertRangeAsync(toUpsert);

        return result;
    }
}