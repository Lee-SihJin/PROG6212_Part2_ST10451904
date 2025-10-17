using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
namespace ContractMonthlyClaimSystem.Models
{
    public class Lecturer
    {
        [Key]
        public int LecturerId { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        public string EmployeeNumber { get; set; }

        [StringLength(15)]
        public string PhoneNumber { get; set; }

        public decimal HourlyRate { get; set; }

        public DateTime ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<MonthlyClaim> MonthlyClaims { get; set; }
        public virtual User User { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}
