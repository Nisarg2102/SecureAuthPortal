using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecureAuthPortal.Data;
using SecureAuthPortal.Models;

namespace SecureAuthPortal.Services
{
    public class LogService : ILogService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LogService> _logger;

        public LogService(IServiceScopeFactory scopeFactory, ILogger<LogService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task LogActivityAsync(string username, string role, string activity, string description, string ipAddress, string status)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var log = new ActivityLog
                {
                    Username = username ?? "Unknown",
                    Role = role ?? "Unknown",
                    Activity = activity,
                    Description = description,
                    IpAddress = ipAddress,
                    Status = status,
                    Timestamp = DateTime.Now
                };

                context.ActivityLog.Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log activity: {Activity} for user {Username}", activity, username);
            }
        }

        public async Task LogErrorAsync(string username, string errorMessage, string controller, string action, string stackTrace, string ipAddress)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var log = new ErrorLog
                {
                    Username = username ?? "Anonymous",
                    ErrorMessage = errorMessage,
                    Controller = controller,
                    Action = action,
                    StackTrace = stackTrace,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.Now
                };

                context.ErrorLog.Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record error log. Original error: {ErrorMessage}", errorMessage);
            }
        }
    }
}
