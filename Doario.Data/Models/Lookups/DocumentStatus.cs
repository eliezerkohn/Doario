using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doario.Data.Models.Lookups
{
    public class DocumentStatus
    {
        public int DocumentStatusId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        public int SortOrder { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.MaxValue;
    }
}