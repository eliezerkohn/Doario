using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doario.Data.Models.Lookups
{
    /// <summary>
    /// Types of messages and actions that can be logged against a document.
    /// Global — same for all tenants.
    /// </summary>
    public class MessageType
    {
        public int MessageTypeId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public int SortOrder { get; set; }
    }
}