using System.ComponentModel.DataAnnotations;

namespace VacationManager.Models
{
    public class UserViewModel
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        public int? TeamId { get; set; }

        public string Role { get; set; }  // за избор на роля
    }
}