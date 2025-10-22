using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    public class ClaimItem
    {
        [Key]
        public int ItemId { get; set; }

        [Required]
        public int ClaimId { get; set; }

        [Required]
        public DateTime WorkDate { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        [Range(0.5, 24, ErrorMessage = "Hours must be between 0.5 and 24")]
        public decimal HoursWorked { get; set; }

        public decimal HourlyRate { get; set; }

        public decimal TotalAmount => HoursWorked * HourlyRate;

        // Navigation properties
        public virtual MonthlyClaim MonthlyClaim { get; set; }
        
        public decimal CalculateTotalAmount()
        {
            return HoursWorked * HourlyRate;
        }
    }
}
