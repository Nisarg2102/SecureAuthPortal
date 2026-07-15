using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureAuthPortal.Data;
using System.Text;

namespace SecureAuthPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ActivityLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/ActivityLog/Index
        public async Task<IActionResult> Index(
            string searchUsername = "",
            string searchActivity = "",
            string searchStatus = "",
            string dateFrom = "",
            string dateTo = "",
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.ActivityLog.AsQueryable();

            if (!string.IsNullOrEmpty(searchUsername))
                query = query.Where(l => l.Username.Contains(searchUsername));

            if (!string.IsNullOrEmpty(searchActivity))
                query = query.Where(l => l.Activity.Contains(searchActivity));

            if (!string.IsNullOrEmpty(searchStatus))
                query = query.Where(l => l.Status == searchStatus);

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
            ViewBag.SearchActivity = searchActivity;
            ViewBag.SearchStatus = searchStatus;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            return View(logs);
        }

        // GET: /Admin/ActivityLog/Export  — CSV export (all filtered results, no pagination)
        public async Task<IActionResult> Export(
            string searchUsername = "",
            string searchActivity = "",
            string searchStatus = "",
            string dateFrom = "",
            string dateTo = "")
        {
            var query = _context.ActivityLog.AsQueryable();

            if (!string.IsNullOrEmpty(searchUsername))
                query = query.Where(l => l.Username.Contains(searchUsername));
            if (!string.IsNullOrEmpty(searchActivity))
                query = query.Where(l => l.Activity.Contains(searchActivity));
            if (!string.IsNullOrEmpty(searchStatus))
                query = query.Where(l => l.Status == searchStatus);
            if (DateTime.TryParse(dateFrom, out var fromDate))
                query = query.Where(l => l.Timestamp >= fromDate);
            if (DateTime.TryParse(dateTo, out var toDate))
                query = query.Where(l => l.Timestamp <= toDate.AddDays(1));

            var logs = await query.OrderByDescending(l => l.Timestamp).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,Username,Role,Activity,Description,Timestamp,IpAddress,Status");
            foreach (var log in logs)
            {
                sb.AppendLine($"{log.Id},\"{log.Username}\",\"{log.Role}\",\"{log.Activity}\",\"{log.Description?.Replace("\"", "\"\"")}\",\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.IpAddress}\",\"{log.Status}\"");
            }

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"ActivityLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // POST: /Admin/ActivityLog/Clear — clear all logs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            _context.ActivityLog.RemoveRange(_context.ActivityLog);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Activity log cleared successfully.";
            return RedirectToAction("Index");
        }
    }
}
