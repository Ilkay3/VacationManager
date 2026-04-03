using Microsoft.CodeAnalysis;
using System.ComponentModel.DataAnnotations;

namespace VacationManager.Models
{
    public class Team
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public int? ProjectId { get; set; }
        public Project Project { get; set; }

        public string? TeamLeadId { get; set; }
        public ApplicationUser? TeamLead { get; set; }

        public ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
    }
}
