using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doario.Data.Models.Mail
{
    // Query result shape for the checks list — not a DB table, not a web DTO.
    // Lives in Doario.Data because it is returned by the repository.
    public class DocumentCheckQueryResult
    {
        public Guid DocumentId { get; set; }
        public decimal CheckAmount { get; set; }
        public string CheckPayerName { get; set; }
        public string CheckNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public string OriginalFileName { get; set; }
        public string AiSummary { get; set; }
        public DateTime UploadedAt { get; set; }
        public string SenderDisplayName { get; set; }
    }
}