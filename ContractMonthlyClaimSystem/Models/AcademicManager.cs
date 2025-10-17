using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    public class AcademicManager
    {
        [Key]
        public int ManagerId { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<MonthlyClaim> FinalApprovedClaims { get; set; }
        public virtual User User { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}
