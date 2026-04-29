using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Doario.Data.Models.Mail
{
    public class DocumentCheck
    {
        public Guid DocumentCheckId { get; set; }
        public Guid DocumentId { get; set; }
        public decimal CheckAmount { get; set; }

        [Required]
        public string CheckPayerName { get; set; }

        [Required]
        public string CheckNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [Required]
        public Document Document { get; set; }
    }
}
