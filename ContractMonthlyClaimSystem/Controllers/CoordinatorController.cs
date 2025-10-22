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
    [Authorize(Roles = "ProgrammeCoordinator,Administrator")]
    public class CoordinatorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CoordinatorController> _logger;

        public CoordinatorController(ApplicationDbContext context, ILogger<CoordinatorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = new CoordinatorDashboardViewModel
                {
                    PendingClaims = await _context.MonthlyClaims
                        .Include(mc => mc.Lecturer)
                        .Include(mc => mc.SupportingDocuments)
                        .Where(mc => mc.Status == ClaimStatus.Submitted)
                        .OrderBy(mc => mc.SubmissionDate)
                        .ToListAsync(),

                    RecentlyProcessedClaims = await _context.MonthlyClaims
                        .Include(mc => mc.Lecturer)
                        .Where(mc => mc.Status != ClaimStatus.Draft &&
                                    mc.Status != ClaimStatus.Submitted)
                        .OrderByDescending(mc => mc.CoordinatorApprovalDate)
                        .Take(10)
                        .ToListAsync(),

                    PendingClaimsCount = await _context.MonthlyClaims
                        .CountAsync(mc => mc.Status == ClaimStatus.Submitted),

                    ApprovedThisMonthCount = await _context.MonthlyClaims
                        .CountAsync(mc => mc.CoordinatorId != null && mc.Status != ClaimStatus.Rejected &&
                                         mc.CoordinatorApprovalDate.Value.Month == DateTime.Now.Month &&
                                         mc.CoordinatorApprovalDate.Value.Year == DateTime.Now.Year),

                    TotalClaimsCount = await _context.MonthlyClaims.CountAsync(),

                    TotalLecturersCount = await _context.Lecturers.CountAsync()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading coordinator dashboard");
                TempData["Error"] = "An error occurred while loading the dashboard.";
                return View(new CoordinatorDashboardViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ClaimDetails(int id)
        {
            try
            {
                var claim = await _context.MonthlyClaims
                    .Include(mc => mc.Lecturer)
                    .Include(mc => mc.SupportingDocuments)
                    .FirstOrDefaultAsync(mc => mc.ClaimId == id);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
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
        public async Task<IActionResult> ProcessClaim(int claimId, bool isApproved, string notes)
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

                if (claim.Status != ClaimStatus.Submitted)
                {
                    TempData["Error"] = "This claim has already been processed.";
                    return RedirectToAction(nameof(Index));
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = await _context.Users
                    .Include(u => u.Coordinator)
                    .FirstOrDefaultAsync(u => u.Id.ToString() == currentUserId);

                if (currentUser?.Coordinator == null)
                {
                    TempData["Error"] = "Coordinator profile not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (isApproved)
                {
                    claim.Status = ClaimStatus.CoordinatorApproved;
                    claim.CoordinatorApprovalDate = DateTime.Now;
                    claim.CoordinatorId = currentUser.Coordinator.CoordinatorId;
                    claim.CoordinatorComments = notes;

                    TempData["Success"] = $"Claim for {claim.DisplayMonth} by {claim.Lecturer?.FullName} has been approved.";
                }
                else
                {
                    claim.Status = ClaimStatus.Rejected;
                    claim.CoordinatorApprovalDate = DateTime.Now;
                    claim.CoordinatorId = currentUser.Coordinator.CoordinatorId;
                    claim.CoordinatorComments = notes;

                    TempData["Warning"] = $"Claim for {claim.DisplayMonth} by {claim.Lecturer?.FullName} has been rejected.";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Coordinator {CoordinatorId} {action} claim {ClaimId}",
                    currentUser.Coordinator.CoordinatorId, isApproved ? "approved" : "rejected", claimId);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing claim {ClaimId}", claimId);
                TempData["Error"] = "An error occurred while processing the claim.";
                return RedirectToAction(nameof(Index));
            }
        }
        [HttpGet]
        public async Task<IActionResult> ProcessedClaims()
        {
            try
            {
                var processedClaims = await _context.MonthlyClaims
                    .Include(mc => mc.Lecturer)
                    .Include(mc => mc.SupportingDocuments)
                    .Where(mc => mc.Status == ClaimStatus.CoordinatorApproved ||
                                mc.Status == ClaimStatus.Rejected ||
                                mc.Status == ClaimStatus.ManagerApproved)
                    .OrderByDescending(mc => mc.CoordinatorApprovalDate)
                    .ToListAsync();

                return View(processedClaims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading processed claims");
                TempData["Error"] = "An error occurred while loading processed claims.";
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
        public async Task<IActionResult> ProcessedClaimDetails(int id)
        {
            try
            {
                var claim = await _context.MonthlyClaims
                    .Include(mc => mc.Lecturer)
                    .Include(mc => mc.Coordinator)
                    .Include(mc => mc.Manager)
                    .Include(mc => mc.SupportingDocuments)
                    .FirstOrDefaultAsync(mc => mc.ClaimId == id);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(claim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading processed claim details");
                TempData["Error"] = "An error occurred while loading claim details.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}