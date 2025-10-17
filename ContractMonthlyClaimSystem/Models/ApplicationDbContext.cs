using ContractMonthlyClaimSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;

namespace ContractMonthlyClaimSystem.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets for each model
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<ProgrammeCoordinator> ProgrammeCoordinators { get; set; }
        public DbSet<AcademicManager> AcademicManagers { get; set; }
        public DbSet<MonthlyClaim> MonthlyClaims { get; set; }
        public DbSet<ClaimItem> ClaimItems { get; set; }
        public DbSet<SupportingDocument> SupportingDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            ConfigureLecturer(modelBuilder);
            ConfigureMonthlyClaim(modelBuilder);
            ConfigureClaimItem(modelBuilder);
            ConfigureSupportingDocument(modelBuilder);

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void ConfigureLecturer(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Lecturer>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.EmployeeNumber).IsUnique();
                entity.Property(e => e.HourlyRate).HasPrecision(18, 2);
            });

            modelBuilder.Entity<ProgrammeCoordinator>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<AcademicManager>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
            });
        }

        private void ConfigureMonthlyClaim(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MonthlyClaim>(entity =>
            {
                entity.HasIndex(e => new { e.LecturerId, e.ClaimMonth }).IsUnique();

                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.Property(e => e.TotalHours).HasPrecision(18, 2);
                entity.Property(e => e.ClaimMonth).HasMaxLength(7);

                // Relationships
                entity.HasOne(mc => mc.Lecturer)
                      .WithMany(l => l.MonthlyClaims)
                      .HasForeignKey(mc => mc.LecturerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(mc => mc.Coordinator)
                      .WithMany(pc => pc.ApprovedClaims)
                      .HasForeignKey(mc => mc.CoordinatorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(mc => mc.Manager)
                      .WithMany(am => am.FinalApprovedClaims)
                      .HasForeignKey(mc => mc.ManagerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureClaimItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClaimItem>(entity =>
            {
                entity.Property(e => e.HoursWorked).HasPrecision(18, 2);
                entity.Property(e => e.HourlyRate).HasPrecision(18, 2);

                entity.HasOne(ci => ci.MonthlyClaim)
                      .WithMany(mc => mc.ClaimItems)
                      .HasForeignKey(ci => ci.ClaimId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureSupportingDocument(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SupportingDocument>(entity =>
            {
                entity.HasOne(sd => sd.MonthlyClaim)
                      .WithMany(mc => mc.SupportingDocuments)
                      .HasForeignKey(sd => sd.ClaimId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed initial programme coordinators
            modelBuilder.Entity<ProgrammeCoordinator>().HasData(
                new ProgrammeCoordinator
                {
                    CoordinatorId = 1,
                    FirstName = "John",
                    LastName = "Smith",
                    Email = "john.smith@university.ac.za",
                    Department = "Computer Science",
                    IsActive = true
                },
                new ProgrammeCoordinator
                {
                    CoordinatorId = 2,
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Email = "sarah.johnson@university.ac.za",
                    Department = "Information Technology",
                    IsActive = true
                }
            );

            // Seed initial academic manager
            modelBuilder.Entity<AcademicManager>().HasData(
                new AcademicManager
                {
                    ManagerId = 1,
                    FirstName = "Dr. Michael",
                    LastName = "Brown",
                    Email = "michael.brown@university.ac.za",
                    IsActive = true
                }
            );
        }
    }
}
