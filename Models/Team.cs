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

        // Project
        public int? ProjectId { get; set; }
        public Project Project { get; set; }

        // Team Lead (1 към 1)
        public string? TeamLeadId { get; set; }
        public ApplicationUser? TeamLead { get; set; }

        // Members (1 към много)
        public ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
    }
}
