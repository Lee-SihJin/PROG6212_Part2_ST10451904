// Controllers/LecturerController.cs
using System.ComponentModel.DataAnnotations;
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

        // Update the SubmitClaim method to handle modal-based documents
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(SubmitClaimViewModel model, string submissionType, List<IFormFile> supportingDocuments)
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

                // Determine status based on submission type
                var status = submissionType == "Draft" ? ClaimStatus.Draft : ClaimStatus.Submitted;

                var monthlyClaim = new MonthlyClaim
                {
                    LecturerId = currentUser.Lecturer.LecturerId,
                    ClaimMonth = DateTime.Parse(model.ClaimMonth + "-01"),
                    SubmissionDate = DateTime.Now,
                    TotalHours = model.TotalHours,
                    TotalAmount = model.TotalHours * currentUser.Lecturer.HourlyRate,
                    Status = status
                };

                _context.MonthlyClaims.Add(monthlyClaim);
                await _context.SaveChangesAsync();

                var latestClaim = await _context.MonthlyClaims
                    .Where(mc => mc.LecturerId == currentUser.Lecturer.LecturerId)
                    .OrderByDescending(mc => mc.ClaimId) // Or use SubmissionDate if you prefer
                    .FirstOrDefaultAsync();

                // Handle file uploads from modal
                if (supportingDocuments != null && supportingDocuments.Count > 0)
                {
                    for (int i = 0; i < supportingDocuments.Count; i++)
                    {
                        var file = supportingDocuments[i];
                        if (file.Length > 0)
                        {
                            // Get document type and description
                            var documentType = Request.Form[$"DocumentTypes[{i}]"].FirstOrDefault();
                            var documentDescription = Request.Form[$"DocumentDescriptions[{i}]"].FirstOrDefault();

                            // Read file into byte array
                            using (var memoryStream = new MemoryStream())
                            {
                                await file.CopyToAsync(memoryStream);
                                var fileData = memoryStream.ToArray();

                                // Create supporting document record with file data
                                var supportingDocument = new SupportingDocument
                                {
                                    ClaimId = latestClaim.ClaimId,
                                    FileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName),
                                    OriginalFileName = file.FileName,
                                    DocumentType = documentType != null ? (DocumentType)int.Parse(documentType) : DocumentType.Other,
                                    FileData = fileData, // Store file in database
                                    FileSize = file.Length,
                                    ContentType = file.ContentType,
                                    Description = documentDescription,
                                    UploadDate = DateTime.Now
                                };

                                _context.SupportingDocuments.Add(supportingDocument);
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                var actionMessage = status == ClaimStatus.Draft ? "saved as draft" : "submitted";
                var documentMessage = supportingDocuments?.Count > 0 ? $" with {supportingDocuments.Count} supporting document(s)" : "";

                TempData["Success"] = $"Claim for {model.ClaimMonth} {actionMessage} successfully{documentMessage}!";

                _logger.LogInformation("Lecturer {LecturerId} {action} claim for {Month} with {DocumentCount} documents",
                    currentUser.Lecturer.LecturerId, actionMessage, model.ClaimMonth, supportingDocuments?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting claim");
                TempData["Error"] = "An error occurred while processing the claim.";
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

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            var document = await _context.SupportingDocuments
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);

            if (document == null || document.FileData == null)
            {
                return NotFound();
            }

            // Return the file from database
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDraft(SubmitClaimViewModel model, string action,
    List<IFormFile> NewSupportingDocuments, List<int> DocumentsToDelete, int id)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors below.";
                return RedirectToAction(nameof(EditDraft), new { id = id });
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

                // Get the existing claim
                var existingClaim = await _context.MonthlyClaims
                    .Include(mc => mc.SupportingDocuments)
                    .FirstOrDefaultAsync(mc => mc.ClaimId == id && mc.LecturerId == currentUser.Lecturer.LecturerId);

                if (existingClaim == null)
                {
                    TempData["Error"] = "Claim not found or access denied.";
                    return RedirectToAction(nameof(Index));
                }

                if (existingClaim.Status != ClaimStatus.Draft)
                {
                    TempData["Error"] = "Only draft claims can be edited.";
                    return RedirectToAction(nameof(ClaimDetails), new { id = id });
                }

                // Update claim details
                existingClaim.TotalHours = model.TotalHours;
                existingClaim.TotalAmount = model.TotalHours * currentUser.Lecturer.HourlyRate;

                // Determine new status based on action
                if (action == "submit")
                {
                    existingClaim.Status = ClaimStatus.Submitted;
                    existingClaim.SubmissionDate = DateTime.Now; // Update submission date when submitting
                }
                else
                {
                    existingClaim.Status = ClaimStatus.Draft;
                }

                // Handle document deletions
                if (DocumentsToDelete != null && DocumentsToDelete.Any())
                {
                    var documentsToRemove = await _context.SupportingDocuments
                        .Where(d => DocumentsToDelete.Contains(d.DocumentId) && d.ClaimId == existingClaim.ClaimId)
                        .ToListAsync();

                    _context.SupportingDocuments.RemoveRange(documentsToRemove);
                }

                // Handle new document uploads
                if (NewSupportingDocuments != null && NewSupportingDocuments.Count > 0)
                {
                    var newDocumentTypes = Request.Form["NewDocumentTypes"].ToList();
                    var newDocumentDescriptions = Request.Form["NewDocumentDescriptions"].ToList();

                    for (int i = 0; i < NewSupportingDocuments.Count; i++)
                    {
                        var file = NewSupportingDocuments[i];
                        if (file.Length > 0)
                        {
                            // Get document type and description
                            var documentType = i < newDocumentTypes.Count ? newDocumentTypes[i] : null;
                            var documentDescription = i < newDocumentDescriptions.Count ? newDocumentDescriptions[i] : null;

                            // Read file into byte array
                            using (var memoryStream = new MemoryStream())
                            {
                                await file.CopyToAsync(memoryStream);
                                var fileData = memoryStream.ToArray();

                                // Create supporting document record with file data
                                var supportingDocument = new SupportingDocument
                                {
                                    ClaimId = existingClaim.ClaimId,
                                    FileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName),
                                    OriginalFileName = file.FileName,
                                    DocumentType = documentType != null ? (DocumentType)int.Parse(documentType) : DocumentType.Other,
                                    FileData = fileData,
                                    FileSize = file.Length,
                                    ContentType = file.ContentType,
                                    Description = documentDescription,
                                    UploadDate = DateTime.Now
                                };

                                _context.SupportingDocuments.Add(supportingDocument);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                var actionMessage = action == "submit" ? "submitted" : "updated";
                var newDocsMessage = NewSupportingDocuments?.Count > 0 ? $" with {NewSupportingDocuments.Count} new document(s)" : "";
                var deleteDocsMessage = DocumentsToDelete?.Count > 0 ? $" and removed {DocumentsToDelete.Count} document(s)" : "";

                TempData["Success"] = $"Claim for {existingClaim.DisplayMonth} {actionMessage} successfully{newDocsMessage}{deleteDocsMessage}!";

                _logger.LogInformation("Lecturer {LecturerId} {action} draft claim {ClaimId}",
                    currentUser.Lecturer.LecturerId, actionMessage, existingClaim.ClaimId);

                return RedirectToAction(nameof(ClaimDetails), new { id = existingClaim.ClaimId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating draft claim {ClaimId}", id);
                TempData["Error"] = "An error occurred while updating the claim.";
                return RedirectToAction(nameof(EditDraft), new { id = id });
            }
        }

        // Also add the EditDraft GET action if you don't have it already
        [HttpGet]
        public async Task<IActionResult> EditDraft(int id)
        {
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

                var claim = await _context.MonthlyClaims
                    .Include(mc => mc.Lecturer)
                    .Include(mc => mc.SupportingDocuments)
                    .FirstOrDefaultAsync(mc => mc.ClaimId == id && mc.LecturerId == currentUser.Lecturer.LecturerId);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found or access denied.";
                    return RedirectToAction(nameof(Index));
                }

                if (claim.Status != ClaimStatus.Draft)
                {
                    TempData["Error"] = "Only draft claims can be edited.";
                    return RedirectToAction(nameof(ClaimDetails), new { id = claim.ClaimId });
                }

                return View(claim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading draft claim for editing");
                TempData["Error"] = "An error occurred while loading the claim for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClaim(int id)
        {
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

                // Get the claim with related documents
                var claim = await _context.MonthlyClaims
                    .Include(c => c.SupportingDocuments)
                    .FirstOrDefaultAsync(c => c.ClaimId == id && c.LecturerId == currentUser.Lecturer.LecturerId);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found or access denied.";
                    return RedirectToAction(nameof(Index));
                }

                // Store claim info for success message
                var claimMonth = claim.DisplayMonth;

                // Remove supporting documents first (if any)
                if (claim.SupportingDocuments.Any())
                {
                    _context.SupportingDocuments.RemoveRange(claim.SupportingDocuments);
                }

                // Remove the claim
                _context.MonthlyClaims.Remove(claim);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Claim for {claimMonth} has been deleted successfully.";

                _logger.LogInformation("Lecturer {LecturerId} deleted draft claim {ClaimId} for {ClaimMonth}",
                    currentUser.Lecturer.LecturerId, id, claimMonth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting claim {ClaimId}", id);
                TempData["Error"] = "An error occurred while deleting the claim.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}