namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class CoordinatorDashboardViewModel
    {
        public int PendingClaimsCount { get; set; }
        public int ApprovedThisMonthCount { get; set; }
        public int TotalClaimsCount { get; set; }
        public int TotalLecturersCount { get; set; }

        public List<MonthlyClaim> PendingClaims { get; set; } = new List<MonthlyClaim>();
        public List<MonthlyClaim> RecentlyProcessedClaims { get; set; } = new List<MonthlyClaim>();
    }
}
