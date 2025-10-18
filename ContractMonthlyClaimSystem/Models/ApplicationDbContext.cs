using System.Reflection;
using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<ProgrammeCoordinator> ProgrammeCoordinators { get; set; }
        public DbSet<AcademicManager> AcademicManagers { get; set; }
        public DbSet<MonthlyClaim> MonthlyClaims { get; set; }
        public DbSet<ClaimItem> ClaimItems { get; set; }
        public DbSet<SupportingDocument> SupportingDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Configure entity relationships and constraints
            ConfigureUserRelationships(modelBuilder);
            ConfigureMonthlyClaimRelationships(modelBuilder);
            ConfigureClaimItemRelationships(modelBuilder);
            ConfigureSupportingDocumentRelationships(modelBuilder);
            ConfigureEnums(modelBuilder);
            ConfigureIndexes(modelBuilder);
        }

        private void ConfigureUserRelationships(ModelBuilder modelBuilder)
        {
            // User -> Lecturer (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Lecturer)
                .WithOne(l => l.User)
                .HasForeignKey<User>(u => u.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> ProgrammeCoordinator (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Coordinator)
                .WithOne(pc => pc.User)
                .HasForeignKey<User>(u => u.CoordinatorId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> AcademicManager (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Manager)
                .WithOne(am => am.User)
                .HasForeignKey<User>(u => u.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Lecturer configurations
            modelBuilder.Entity<Lecturer>(entity =>
            {
                entity.HasKey(l => l.LecturerId);
                entity.HasIndex(l => l.Email).IsUnique();
                entity.HasIndex(l => l.EmployeeNumber).IsUnique();

                entity.Property(l => l.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(l => l.LastName).IsRequired().HasMaxLength(100);
                entity.Property(l => l.Email).IsRequired().HasMaxLength(256);
                entity.Property(l => l.EmployeeNumber).IsRequired().HasMaxLength(20);
                entity.Property(l => l.PhoneNumber).HasMaxLength(15);
                entity.Property(l => l.HourlyRate).HasColumnType("decimal(10,2)");

                // Ignore computed property for database
                entity.Ignore(l => l.FullName);
            });

            // ProgrammeCoordinator configurations
            modelBuilder.Entity<ProgrammeCoordinator>(entity =>
            {
                entity.HasKey(pc => pc.CoordinatorId);
                entity.HasIndex(pc => pc.Email).IsUnique();

                entity.Property(pc => pc.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(pc => pc.LastName).IsRequired().HasMaxLength(100);
                entity.Property(pc => pc.Email).IsRequired().HasMaxLength(256);
                entity.Property(pc => pc.Department).IsRequired().HasMaxLength(100);

                // Ignore computed property for database
                entity.Ignore(pc => pc.FullName);
            });

            // AcademicManager configurations
            modelBuilder.Entity<AcademicManager>(entity =>
            {
                entity.HasKey(am => am.ManagerId);
                entity.HasIndex(am => am.Email).IsUnique();

                entity.Property(am => am.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(am => am.LastName).IsRequired().HasMaxLength(100);
                entity.Property(am => am.Email).IsRequired().HasMaxLength(256);

                // Ignore computed property for database
                entity.Ignore(am => am.FullName);
            });
        }

        private void ConfigureMonthlyClaimRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MonthlyClaim>(entity =>
            {
                entity.HasKey(mc => mc.ClaimId);

                // Lecturer -> MonthlyClaims (One-to-Many)
                entity.HasOne(mc => mc.Lecturer)
                    .WithMany(l => l.MonthlyClaims)
                    .HasForeignKey(mc => mc.LecturerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // ProgrammeCoordinator -> MonthlyClaims (One-to-Many)
                entity.HasOne(mc => mc.Coordinator)
                    .WithMany(pc => pc.ApprovedClaims)
                    .HasForeignKey(mc => mc.CoordinatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                // AcademicManager -> MonthlyClaims (One-to-Many)
                entity.HasOne(mc => mc.Manager)
                    .WithMany(am => am.FinalApprovedClaims)
                    .HasForeignKey(mc => mc.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Properties configuration
                entity.Property(mc => mc.ClaimMonth).IsRequired();
                entity.Property(mc => mc.SubmissionDate).IsRequired();
                entity.Property(mc => mc.TotalHours).HasColumnType("decimal(8,2)").HasDefaultValue(0);
                entity.Property(mc => mc.TotalAmount).HasColumnType("decimal(10,2)").HasDefaultValue(0);
                entity.Property(mc => mc.Status).IsRequired().HasConversion<int>();
                entity.Property(mc => mc.CoordinatorComments).HasMaxLength(500);
                entity.Property(mc => mc.ManagerComments).HasMaxLength(500);

                // Unique constraint: One claim per lecturer per month
                entity.HasIndex(mc => new { mc.LecturerId, mc.ClaimMonth })
                    .IsUnique();

                // Ignore computed properties for database
                entity.Ignore(mc => mc.DisplayMonth);
                entity.Ignore(mc => mc.CanBeEdited);
                entity.Ignore(mc => mc.CanBeSubmitted);
                entity.Ignore(mc => mc.RequiresCoordinator);
                entity.Ignore(mc => mc.RequiresManager);
            });
        }

        private void ConfigureClaimItemRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClaimItem>(entity =>
            {
                entity.HasKey(ci => ci.ItemId);

                // MonthlyClaim -> ClaimItems (One-to-Many)
                entity.HasOne(ci => ci.MonthlyClaim)
                    .WithMany(mc => mc.ClaimItems)
                    .HasForeignKey(ci => ci.ClaimId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Properties configuration
                entity.Property(ci => ci.WorkDate).IsRequired();
                entity.Property(ci => ci.Description).IsRequired().HasMaxLength(200);
                entity.Property(ci => ci.HoursWorked).HasColumnType("decimal(4,2)");
                entity.Property(ci => ci.HourlyRate).HasColumnType("decimal(10,2)");

                // Check constraint for hours worked
                entity.HasCheckConstraint("CK_ClaimItem_HoursWorked", "[HoursWorked] BETWEEN 0.5 AND 24.0");

                // Ignore computed property for database
                entity.Ignore(ci => ci.TotalAmount);
            });
        }

        private void ConfigureSupportingDocumentRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SupportingDocument>(entity =>
            {
                entity.HasKey(sd => sd.DocumentId);

                // MonthlyClaim -> SupportingDocuments (One-to-Many)
                entity.HasOne(sd => sd.MonthlyClaim)
                    .WithMany(mc => mc.SupportingDocuments)
                    .HasForeignKey(sd => sd.ClaimId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Properties configuration
                entity.Property(sd => sd.FileName).IsRequired().HasMaxLength(255);
                entity.Property(sd => sd.OriginalFileName).IsRequired().HasMaxLength(100);
                entity.Property(sd => sd.DocumentType).IsRequired().HasConversion<int>();
                entity.Property(sd => sd.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(sd => sd.UploadDate).IsRequired();
                entity.Property(sd => sd.Description).HasMaxLength(500);
                entity.Property(sd => sd.FileSize).IsRequired();

                // Check constraint for document type
                entity.HasCheckConstraint("CK_SupportingDocument_DocumentType", "[DocumentType] BETWEEN 0 AND 4");

                // Ignore computed property for database
                entity.Ignore(sd => sd.FileSizeDisplay);
            });
        }

        private void ConfigureEnums(ModelBuilder modelBuilder)
        {
            // Configure enum conversions
            modelBuilder.Entity<MonthlyClaim>()
                .Property(mc => mc.Status)
                .HasConversion<int>();

            modelBuilder.Entity<SupportingDocument>()
                .Property(sd => sd.DocumentType)
                .HasConversion<int>();

            modelBuilder.Entity<User>()
                .Property(u => u.UserType)
                .HasConversion<int>();
        }

        private void ConfigureIndexes(ModelBuilder modelBuilder)
        {
            // MonthlyClaim indexes
            modelBuilder.Entity<MonthlyClaim>()
                .HasIndex(mc => mc.LecturerId);

            modelBuilder.Entity<MonthlyClaim>()
                .HasIndex(mc => mc.CoordinatorId);

            modelBuilder.Entity<MonthlyClaim>()
                .HasIndex(mc => mc.ManagerId);

            modelBuilder.Entity<MonthlyClaim>()
                .HasIndex(mc => mc.Status);

            modelBuilder.Entity<MonthlyClaim>()
                .HasIndex(mc => mc.ClaimMonth);

            // ClaimItem indexes
            modelBuilder.Entity<ClaimItem>()
                .HasIndex(ci => ci.ClaimId);

            modelBuilder.Entity<ClaimItem>()
                .HasIndex(ci => ci.WorkDate);

            // SupportingDocument indexes
            modelBuilder.Entity<SupportingDocument>()
                .HasIndex(sd => sd.ClaimId);

            modelBuilder.Entity<SupportingDocument>()
                .HasIndex(sd => sd.DocumentType);

            // User indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserType);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.LecturerId);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.CoordinatorId);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.ManagerId);
        }
    }
}