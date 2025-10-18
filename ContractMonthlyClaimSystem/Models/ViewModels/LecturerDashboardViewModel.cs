namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class LecturerDashboardViewModel
    {
        public Lecturer? Lecturer { get; set; }
        public List<MonthlyClaim> RecentClaims { get; set; } = new List<MonthlyClaim>();
        public string CurrentMonth { get; set; } = string.Empty;
    }
}
