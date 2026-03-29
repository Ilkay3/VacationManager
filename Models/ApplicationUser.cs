using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace VacationManager.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        public string FullName => FirstName + " " + LastName;

        // Team
        public int? TeamId { get; set; }
        public Team Team { get; set; }

        // Ако е Team Lead
        public Team LedTeam { get; set; }

        public ICollection<VacationRequest> VacationRequests { get; set; } = new List<VacationRequest>();
    }
}
