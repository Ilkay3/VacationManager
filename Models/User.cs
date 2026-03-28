using System.ComponentModel.DataAnnotations;
using System.Data;

namespace VacationManager.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        // Team
        public int? TeamId { get; set; }
        public Team Team { get; set; }

        // Ако е Team Lead
        public Team LedTeam { get; set; }

        public ICollection<VacationRequest> VacationRequests { get; set; } = new List<VacationRequest>();
    }
}
