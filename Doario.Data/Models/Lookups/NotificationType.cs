using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doario.Data.Models.Lookups
{
    public class NotificationType
    {
        public int NotificationTypeId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public int SortOrder { get; set; }
    }
}