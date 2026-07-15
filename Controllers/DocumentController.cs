using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureAuthPortal.Data;
using SecureAuthPortal.Models;
using SecureAuthPortal.Services;
using Microsoft.AspNetCore.Authorization;

namespace SecureAuthPortal.Controllers
{
    /// <summary>
    /// Document Management Controller
    /// Handles document upload (Aadhar, PAN, Other) and admin approval
    /// 
    /// User Side: Upload documents
    /// Admin Side: Review and approve/reject documents
    /// </summary>
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogService _logService;

        public DocumentController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            ILogService logService)
        {
            _context = context;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _logService = logService;
        }

        // ==================== USER SIDE ====================

        /// <summary>
        /// GET: User View - List their own documents
        /// </summary>
        public IActionResult MyDocuments()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login", "Account");

            return RedirectToAction("Upload");
        }

        /// <summary>
        /// GET: Upload document form
        /// </summary>
        public async Task<IActionResult> Upload()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login", "Account");

            long.TryParse(HttpContext.Session.GetString("UserId"), out long userId);
            ViewBag.Documents = await _context.DocumentMaster
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();

            return View();
        }

        /// <summary>
        /// POST: Upload document
        /// Validates file type, size, and saves to server
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, string documentType)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                // ✅ VALIDATION: File selected
                if (file == null || file.Length == 0)
                {
                    TempData["Error"] = "Please select a file.";
                    return RedirectToAction("Upload");
                }

                // ✅ VALIDATION: Document type selected
                if (string.IsNullOrWhiteSpace(documentType))
                {
                    TempData["Error"] = "Please select a document type.";
                    return RedirectToAction("Upload");
                }

                // ✅ VALIDATION: File extension
                var allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions")
                    .Get<string[]>() ?? new[] { ".pdf" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}";
                    return RedirectToAction("Upload");
                }

                // ✅ VALIDATION: File size
                var maxFileSize = (_configuration.GetValue<int?>("FileUpload:MaxFileSizeInMB") ?? 5) * 1024 * 1024;
                if (file.Length > maxFileSize)
                {
                    TempData["Error"] = $"File size exceeds {maxFileSize / (1024 * 1024)} MB limit.";
                    return RedirectToAction("Upload");
                }

                // ✅ VALIDATION: Strict PDF magic byte check (prevents renamed files)
                // PDF files always start with the 4-byte signature: %PDF (0x25 0x50 0x44 0x46)
                byte[] pdfMagic = new byte[4];
                using (var peekStream = file.OpenReadStream())
                {
                    int bytesRead = await peekStream.ReadAsync(pdfMagic, 0, 4);
                    if (bytesRead < 4 ||
                        pdfMagic[0] != 0x25 ||  // %
                        pdfMagic[1] != 0x50 ||  // P
                        pdfMagic[2] != 0x44 ||  // D
                        pdfMagic[3] != 0x46)    // F
                    {
                        TempData["Error"] = "Invalid file. The uploaded file is not a real PDF document.";
                        return RedirectToAction("Upload");
                    }
                }

                // Read file bytes into memory
                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }

                // Save to database (file stored as bytes)
                var document = new DocumentMaster
                {
                    UserId = userId,
                    DocumentType = documentType,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileData = fileBytes,
                    Status = "Pending",
                    UploadDate = DateTime.Now
                };

                _context.DocumentMaster.Add(document);
                await _context.SaveChangesAsync();

                // Update DocumentPath now that we have the ID
                document.DocumentPath = $"/Document/Download/{document.DocumentId}";
                await _context.SaveChangesAsync();

                string uploader = HttpContext.Session.GetString("Username") ?? "User";
                await _logService.LogActivityAsync(uploader, "User", "Document Upload", $"'{uploader}' uploaded document '{file.FileName}' ({documentType}).", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Success");
                TempData["Success"] = "Document uploaded successfully! Waiting for admin approval.";
                return RedirectToAction("Upload");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error uploading file: {ex.Message}";
                return RedirectToAction("Upload");
            }
        }

        /// <summary>
        /// POST: Delete document (only by owner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                {
                    return Unauthorized();
                }

                var document = await _context.DocumentMaster.FindAsync(id);
                if (document == null)
                    return NotFound();

                // ✅ Check ownership
                if (document.UserId != userId)
                    return Unauthorized();

                // ✅ Check status (can only delete if not approved)
                if (document.Status == "Approved")
                {
                    TempData["Error"] = "Cannot delete approved documents";
                    return RedirectToAction("Upload");
                }

                _context.DocumentMaster.Remove(document);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Document deleted successfully.";
                return RedirectToAction("Upload");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting document: {ex.Message}";
                return RedirectToAction("Upload");
            }
        }

        // ==================== DOWNLOAD ====================

        // GET: /Document/Download/5  — works for both user (own docs) and admin
        public async Task<IActionResult> Download(long id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var role   = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var doc = await _context.DocumentMaster.FindAsync(id);
            if (doc == null) return NotFound();

            // Admin can download any doc; user can only download their own
            if (role != "Admin" && doc.UserId.ToString() != userId)
                return Unauthorized();

            if (doc.FileData == null || doc.FileData.Length == 0)
                return NotFound("File data not found in database.");

            string downloader = HttpContext.Session.GetString("Username") ?? "Unknown";
            string dlRole = HttpContext.Session.GetString("Role") ?? "User";
            await _logService.LogActivityAsync(downloader, dlRole, "Document Download", $"'{downloader}' downloaded document '{doc.FileName}'.", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Success");

            var contentType = string.IsNullOrEmpty(doc.ContentType)
                ? "application/octet-stream"
                : doc.ContentType;

            return File(doc.FileData, contentType, doc.FileName);
        }

        // ==================== ADMIN SIDE ====================

        /// <summary>
        /// GET: Admin - List all pending documents for approval
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PendingApprovals()
        {
            try
            {
                var documents = await _context.DocumentMaster
                    .Where(d => d.Status == "Pending")
                    .Include(d => d.User)
                    .OrderBy(d => d.UploadDate)
                    .ToListAsync();

                return View(documents);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading documents: {ex.Message}";
                return View(new List<DocumentMaster>());
            }
        }

        /// <summary>
        /// GET: Admin - View document details
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReviewDocument(long id)
        {
            try
            {
                var document = await _context.DocumentMaster
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.DocumentId == id);

                if (document == null)
                    return NotFound();

                return View(document);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading document: {ex.Message}";
                return RedirectToAction("PendingApprovals");
            }
        }

        /// <summary>
        /// POST: Admin - Approve document
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(long id, string notes)
        {
            try
            {
                var adminIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(adminIdStr, out long adminId)) return Unauthorized();

                var document = await _context.DocumentMaster.FindAsync(id);
                if (document == null)
                    return NotFound();

                document.Status = "Approved";
                document.ApprovedBy = adminId;
                document.ApprovedDate = DateTime.Now;
                document.VerificationNotes = notes;

                _context.DocumentMaster.Update(document);
                await _context.SaveChangesAsync();

                string adminName = User.Identity?.Name ?? "Admin";
                await _logService.LogActivityAsync(adminName, "Admin", "Document Approval", $"Admin '{adminName}' approved document '{document.FileName}' (ID: {id}).", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Success");

                return Json(new { success = true, message = "Document has been approved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: Admin - Reject document
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(long id, string notes)
        {
            try
            {
                var adminIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(adminIdStr, out long adminId)) return Unauthorized();

                var document = await _context.DocumentMaster.FindAsync(id);
                if (document == null)
                    return NotFound();

                document.Status = "Rejected";
                document.ApprovedBy = adminId;
                document.ApprovedDate = DateTime.Now;
                document.VerificationNotes = notes;

                _context.DocumentMaster.Update(document);
                await _context.SaveChangesAsync();

                string adminName = User.Identity?.Name ?? "Admin";
                await _logService.LogActivityAsync(adminName, "Admin", "Document Rejection", $"Admin '{adminName}' rejected document '{document.FileName}' (ID: {id}).", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Success");

                return Json(new { success = true, message = "Document has been rejected successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(long id, string status)
        {
            try
            {
                var document = await _context.DocumentMaster.FindAsync(id);
                if (document == null)
                {
                    TempData["Error"] = "Document not found.";
                    return RedirectToAction("UserList", "Account");
                }

                document.Status = status;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Document status updated to '{status}'.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating document status: {ex.Message}";
            }

            return RedirectToAction("UserList", "Account");
        }

    }
}
