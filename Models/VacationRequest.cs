using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VacationManager.Models
{
    public class VacationRequest
    {
        public int Id { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public bool IsHalfDay { get; set; }

        public string Status { get; set; } = "Pending";

        [Required]
        public int VacationTypeId { get; set; }

        public VacationType? VacationType { get; set; }

        public string? FilePath { get; set; }

        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }
    }
}