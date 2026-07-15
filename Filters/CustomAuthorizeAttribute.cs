using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SecureAuthPortal.Filters
{
    /// <summary>
    /// Custom Authorization Attribute
    /// 
    /// Purpose: Check user role before allowing access to controller/action
    /// 
    /// Usage:
    /// [CustomAuthorize("Admin")] - Only Admin role allowed
    /// [CustomAuthorize("Admin", "Manager")] - Admin or Manager allowed
    /// [CustomAuthorize] - Any authenticated user allowed
    /// </summary>
    public class CustomAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public CustomAuthorizeAttribute(params string[] roles)
        {
            _allowedRoles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Step 1: Check if user is authenticated (has session)
            var userRole = context.HttpContext.Session.GetString("Role");
            var userId = context.HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
            {
                // User not logged in - redirect to login
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
                return;
            }

            // Step 2: Check if user has required role (if roles specified)
            if (_allowedRoles.Length > 0 && !_allowedRoles.Contains(userRole))
            {
                // User logged in but doesn't have permission - return 403 Forbidden
                context.Result = new StatusCodeResult(403);
                return;
            }

            // User is authenticated and authorized
        }
    }
}
