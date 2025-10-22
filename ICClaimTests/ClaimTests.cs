using Xunit;
using ContractMonthlyClaimSystem.Models;
namespace ICClaimTests
{
    public class ClaimTests
    {
        [Fact]
        public void CalculateTotalAmount()
        {
            var claim = new ClaimItem();
            claim.HoursWorked = 20;
            claim.HourlyRate = 670;

            var getResult = claim.CalculateTotalAmount();

            Assert.Equal(13400, getResult);
        }
        [Fact]
        public void AdditionalNotes_Simulation()
        {
            var claim = new ClaimItem();
            claim.Description = "This is a test note for the claim Description.";

            var description = claim.Description;
            Assert.Equal("This is a test note for the claim Description.", description);
        }
        [Fact]
        public void FileProperties_IsStoredCorrectly()
        {
            var claim = new SupportingDocument();
            claim.FileName = "invoice.pdf";
            claim.Description = "123";

            Assert.Equal("invoice.pdf", claim.FileName);
            Assert.Equal("123", claim.Description);
        }
    }
}
