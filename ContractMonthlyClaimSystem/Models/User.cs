using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ContractMonthlyClaimSystem.Models
{
    public class User : IdentityUser<int>
    {

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        public UserType UserType { get; set; }

        // Foreign keys for specific user types
        [ForeignKey("Lecturer")]
        public int? LecturerId { get; set; }

        [ForeignKey("Coordinator")]
        public int? CoordinatorId { get; set; }

        [ForeignKey("Manager")]
        public int? ManagerId { get; set; }

        // Navigation properties
        public virtual Lecturer Lecturer { get; set; }
        public virtual ProgrammeCoordinator Coordinator { get; set; }
        public virtual AcademicManager Manager { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    public enum UserType
    {
        Lecturer = 1,
        ProgrammeCoordinator = 2,
        AcademicManager = 3,
        Administrator = 4
    }
}
