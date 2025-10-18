using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class SubmitClaimViewModel
    {
        [Required(ErrorMessage = "Please select a month")]
        [Display(Name = "Claim Month")]
        public string ClaimMonth { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter total hours")]
        [Range(0.5, 200, ErrorMessage = "Hours must be between 0.5 and 200")]
        [Display(Name = "Total Hours")]
        public decimal TotalHours { get; set; }

        [Display(Name = "Supporting Documents")]
        public List<IFormFile>? SupportingDocuments { get; set; }

        public List<string> AvailableMonths { get; set; } = new List<string>();
    }
}
