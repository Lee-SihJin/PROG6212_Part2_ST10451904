using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    public enum ClaimStatus
    {
        Draft = 0,
        Submitted = 1,
        CoordinatorApproved = 2,
        ManagerApproved = 3,
        Rejected = 4,
        Paid = 5
    }

    public class MonthlyClaim
    {
        [Key]
        public int ClaimId { get; set; }

        [Required]
        public int LecturerId { get; set; }

        [Required]
        public DateTime ClaimMonth { get; set; } // Store as DateTime, format in display

        [Required]
        public DateTime SubmissionDate { get; set; }

        public DateTime? CoordinatorReviewDate { get; set; }
        public DateTime? ManagerReviewDate { get; set; }
        public DateTime? CoordinatorApprovalDate { get; set; }
        public DateTime? ManagerApprovalDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }

        [Required]
        public ClaimStatus Status { get; set; } = ClaimStatus.Draft;

        [StringLength(500)]
        public string? CoordinatorComments { get; set; }

        [StringLength(500)]
        public string? ManagerComments { get; set; }

        public int? CoordinatorId { get; set; }
        public int? ManagerId { get; set; }

        // Navigation properties
        public virtual Lecturer Lecturer { get; set; }
        public virtual ProgrammeCoordinator Coordinator { get; set; }
        public virtual AcademicManager Manager { get; set; }
        public virtual ICollection<ClaimItem> ClaimItems { get; set; }
        public virtual ICollection<SupportingDocument> SupportingDocuments { get; set; }

        // Computed properties
        public string DisplayMonth => DateTime.Parse($"{ClaimMonth}-01").ToString("MMMM yyyy");
        
        // Add validation for business rules
        public bool CanBeEdited => Status == ClaimStatus.Draft;
        public bool CanBeSubmitted => Status == ClaimStatus.Draft && ClaimItems?.Any() == true;
        public bool RequiresCoordinator => Status >= ClaimStatus.Submitted;
        public bool RequiresManager => Status >= ClaimStatus.CoordinatorApproved;
    }

}
