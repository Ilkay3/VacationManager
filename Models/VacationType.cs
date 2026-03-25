using System.ComponentModel.DataAnnotations;

namespace VacationManager.Models
{
    public class VacationType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        public ICollection<VacationRequest> VacationRequests { get; set; } = new List<VacationRequest>();
    }
}
