using System.IO.Compression;
using System.Security.Claims;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "AcademicManager,Administrator")]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManagerController> _logger;

        public ManagerController(ApplicationDbContext context, ILogger<ManagerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                // Get claims first, then calculate average processing days in memory
                var approvedClaims = await _context.MonthlyClaims
                    .Include(mc => mc.Lecturer)
                    .Include(mc => mc.Coordinator)
                    .Where(mc => mc.Status == ClaimStatus.ManagerApproved &&
                                 mc.ManagerApprovalDate.HasValue &&
                                 mc.CoordinatorApprovalDate.HasValue)
                    .ToListAsync();

                double averageProcessingDays = 0;
                if (approvedClaims.Any())
                {
                    averageProcessingDays = approvedClaims
                        .Average(mc => (mc.ManagerApprovalDate.Value - mc.CoordinatorApprovalDate.Value).TotalDays);
                }

                var viewModel = new ManagerDashboardViewModel
                {
                    PendingApprovalClaims = await _context.MonthlyClaims
                        .Include(mc => mc.Lecturer)
                        .Include(mc => mc.Coordinator)
                        .Where(mc => mc.Status == ClaimStatus.CoordinatorApproved)
                        .OrderBy(mc => mc.CoordinatorApprovalDate)
                        .ToListAsync(),

                    RecentlyProcessedClaims = await _context.MonthlyClaims
                        .Include(mc => mc.Lecturer)
                        .Where(mc => mc.Status == ClaimStatus.ManagerApproved || mc.Status == ClaimStatus.Rejected)
                        .OrderByDescending(mc => mc.ManagerApprovalDate)
                        .Take(10)
                        .ToListAsync(),

                    PendingApprovalCount = await _context.MonthlyClaims
                        .CountAsync(mc => mc.Status == ClaimStatus.CoordinatorApproved),

                    ApprovedThisMonthCount = await _context.MonthlyClaims
                        .CountAsync(mc => mc.Status == ClaimStatus.ManagerApproved &&
                                         mc.ManagerApprovalDate.Value.Month == currentMonth &&
                                         mc.ManagerApprovalDate.Value.Year == currentYear),

                    RejectedThisMonthCount = await _context.MonthlyClaims
                        .CountAsync(mc => mc.Status == ClaimStatus.Rejected &&
                                         mc.ManagerApprovalDate.HasValue &&
                                         mc.ManagerApprovalDate.Value.Month == currentMonth &&
                                         mc.ManagerApprovalDate.Value.Year == currentYear),

                    TotalLecturersCount = await _context.Lecturers.CountAsync(),

                    TotalAmountApproved = await _context.MonthlyClaims
                        .Where(mc => mc.Status == ClaimStatus.ManagerApproved &&
                                    mc.ManagerApprovalDate.HasValue &&
                                    mc.ManagerApprovalDate.Value.Month == currentMonth &&
                                    mc.ManagerApprovalDate.Value.Year == currentYear)
                        .SumAsync(mc => mc.TotalAmount),

                    AverageProcessingDays = averageProcessingDays
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading manager dashboard");
                TempData["Error"] = "An error occurred while loading the dashboard.";
                return View(new ManagerDashboardViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ClaimDetails(int id)
        {
            try
            {
                var claim = await _context.MonthlyClaims
                    .Include(mc => mc.Lecturer)
                    .Include(mc => mc.Coordinator)
                    .Include(mc => mc.SupportingDocuments)
                    .FirstOrDefaultAsync(mc => mc.ClaimId == id);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (claim.Status != ClaimStatus.CoordinatorApproved)
                {
                    TempData["Error"] = "This claim is not ready for final approval.";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessFinalApproval(int claimId, bool isApproved, string notes)
        {
            try
            {
                var claim = await _context.MonthlyClaims
                    .Include(mc => mc.Lecturer)
                    .FirstOrDefaultAsync(mc => mc.ClaimId == claimId);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (claim.Status != ClaimStatus.CoordinatorApproved)
                {
                    TempData["Error"] = "This claim is not ready for final approval.";
                    return RedirectToAction(nameof(Index));
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = await _context.Users
                    .Include(u => u.Manager)
                    .FirstOrDefaultAsync(u => u.Id.ToString() == currentUserId);

                if (currentUser?.Manager == null)
                {
                    TempData["Error"] = "Manager profile not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (isApproved)
                {
                    claim.Status = ClaimStatus.ManagerApproved;
                    claim.ManagerApprovalDate = DateTime.Now;
                    claim.ManagerId = currentUser.Manager.ManagerId;
                    claim.ManagerComments = notes;

                    TempData["Success"] = $"Claim for {claim.DisplayMonth} by {claim.Lecturer?.FullName} has been finally approved.";
                }
                else
                {
                    claim.Status = ClaimStatus.Rejected;
                    claim.ManagerApprovalDate = DateTime.Now;
                    claim.ManagerId = currentUser.Manager.ManagerId;
                    claim.ManagerComments = notes;

                    TempData["Warning"] = $"Claim for {claim.DisplayMonth} by {claim.Lecturer?.FullName} has been finally rejected.";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Manager {ManagerId} {action} claim {ClaimId}",
                    currentUser.Manager.ManagerId, isApproved ? "finally approved" : "finally rejected", claimId);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing final approval for claim {ClaimId}", claimId);
                TempData["Error"] = "An error occurred while processing the final approval.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            var document = await _context.SupportingDocuments
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);

            if (document == null || document.FileData == null)
            {
                return NotFound();
            }

            return File(document.FileData, document.ContentType, document.OriginalFileName);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAllDocuments(int claimId)
        {
            var claim = await _context.MonthlyClaims
                .Include(c => c.SupportingDocuments)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim?.SupportingDocuments?.Any() != true)
            {
                TempData["Error"] = "No documents found for this claim.";
                return RedirectToAction(nameof(ClaimDetails), new { id = claimId });
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var document in claim.SupportingDocuments)
                    {
                        if (document.FileData != null && document.FileData.Length > 0)
                        {
                            var entry = archive.CreateEntry(document.OriginalFileName, CompressionLevel.Fastest);
                            using (var entryStream = entry.Open())
                            using (var fileStream = new MemoryStream(document.FileData))
                            {
                                await fileStream.CopyToAsync(entryStream);
                            }
                        }
                    }
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                return File(memoryStream.ToArray(), "application/zip", $"Claim-{claimId}-Documents.zip");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProcessedClaims(string status, string month, string lecturer)
        {
            try
            {
                // Start with base query
                var query = _context.MonthlyClaims
                    .Include(mc => mc.Lecturer)
                    .Include(mc => mc.Coordinator)
                    .Where(mc => mc.Status == ClaimStatus.ManagerApproved || mc.Status == ClaimStatus.Rejected)
                    .AsQueryable();

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    if (status == "ManagerApproved")
                    {
                        query = query.Where(mc => mc.Status == ClaimStatus.ManagerApproved);
                    }
                    else if (status == "Rejected")
                    {
                        query = query.Where(mc => mc.Status == ClaimStatus.Rejected);
                    }
                }

                // Apply month filter
                if (!string.IsNullOrEmpty(month))
                {
                    var filterDate = DateTime.Parse(month + "-01");
                    query = query.Where(mc => mc.ClaimMonth.Year == filterDate.Year &&
                                             mc.ClaimMonth.Month == filterDate.Month);
                }

                // Apply lecturer filter
                if (!string.IsNullOrEmpty(lecturer))
                {
                    query = query.Where(mc => mc.Lecturer.FirstName.Contains(lecturer) ||
                                             mc.Lecturer.LastName.Contains(lecturer) ||
                                             mc.Lecturer.EmployeeNumber.Contains(lecturer));
                }

                // Order by decision date (most recent first)
                var processedClaims = await query
                    .OrderByDescending(mc => mc.ManagerApprovalDate)
                    .ToListAsync();

                // Pass filter values to view for display
                ViewBag.CurrentStatus = status;
                ViewBag.CurrentMonth = month;
                ViewBag.CurrentLecturer = lecturer;

                return View(processedClaims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading processed claims with filters: Status={Status}, Month={Month}, Lecturer={Lecturer}",
                    status, month, lecturer);
                TempData["Error"] = "An error occurred while loading processed claims.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProcessedClaimDetails(int id)
        {
            try
            {
                var claim = await _context.MonthlyClaims
                    .Include(mc => mc.Lecturer)
                    .Include(mc => mc.Coordinator)
                    .Include(mc => mc.Manager)
                    .Include(mc => mc.SupportingDocuments)
                    .Include(mc => mc.ClaimItems)
                    .FirstOrDefaultAsync(mc => mc.ClaimId == id);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(ProcessedClaims));
                }

                // Only allow viewing of processed claims (approved or rejected)
                if (claim.Status != ClaimStatus.ManagerApproved && claim.Status != ClaimStatus.Rejected)
                {
                    TempData["Error"] = "This claim has not been finally processed yet.";
                    return RedirectToAction(nameof(ProcessedClaims));
                }

                return View(claim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading processed claim details for claim {ClaimId}", id);
                TempData["Error"] = "An error occurred while loading claim details.";
                return RedirectToAction(nameof(ProcessedClaims));
            }
        }
    }
}