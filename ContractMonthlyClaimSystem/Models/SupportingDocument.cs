using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    public enum DocumentType
    {
        Timesheet = 0,
        Invoice = 1,
        Receipt = 2,
        Contract = 3,
        Other = 4
    }

    public class SupportingDocument
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        public int ClaimId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(100)]
        public string OriginalFileName { get; set; }

        [Required]
        public DocumentType DocumentType { get; set; }

        // Add the BLOB field
        public byte[]? FileData { get; set; }

        public long FileSize { get; set; } // in bytes

        [Required]
        public string ContentType { get; set; }

        [Required]
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public virtual MonthlyClaim MonthlyClaim { get; set; }

        // Computed properties
        public string FileSizeDisplay => FileSize < 1024 ? $"{FileSize} B" :
                                       FileSize < 1048576 ? $"{FileSize / 1024.0:0.00} KB" :
                                       $"{FileSize / 1048576.0:0.00} MB";
    }

}
