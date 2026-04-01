using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VacationManager.Models;

namespace VacationManager.Data
{
    public class VacationManagerDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Team> Teams { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<VacationRequest> VacationRequests { get; set; }
        public DbSet<VacationType> VacationTypes { get; set; }

        public VacationManagerDbContext(DbContextOptions<VacationManagerDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ------------------------------
            // Team → TeamLead (1:1)
            // TeamLeadId е FK в Team, сочи към ApplicationUser
            // ------------------------------
            builder.Entity<Team>()
                .HasOne(t => t.TeamLead)
                .WithOne(u => u.LedTeam)
                .HasForeignKey<Team>(t => t.TeamLeadId)
                .OnDelete(DeleteBehavior.Restrict);

            // ------------------------------
            // User → Team (many-to-1)
            // TeamId е FK в ApplicationUser, сочи към Team
            // ------------------------------
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(u => u.TeamId)
                .OnDelete(DeleteBehavior.SetNull);

            // ------------------------------
            // Team → Project (many-to-1)
            // ------------------------------
            builder.Entity<Team>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Teams)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            // ------------------------------
            // VacationRequest → User (many-to-1)
            // ------------------------------
            builder.Entity<VacationRequest>()
                .HasOne(v => v.User)
                .WithMany(u => u.VacationRequests)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ------------------------------
            // VacationRequest → VacationType (many-to-1)
            // ------------------------------
            builder.Entity<VacationRequest>()
                .HasOne(v => v.VacationType)
                .WithMany(t => t.VacationRequests)
                .HasForeignKey(v => v.VacationTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ------------------------------
            // MySQL fix за DateTime
            // ------------------------------
            builder.Entity<VacationRequest>()
                .Property(v => v.CreatedOn)
                .HasColumnType("datetime");

            // ------------------------------
            // Seed VacationTypes
            // ------------------------------
            builder.Entity<VacationType>().HasData(
                new VacationType { Id = 1, Name = "Paid" },
                new VacationType { Id = 2, Name = "Unpaid" },
                new VacationType { Id = 3, Name = "Sick" }
            );

            // ------------------------------
            // Seed Identity Roles
            // ------------------------------
            var roles = new[]
            {
            new IdentityRole { Id = "1", Name = "CEO", NormalizedName = "CEO" },
            new IdentityRole { Id = "2", Name = "Team Lead", NormalizedName = "TEAM LEAD" },
            new IdentityRole { Id = "3", Name = "Unassigned", NormalizedName = "UNASSIGNED" },
            new IdentityRole { Id = "4", Name = "Developer", NormalizedName = "DEVELOPER" }
        };

            builder.Entity<IdentityRole>().HasData(roles);
        }
        public DbSet<VacationManager.Models.UserViewModel> UserViewModel { get; set; } = default!;
    }
}