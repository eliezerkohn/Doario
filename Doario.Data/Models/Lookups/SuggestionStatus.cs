using System.ComponentModel.DataAnnotations;

namespace Doario.Data.Models.Lookups
{
    public class SuggestionStatus
    {
        public int SuggestionStatusId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }

        public int SortOrder { get; set; }
    }
}