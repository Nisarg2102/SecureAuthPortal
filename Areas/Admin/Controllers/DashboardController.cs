using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureAuthPortal.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SecureAuthPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Dashboard", "Account", new { area = "" });
            }

            ViewBag.TotalUsers = await _context.UserMaster.CountAsync();
            ViewBag.TotalAdmins = await _context.UserMaster.CountAsync(u => u.Role.RoleName == "Admin");
            ViewBag.TotalDocuments = await _context.DocumentMaster.CountAsync();
            ViewBag.PendingDocuments = await _context.DocumentMaster.CountAsync(d => d.Status == "Pending");

            ViewBag.RecentUsers = await _context.UserMaster
                .OrderByDescending(u => u.CreatedDate)
                .Take(5)
                .ToListAsync();

            ViewBag.PendingDocumentsList = await _context.DocumentMaster
                .Where(d => d.Status == "Pending")
                .OrderByDescending(d => d.UploadDate)
                .Include(d => d.User)
                .Take(5)
                .ToListAsync();

            var userRegistrationData = await _context.UserMaster
                .Where(u => u.CreatedDate >= DateTime.Now.AddDays(-7))
                .GroupBy(u => u.CreatedDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.UserRegistrationChartData = userRegistrationData;

            return View();
        }
    }
}
