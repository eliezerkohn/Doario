using System;
using System.ComponentModel.DataAnnotations;

namespace Doario.Data.Models.Mail
{
    /// <summary>
    /// Captures structured errors from OCR, AI, and email delivery pipelines.
    /// </summary>
    public class ErrorLog
    {
        public Guid ErrorLogId { get; set; }

        public Guid TenantId { get; set; }

        /// <summary>
        /// Nullable — some errors may occur before a document is created.
        /// </summary>
        public Guid? DocumentId { get; set; }

        /// <summary>
        /// "OCR" | "AI" | "Delivery"
        /// </summary>
        [Required, MaxLength(50)]
        public string ErrorType { get; set; }

        [Required, MaxLength(2000)]
        public string Message { get; set; }

        [MaxLength(8000)]
        public string StackTrace { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}