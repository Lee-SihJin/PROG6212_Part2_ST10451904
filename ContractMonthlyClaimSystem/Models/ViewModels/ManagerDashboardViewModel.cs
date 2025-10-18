using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ManagerDashboardViewModel
    {
        public int PendingApprovalCount { get; set; }
        public int ApprovedThisMonthCount { get; set; }
        public int RejectedThisMonthCount { get; set; }
        public int TotalLecturersCount { get; set; }
        public decimal TotalAmountApproved { get; set; }
        public double AverageProcessingDays { get; set; }

        public List<MonthlyClaim> PendingApprovalClaims { get; set; } = new List<MonthlyClaim>();
        public List<MonthlyClaim> RecentlyProcessedClaims { get; set; } = new List<MonthlyClaim>();
    }
}