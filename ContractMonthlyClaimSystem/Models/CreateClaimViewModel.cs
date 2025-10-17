using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    public class CreateClaimViewModel
    {
        [Required]
        [StringLength(7)]
        public string ClaimMonth { get; set; }

        public List<ClaimItemViewModel> ClaimItems { get; set; } = new List<ClaimItemViewModel>();
    }

    public class ClaimItemViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime WorkDate { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        [Range(0.5, 24)]
        public decimal HoursWorked { get; set; }
    }

    public class MonthlyClaimViewModel
    {
        public int ClaimId { get; set; }
        public string LecturerName { get; set; }
        public string ClaimMonth { get; set; }
        public string DisplayMonth { get; set; }
        public DateTime SubmissionDate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public bool CanEdit { get; set; }
        public List<ClaimItemViewModel> ClaimItems { get; set; }
        public List<DocumentViewModel> SupportingDocuments { get; set; }
    }

    public class DocumentViewModel
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; }
        public string OriginalFileName { get; set; }
        public string DocumentType { get; set; }
        public string FileSizeDisplay { get; set; }
        public DateTime UploadDate { get; set; }
        public string Description { get; set; }
    }
}
