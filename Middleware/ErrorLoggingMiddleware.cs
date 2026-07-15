using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SecureAuthPortal.Services;
using Microsoft.AspNetCore.Routing;

namespace SecureAuthPortal.Middleware
{
    public class ErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
                throw; // Re-throw to allow default error handling page to show
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // We use a new scope to resolve the scoped ILogService
            using var scope = context.RequestServices.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<ILogService>();

            // Safely read session — session may not be available for all request types
            string username = "Anonymous";
            try { username = context.Session.GetString("Username") ?? "Anonymous"; } catch { }

            string ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Extract controller and action if available
            var routeData = context.GetRouteData();
            string controllerName = routeData?.Values["controller"]?.ToString() ?? "Unknown";
            string actionName = routeData?.Values["action"]?.ToString() ?? "Unknown";

            try
            {
                await logService.LogErrorAsync(
                    username: username,
                    errorMessage: exception.Message,
                    controller: controllerName,
                    action: actionName,
                    stackTrace: exception.StackTrace ?? string.Empty,
                    ipAddress: ipAddress
                );
            }
            catch
            {
                // Swallow logging errors — don't let them mask the original exception
            }
        }
    }
}
