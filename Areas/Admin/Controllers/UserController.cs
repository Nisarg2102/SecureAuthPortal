// ============================================================
// Admin User Controller
// ============================================================
// Route: /Admin/User/List   → Show all users
// Route: /Admin/User/Edit   → Edit a user
// Route: /Admin/User/Delete → Delete a user
//
// Inherits AdminBaseController → already protected
// This replaces UserList, Edit, Delete from old AccountController
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SecureAuthPortal.Data;
using SecureAuthPortal.Models;

namespace SecureAuthPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/User/List
        // searchText = what user typed, searchBy = which column to search
        public IActionResult List(string searchText, string searchBy)
        {
            // AsQueryable() keeps the query flexible — we can keep adding
            // WHERE conditions before finally calling .ToList() to hit the DB
            var users = _context.UserMaster
                                .Include(x => x.Role)   // JOIN with RoleMaster
                                .AsQueryable();

            if (!string.IsNullOrEmpty(searchText))
            {
                switch (searchBy)
                {
                    case "FullName":
                        users = users.Where(x => x.FullName.Contains(searchText));
                        break;
                    case "Username":
                        users = users.Where(x => x.Username.Contains(searchText));
                        break;
                    case "MobileNo":
                        users = users.Where(x => x.MobileNo.Contains(searchText));
                        break;
                    case "Role":
                        users = users.Where(x => x.Role.RoleName.Contains(searchText));
                        break;
                    default:
                        users = users.Where(x =>
                            x.FullName.Contains(searchText)     ||
                            x.Username.Contains(searchText)     ||
                            x.MobileNo.Contains(searchText)     ||
                            x.Role.RoleName.Contains(searchText));
                        break;
                }
            }

            ViewBag.SearchText = searchText;
            ViewBag.SearchBy   = searchBy;

            return View(users.ToList()); // Execute the SQL query NOW
        }

        // GET: /Admin/User/Edit/5
        public IActionResult Edit(long id)
        {
            var user = _context.UserMaster.Find(id);
            if (user == null) return NotFound();

            // Populate the Role dropdown
            ViewBag.Roles = new SelectList(
                _context.RoleMaster.ToList(), "RoleId", "RoleName", user.RoleId);

            return View(user);
        }

        // POST: /Admin/User/Edit
        [HttpPost]
        public IActionResult Edit(UserMaster model)
        {
            _context.UserMaster.Update(model);
            _context.SaveChanges();

            TempData["Success"] = "User updated successfully!";
            return RedirectToAction("List");
        }

        // GET: /Admin/User/Delete/5
        public IActionResult Delete(long id)
        {
            var user = _context.UserMaster.Find(id);
            if (user != null)
            {
                _context.UserMaster.Remove(user);
                _context.SaveChanges();
                TempData["Success"] = "User deleted successfully!";
            }
            return RedirectToAction("List");
        }
    }
}
