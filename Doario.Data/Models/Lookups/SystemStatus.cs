using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doario.Data.Models.Lookups;

/// <summary>
/// Shared system status lookup table
/// Used by TenantConnection, StaffSyncLog, DocumentDelivery
/// Default rows:
/// 100=Active, 200=Expired, 300=Revoked
/// 400=Success, 500=Failed, 600=PartialSuccess
/// 700=Pending, 800=Sent
/// </summary>
public class SystemStatus
{
    public int SystemStatusId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    public int SortOrder { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.MaxValue;
}