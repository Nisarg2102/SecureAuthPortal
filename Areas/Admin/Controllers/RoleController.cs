// ============================================================
// Admin Role Controller — Step 3 of the plan (CRUD for Roles)
// ============================================================
using Microsoft.AspNetCore.Authorization;
// Route: /Admin/Role/Index  → List all roles
// Route: /Admin/Role/Add    → POST: Add a new role
// Route: /Admin/Role/Delete → GET: Delete a role (with safety check)
//
// SAFETY CHECK: A role CANNOT be deleted if any user is assigned to it.
// This prevents orphan data in the database.
// ============================================================

using Microsoft.AspNetCore.Mvc;
using SecureAuthPortal.Data;
using SecureAuthPortal.Models;
using SecureAuthPortal.Services;

namespace SecureAuthPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogService _logService;

        public RoleController(ApplicationDbContext context, ILogService logService)
        {
            _context = context;
            _logService = logService;
        }

        // GET: /Admin/Role/Index
        public IActionResult Index()
        {
            // Load all roles AND count how many users each role has
            // We use ViewBag to pass role-user counts to the view
            var roles = _context.RoleMaster.ToList();

            // Dictionary: RoleId → UserCount
            // This lets the view show "X users" next to each role
            var userCountPerRole = _context.UserMaster
                .GroupBy(x => x.RoleId)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.UserCountPerRole = userCountPerRole;

            return View(roles);
        }

        // POST: /Admin/Role/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(string roleName)
        {
            // Validation: name must not be blank
            if (string.IsNullOrWhiteSpace(roleName))
            {
                TempData["Error"] = "Role name cannot be empty.";
                return RedirectToAction("Index");
            }

            // Validation: no duplicate role names (case-insensitive check)
            bool exists = _context.RoleMaster
                .Any(x => x.RoleName.ToLower() == roleName.Trim().ToLower());

            if (exists)
            {
                TempData["Error"] = $"Role '{roleName}' already exists.";
                return RedirectToAction("Index");
            }

            // All good — save the new role
            _context.RoleMaster.Add(new RoleMaster { RoleName = roleName.Trim(), CreatedDate = DateTime.Now });
            await _context.SaveChangesAsync();

            string admin = HttpContext.Session.GetString("Username") ?? "Admin";
            await _logService.LogActivityAsync(admin, "Admin", "Create Role", $"Admin '{admin}' created role '{roleName.Trim()}'.", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Success");

            TempData["Success"] = $"Role '{roleName.Trim()}' added successfully!";
            return RedirectToAction("Index");
        }

        // POST: /Admin/Role/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            bool hasUsers = _context.UserMaster.Any(x => x.RoleId == id);
            if (hasUsers)
            {
                TempData["Error"] = "Cannot delete this role — users are assigned to it. Reassign those users first.";
                return RedirectToAction("Index");
            }

            var role = await _context.RoleMaster.FindAsync(id);
            if (role != null)
            {
                string roleName = role.RoleName;
                _context.RoleMaster.Remove(role);
                await _context.SaveChangesAsync();
                string admin = HttpContext.Session.GetString("Username") ?? "Admin";
                await _logService.LogActivityAsync(admin, "Admin", "Delete Role", $"Admin '{admin}' deleted role '{roleName}'.", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Success");
                TempData["Success"] = $"Role '{roleName}' deleted successfully!";
            }

            return RedirectToAction("Index");
        }
    }
}
