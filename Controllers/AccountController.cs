using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SecureAuthPortal.Data;
using SecureAuthPortal.Models;
using SecureAuthPortal.ViewModels;
using SecureAuthPortal.Services;
using BCrypt.Net;
using System.Text.Json;

namespace SecureAuthPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogService _logService;

        public AccountController(ApplicationDbContext context, IConfiguration configuration, ILogService logService)
        {
            _context = context;
            _configuration = configuration;
            _logService = logService;
        }

        private async Task<bool> VerifyCaptcha(string token)
        {
            var secretKey = _configuration["RecaptchaSettings:SecretKey"];
            using var http = new HttpClient();
            var response = await http.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
                null);
            var json = await response.Content.ReadAsStringAsync();
            
            System.Diagnostics.Debug.WriteLine($"reCAPTCHA response: {json}");
            Console.WriteLine($"reCAPTCHA response: {json}");

            try
            {
                var doc = JsonDocument.Parse(json);
                bool success = doc.RootElement.GetProperty("success").GetBoolean();
                // v3 returns a score (0.0 to 1.0) — accept if score >= 0.5
                if (success && doc.RootElement.TryGetProperty("score", out var scoreProp))
                    return scoreProp.GetDouble() >= 0.5;
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"reCAPTCHA parsing error: {ex.Message}");
                return false;
            }
        }

        // LOGIN GET
        public IActionResult Login()
        {
            ViewBag.RecaptchaSiteKey = _configuration["RecaptchaSettings:SiteKey"];
            return View();
        }
        public IActionResult Register()
        {
            ViewBag.RecaptchaSiteKey = _configuration["RecaptchaSettings:SiteKey"];
            ViewBag.Roles = new SelectList(_context.RoleMaster.ToList(), "RoleId", "RoleName");
            return View();
        }
        public IActionResult Edit(long id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Dashboard");
            }

            var user = _context.UserMaster.Find(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("UserList");
            }

            ViewBag.Roles = new SelectList(
                _context.RoleMaster.ToList(),
                "RoleId",
                "RoleName");

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var captchaToken = Request.Form["g-recaptcha-response"].ToString();
            if (string.IsNullOrEmpty(captchaToken) || !await VerifyCaptcha(captchaToken))
            {
                ModelState.AddModelError("", "Please complete the CAPTCHA verification.");
                ViewBag.RecaptchaSiteKey = _configuration["RecaptchaSettings:SiteKey"];
                ViewBag.Roles = new SelectList(_context.RoleMaster.ToList(), "RoleId", "RoleName");
                return View(model);
            }

            if (ModelState.IsValid)
            {
        // 🔍 CHECK DUPLICATES
        bool exists = _context.UserMaster.Any(x =>
            x.Username == model.Username ||
            x.EmailId == model.EmailId ||
            x.MobileNo == model.MobileNo
        );

        if (exists)
        {
            ModelState.AddModelError("", "Username, Email or Mobile already exists");

            ViewBag.Roles = new SelectList(
                _context.RoleMaster.ToList(),
                "RoleId",
                "RoleName");

            return View(model);
        }

        // 👤 CREATE USER
        UserMaster user = new UserMaster()
        {
            FullName = model.FullName,
            Username = model.Username,
            Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
            EmailId = model.EmailId,
            MobileNo = model.MobileNo,
            DOB = model.DOB,
            Gender = model.Gender,
            RoleId = model.RoleId,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _context.UserMaster.Add(user);
        await _context.SaveChangesAsync();

        await _logService.LogActivityAsync(model.Username, _context.RoleMaster.Find(model.RoleId)?.RoleName ?? "User", "Registration", $"New user '{model.Username}' registered.", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Success");
        TempData["Success"] = "Registration Successful";

        return RedirectToAction("Login");
    }

            ViewBag.Roles = new SelectList(
        _context.RoleMaster.ToList(),
        "RoleId",
        "RoleName");
        ViewBag.RecaptchaSiteKey = _configuration["RecaptchaSettings:SiteKey"];

    return View(model);
}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserMaster model)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized();
            }

            var user = await _context.UserMaster.FindAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            user.FullName = model.FullName;
            user.Username = model.Username;
            user.EmailId = model.EmailId;
            user.MobileNo = model.MobileNo;
            user.DOB = model.DOB;
            user.Gender = model.Gender;
            user.RoleId = model.RoleId;

            await _context.SaveChangesAsync();
            var adminUser = HttpContext.Session.GetString("Username") ?? "Admin";
            await _logService.LogActivityAsync(adminUser, "Admin", "Edit User", $"Admin '{adminUser}' edited user '{user.Username}'.", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Success");
            TempData["Success"] = "User updated successfully!";
            return RedirectToAction("UserList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(long id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Dashboard");
        
            var user = _context.UserMaster.Include(u => u.Role).FirstOrDefault(u => u.UserId == id);
        
            if (user != null)
            {
                // Prevent deactivating the last admin
                if (user.Role.RoleName == "Admin" && user.IsActive)
                {
                    int adminCount = _context.UserMaster.Count(u => u.Role.RoleName == "Admin" && u.IsActive);
                    if (adminCount <= 1)
                    {
                        TempData["Error"] = "Cannot deactivate the last active admin.";
                        return RedirectToAction("UserList");
                    }
                }
        
                user.IsActive = !user.IsActive;
                _context.UserMaster.Update(user);
                _context.SaveChanges();
                var adminActor = HttpContext.Session.GetString("Username") ?? "Admin";
                string action = user.IsActive ? "Activated User" : "Deactivated User";
                await _logService.LogActivityAsync(adminActor, "Admin", action, $"Admin '{adminActor}' set '{user.Username}' to {(user.IsActive ? "Active" : "Inactive")}.", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Success");
                TempData["Success"] = $"User '{user.Username}' status updated to {(user.IsActive ? "Active" : "Inactive")}.";
            }
        
            return RedirectToAction("UserList");
        }

        // ADMIN: Unblock a locked user
        [HttpPost]
        public IActionResult UnblockUser(long id)
        {
            // Ensure only admins can perform this action
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Dashboard");

            var user = _context.UserMaster.Find(id);
            if (user != null)
            {
                // Reset lockout fields
                user.LockoutEnd = null;
                user.FailedLoginAttempts = 0;
                _context.SaveChanges();
                TempData["Success"] = $"User '{user.Username}' has been unblocked.";
            }
            else
            {
                TempData["Error"] = "User not found.";
            }

            return RedirectToAction("UserList");
        }

        // GET: Account/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Dashboard");
            }

            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.UserMaster
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            // Prevent deletion of the last admin
            if (user.Role?.RoleName == "Admin")
            {
                var adminCount = await _context.UserMaster.CountAsync(u => u.Role.RoleName == "Admin");
                if (adminCount <= 1)
                {
                    TempData["Error"] = "Cannot delete the last admin account.";
                    return RedirectToAction("UserList");
                }
            }

            string deletedUsername = user.Username;
            _context.UserMaster.Remove(user);
            await _context.SaveChangesAsync();

            var adminDeleter = HttpContext.Session.GetString("Username") ?? "Admin";
            await _logService.LogActivityAsync(adminDeleter, "Admin", "Delete User", $"Admin '{adminDeleter}' deleted user '{deletedUsername}'.", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Success");
            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction("UserList");
        }



        [HttpPost]
public async Task<IActionResult> Login(LoginViewModel model)
{
    ViewBag.RecaptchaSiteKey =
        _configuration["RecaptchaSettings:SiteKey"];
    var claims = new List<System.Security.Claims.Claim>();

    var captchaToken =
        Request.Form["g-recaptcha-response"].ToString();

    if (string.IsNullOrEmpty(captchaToken) ||
        !await VerifyCaptcha(captchaToken))
    {
        ViewBag.Error =
            "Please complete CAPTCHA verification.";

        return View(model);
    }

    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var user = _context.UserMaster
        .Include(x => x.Role)
        .FirstOrDefault(x =>
            x.Username == model.Username);

    if (user == null)
    {
        ViewBag.Error =
            "Invalid Username or Password";

        return View(model);
    }

    // CHECK IF USER IS ACTIVE
    if (!user.IsActive)
    {
        ViewBag.Error = "Your account is inactive. Please contact an administrator.";
        return View(model);
    }

    // CHECK LOCKOUT
    if (user.LockoutEnd.HasValue &&
        user.LockoutEnd > DateTime.Now)
    {
        ModelState.AddModelError("",
            $"Account locked until {user.LockoutEnd}");

        return View(model);
    }

    bool isPasswordValid =
        BCrypt.Net.BCrypt.Verify(
            model.Password,
            user.Password);

    if (isPasswordValid)
    {
        // RESET FAILED ATTEMPTS
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        _context.SaveChanges();

        claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username));
        claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role.RoleName));
        claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.UserId.ToString()));

        var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, "CookieAuth");

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true
        };

        await HttpContext.SignInAsync("CookieAuth", new System.Security.Claims.ClaimsPrincipal(claimsIdentity), authProperties);

        HttpContext.Session.SetString("UserId", user.UserId.ToString());
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("Role", user.Role.RoleName);

        await _logService.LogActivityAsync(user.Username, user.Role.RoleName, "Login", $"User '{user.Username}' logged in successfully.", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Success");
        TempData["Success"] = "Login Successful";

        return RedirectToAction("Dashboard");
    }

    // FAILED LOGIN
    user.FailedLoginAttempts++;

    if (user.FailedLoginAttempts >= 5)
    {
        user.LockoutEnd = DateTime.Now.AddMinutes(15);
        user.FailedLoginAttempts = 0;
    }

    _context.SaveChanges();

    await _logService.LogActivityAsync(model.Username, "Unknown", "Login", $"Failed login attempt for '{model.Username}'.", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Failed");

    ViewBag.Error = "Invalid Username or Password";

    return View(model);
}

        // DASHBOARD
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login");

            ViewBag.Role = HttpContext.Session.GetString("Role");

            if (long.TryParse(HttpContext.Session.GetString("UserId"), out long userId))
            {
                ViewBag.Documents = _context.DocumentMaster
                    .Where(d => d.UserId == userId)
                    .OrderByDescending(d => d.UploadDate)
                    .ToList();
            }

            return View();
        }

        // USER LIST
       public IActionResult UserList(
    string searchText,
    string searchBy)
{
    string? role =
        HttpContext.Session.GetString("Role");

    if(role != "Admin")
    {
        return RedirectToAction("Dashboard");
    }

    var users = _context.UserMaster
        .Include(x => x.Role)
        .AsQueryable();

    if(!string.IsNullOrEmpty(searchText))
    {
        switch(searchBy)
        {
            case "FullName":

                users = users.Where(x =>
                    x.FullName.Contains(searchText));

                break;

            case "Username":

                users = users.Where(x =>
                    x.Username.Contains(searchText));

                break;

            case "MobileNo":

                users = users.Where(x =>
                    x.MobileNo.Contains(searchText));

                break;

            case "Role":

                users = users.Where(x =>
                    x.Role.RoleName
                    .Contains(searchText));

                break;

            default:

                users = users.Where(x =>

                    x.FullName.Contains(searchText) ||

                    x.Username.Contains(searchText) ||

                    x.MobileNo.Contains(searchText) ||

                    x.Role.RoleName
                    .Contains(searchText)
                );

                break;
        }
    }

    ViewBag.SearchText = searchText;

    ViewBag.SearchBy = searchBy;

    var userList = users.ToList();

    // Load documents for each user (keyed by UserId)
    var userIds = userList.Select(u => u.UserId).ToList();
    ViewBag.DocumentsPerUser = _context.DocumentMaster
        .Where(d => userIds.Contains(d.UserId))
        .GroupBy(d => d.UserId)
        .ToDictionary(g => g.Key, g => g.ToList());

    return View(userList);
}

        // GET: Account/CheckAvailability (AJAX Check)
        [HttpGet]
        public IActionResult CheckAvailability(string type, string value)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value))
            {
                return Json(new { available = false, message = "Invalid input" });
            }

            bool exists = false;
            value = value.Trim();

            switch (type.ToLower())
            {
                case "username":
                    exists = _context.UserMaster.Any(x => x.Username.ToLower() == value.ToLower());
                    break;
                case "email":
                case "emailid":
                    exists = _context.UserMaster.Any(x => x.EmailId.ToLower() == value.ToLower());
                    break;
                case "mobile":
                case "mobileno":
                    exists = _context.UserMaster.Any(x => x.MobileNo == value);
                    break;
                default:
                    return Json(new { available = false, message = "Invalid validation type" });
            }

            return Json(new { available = !exists });
        }

        // LOGOUT
        public async Task<IActionResult> Logout()
        {
            string username = HttpContext.Session.GetString("Username") ?? "Unknown";
            string role = HttpContext.Session.GetString("Role") ?? "Unknown";
            await _logService.LogActivityAsync(username, role, "Logout", $"User '{username}' logged out.", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Success");
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync("CookieAuth");
            TempData["Success"] = "Logged Out Successfully";

            return RedirectToAction("Login");
        }

        // GET: Account/AccessDenied
        public IActionResult AccessDenied()
        {
            TempData["Error"] = "You do not have permission to access that page.";
            var role = HttpContext.Session.GetString("Role");
            if (!string.IsNullOrEmpty(role))
                return RedirectToAction("Dashboard");

            return RedirectToAction("Login");
        }

        // GET: Account/KeepAlive — extends server session on user activity
        [HttpGet]
        public IActionResult KeepAlive()
        {
            if (HttpContext.Session.GetString("Username") == null)
                return Unauthorized();

            return Ok();
        }

        // FORGOT PASSWORD GET
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // FORGOT PASSWORD POST
        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.UserMaster.FirstOrDefault(x =>
                    x.Username.ToLower() == model.Username.ToLower() &&
                    x.EmailId.ToLower() == model.EmailId.ToLower() &&
                    x.MobileNo == model.MobileNo);

                if (user != null)
                {
                    HttpContext.Session.SetString("ResetUsername", user.Username);
                    return RedirectToAction("ResetPassword");
                }

                ModelState.AddModelError("", "No matching user found with the provided details.");
            }

            return View(model);
        }

        // RESET PASSWORD GET
        public IActionResult ResetPassword()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("ResetUsername")))
            {
                TempData["Error"] = "Session expired or invalid. Please verify your details again.";
                return RedirectToAction("ForgotPassword");
            }

            return View();
        }

        // RESET PASSWORD POST
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var username = HttpContext.Session.GetString("ResetUsername");
            if (string.IsNullOrEmpty(username))
            {
                TempData["Error"] = "Session expired or invalid. Please verify your details again.";
                return RedirectToAction("ForgotPassword");
            }

            if (ModelState.IsValid)
            {
                var user = _context.UserMaster.FirstOrDefault(x => x.Username == username);
                if (user != null)
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    _context.UserMaster.Update(user);
                    _context.SaveChanges();

                    await _logService.LogActivityAsync(username, "User", "Password Reset", $"User '{username}' reset their password.", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Success");
                    HttpContext.Session.Remove("ResetUsername");
                    TempData["Success"] = "Password reset successfully! Please login with your new password.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("", "User not found. Please try again.");
            }

            return View(model);
        }
    }
}