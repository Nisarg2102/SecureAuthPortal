using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureAuthPortal.Data;
using System.Text;

namespace SecureAuthPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ErrorLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ErrorLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/ErrorLog/Index
        public async Task<IActionResult> Index(
            string searchUsername = "",
            string searchController = "",
            string dateFrom = "",
            string dateTo = "",
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.ErrorLog.AsQueryable();

            if (!string.IsNullOrEmpty(searchUsername))
                query = query.Where(l => l.Username.Contains(searchUsername));

            if (!string.IsNullOrEmpty(searchController))
                query = query.Where(l => l.Controller.Contains(searchController));

            if (DateTime.TryParse(dateFrom, out var fromDate))
                query = query.Where(l => l.Timestamp >= fromDate);

            if (DateTime.TryParse(dateTo, out var toDate))
                query = query.Where(l => l.Timestamp <= toDate.AddDays(1));

            int totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.SearchUsername = searchUsername;
            ViewBag.SearchController = searchController;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            return View(logs);
        }

        // GET: /Admin/ErrorLog/Export — CSV export
        public async Task<IActionResult> Export(
            string searchUsername = "",
            string searchController = "",
            string dateFrom = "",
            string dateTo = "")
        {
            var query = _context.ErrorLog.AsQueryable();

            if (!string.IsNullOrEmpty(searchUsername))
                query = query.Where(l => l.Username.Contains(searchUsername));
            if (!string.IsNullOrEmpty(searchController))
                query = query.Where(l => l.Controller.Contains(searchController));
            if (DateTime.TryParse(dateFrom, out var fromDate))
                query = query.Where(l => l.Timestamp >= fromDate);
            if (DateTime.TryParse(dateTo, out var toDate))
                query = query.Where(l => l.Timestamp <= toDate.AddDays(1));

            var logs = await query.OrderByDescending(l => l.Timestamp).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,Username,ErrorMessage,Controller,Action,Timestamp,IpAddress");
            foreach (var log in logs)
            {
                sb.AppendLine($"{log.Id},\"{log.Username}\",\"{log.ErrorMessage?.Replace("\"", "\"\"")}\",\"{log.Controller}\",\"{log.Action}\",\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.IpAddress}\"");
            }

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"ErrorLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // POST: /Admin/ErrorLog/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            _context.ErrorLog.RemoveRange(_context.ErrorLog);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Error log cleared successfully.";
            return RedirectToAction("Index");
        }
    }
}
