using System;
using System.Threading.Tasks;

namespace SecureAuthPortal.Services
{
    public interface ILogService
    {
        Task LogActivityAsync(string username, string role, string activity, string description, string ipAddress, string status);
        Task LogErrorAsync(string username, string errorMessage, string controller, string action, string stackTrace, string ipAddress);
    }
}
