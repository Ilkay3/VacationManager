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

        // Team Lead
        public string TeamLeadId { get; set; }
        public ApplicationUser TeamLead { get; set; }

        // Members
        public ICollection<ApplicationUser> Developers { get; set; } = new List<ApplicationUser>();
    }
}
