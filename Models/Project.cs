using System.ComponentModel.DataAnnotations;

namespace VacationManager.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public ICollection<Team> Teams { get; set; } = new List<Team>();
    }
}