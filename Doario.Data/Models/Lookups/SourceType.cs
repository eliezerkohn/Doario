using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doario.Data.Models.Lookups;

/// <summary>
/// Lookup — where imported data came from
/// Default rows: 100=CSV, 200=MicrosoftGraph, 300=GoogleDirectory, 
///               400=Connector, 500=SalesforceAPI, 600=PowerSchoolAPI
/// </summary>
public class SourceType
{
    public int SourceTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    public int SortOrder { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.MaxValue;
}