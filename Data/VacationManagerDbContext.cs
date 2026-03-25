using Microsoft.EntityFrameworkCore;
using VacationManager.Models;

namespace VacationManager.Data
{
    public class VacationManagerDbContext : DbContext
    {
        public VacationManagerDbContext(DbContextOptions<VacationManagerDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<VacationRequest> VacationRequests { get; set; }
        public DbSet<VacationType> VacationTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔹 User -> Role
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 User -> Team
            modelBuilder.Entity<User>()
                .HasOne(u => u.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(u => u.TeamId)
                .OnDelete(DeleteBehavior.SetNull);

            // 🔹 Team -> TeamLead (1:1)
            modelBuilder.Entity<Team>()
                .HasOne(t => t.TeamLead)
                .WithOne(u => u.LedTeam)
                .HasForeignKey<Team>(t => t.TeamLeadId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 Team -> Project
            modelBuilder.Entity<Team>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Teams)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔹 VacationRequest -> User
            modelBuilder.Entity<VacationRequest>()
                .HasOne(v => v.User)
                .WithMany(u => u.VacationRequests)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔹 VacationRequest -> VacationType
            modelBuilder.Entity<VacationRequest>()
                .HasOne(v => v.VacationType)
                .WithMany(t => t.VacationRequests)
                .HasForeignKey(v => v.VacationTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔥 Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "CEO" },
                new Role { Id = 2, Name = "Team Lead" },
                new Role { Id = 3, Name = "Developer" },
                new Role { Id = 4, Name = "Unassigned" }
            );

            // 🔥 Seed VacationTypes
            modelBuilder.Entity<VacationType>().HasData(
                new VacationType { Id = 1, Name = "Paid" },
                new VacationType { Id = 2, Name = "Unpaid" },
                new VacationType { Id = 3, Name = "Sick" }
            );
        }
    }
}