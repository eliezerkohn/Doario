using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doario.Data.Models.Lookups;

public class AssignmentType
{
    public int AssignmentTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

}