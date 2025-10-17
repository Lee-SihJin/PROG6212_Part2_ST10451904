// Controllers/LecturerController.cs
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "Lecturer,Administrator")]
    public class LecturerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LecturerController> _logger;

        public LecturerController(ApplicationDbContext context, ILogger<LecturerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Lecturer Dashboard
        public async Task<IActionResult> Index()
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = await _context.Users
                    .Include(u => u.Lecturer)
                    .FirstOrDefaultAsync(u => u.Id.ToString() == currentUserId);

                if (currentUser?.Lecturer == null)
                {
                    TempData["Error"] = "Lecturer profile not found. Please contact administrator.";
                    return View(new LecturerDashboardViewModel());
                }

                var lecturerId = currentUser.Lecturer.LecturerId;

                var viewModel = new LecturerDashboardViewModel
                {
                    Lecturer = currentUser.Lecturer,
                    RecentClaims = await _context.MonthlyClaims
                    .Include(mc => mc.ClaimItems)
                    .Include(mc => mc.Coordinator)
                    .Include(mc => mc.Manager)
                    .Where(mc => mc.LecturerId == lecturerId)
                    .OrderByDescending(mc => mc.ClaimMonth)
                    .Take(10)
                    .ToListAsync(),
                    CurrentMonth = DateTime.Now.ToString("yyyy-MM")
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lecturer dashboard");
                TempData["Error"] = "An error occurred while loading the dashboard.";
                return View(new LecturerDashboardViewModel());
            }
        }

        // POST: Submit New Claim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(SubmitClaimViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors below.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = await _context.Users
                    .Include(u => u.Lecturer)
                    .FirstOrDefaultAsync(u => u.Id.ToString() == currentUserId);

                if (currentUser?.Lecturer == null)
                {
                    TempData["Error"] = "Lecturer profile not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if claim already exists for this month
                var existingClaim = await _context.MonthlyClaims
                    .FirstOrDefaultAsync(mc => mc.LecturerId == currentUser.Lecturer.LecturerId &&
                                              mc.ClaimMonth == DateTime.Parse(model.ClaimMonth + "-01"));

                if (existingClaim != null)
                {
                    TempData["Error"] = "A claim already exists for the selected month.";
                    return RedirectToAction(nameof(Index));
                }

                var monthlyClaim = new MonthlyClaim
                {
                    LecturerId = currentUser.Lecturer.LecturerId,
                    ClaimMonth = DateTime.Parse(model.ClaimMonth + "-01"),
                    SubmissionDate = DateTime.Now,
                    TotalHours = model.TotalHours,
                    TotalAmount = model.TotalHours * currentUser.Lecturer.HourlyRate,
                    Status = ClaimStatus.Submitted
                };

                _context.MonthlyClaims.Add(monthlyClaim);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Claim for {model.ClaimMonth} submitted successfully!";
                _logger.LogInformation("Lecturer {LecturerId} submitted claim for {Month}", currentUser.Lecturer.LecturerId, model.ClaimMonth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting claim");
                TempData["Error"] = "An error occurred while submitting the claim.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Claim Details
        public async Task<IActionResult> ClaimDetails(int id)
        {
            try
            {
                var claim = await _context.MonthlyClaims
                    .Include(mc => mc.Lecturer)
                    .Include(mc => mc.ClaimItems)
                    .Include(mc => mc.SupportingDocuments)
                    .Include(mc => mc.Coordinator)
                    .Include(mc => mc.Manager)
                    .FirstOrDefaultAsync(mc => mc.ClaimId == id);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Verify the claim belongs to the current lecturer
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = await _context.Users
                    .Include(u => u.Lecturer)
                    .FirstOrDefaultAsync(u => u.Id.ToString() == currentUserId);

                if (currentUser?.Lecturer?.LecturerId != claim.LecturerId && !User.IsInRole("Administrator"))
                {
                    TempData["Error"] = "Access denied.";
                    return RedirectToAction(nameof(Index));
                }

                return View(claim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading claim details");
                TempData["Error"] = "An error occurred while loading claim details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Create Draft Claim
        public IActionResult CreateDraft()
        {
            var model = new SubmitClaimViewModel
            {
                AvailableMonths = GetAvailableMonths()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDraft(SubmitClaimViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableMonths = GetAvailableMonths();
                return View(model);
            }

            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = await _context.Users
                    .Include(u => u.Lecturer)
                    .FirstOrDefaultAsync(u => u.Id.ToString() == currentUserId);

                if (currentUser?.Lecturer == null)
                {
                    TempData["Error"] = "Lecturer profile not found.";
                    return RedirectToAction(nameof(Index));
                }

                var monthlyClaim = new MonthlyClaim
                {
                    LecturerId = currentUser.Lecturer.LecturerId,
                    ClaimMonth = DateTime.Parse(model.ClaimMonth + "-01"),
                    SubmissionDate = DateTime.Now,
                    TotalHours = model.TotalHours,
                    TotalAmount = model.TotalHours * currentUser.Lecturer.HourlyRate,
                    Status = ClaimStatus.Draft
                };

                _context.MonthlyClaims.Add(monthlyClaim);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Draft claim for {model.ClaimMonth} created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating draft claim");
                TempData["Error"] = "An error occurred while creating the draft claim.";
                model.AvailableMonths = GetAvailableMonths();
                return View(model);
            }
        }

        private List<string> GetAvailableMonths()
        {
            var months = new List<string>();
            var currentDate = DateTime.Now;

            // Allow claims for current month and previous 6 months
            for (int i = 0; i < 6; i++)
            {
                var date = currentDate.AddMonths(-i);
                months.Add(date.ToString("yyyy-MM"));
            }

            return months;
        }
    }

    // View Models
    public class LecturerDashboardViewModel
    {
        public Lecturer? Lecturer { get; set; }
        public List<MonthlyClaim> RecentClaims { get; set; } = new List<MonthlyClaim>();
        public string CurrentMonth { get; set; } = string.Empty;
    }

    public class SubmitClaimViewModel
    {
        [Required(ErrorMessage = "Please select a month")]
        [Display(Name = "Claim Month")]
        public string ClaimMonth { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter total hours")]
        [Range(0.5, 200, ErrorMessage = "Hours must be between 0.5 and 200")]
        [Display(Name = "Total Hours")]
        public decimal TotalHours { get; set; }

        public List<string> AvailableMonths { get; set; } = new List<string>();
    }
}