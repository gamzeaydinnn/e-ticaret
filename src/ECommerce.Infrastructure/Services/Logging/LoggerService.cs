using System;

namespace ECommerce.Infrastructure.Services.Logging
{
    public class LoggerService
    {
        public void LogInfo(string message) => Console.WriteLine($"INFO: {message}");
        public void LogError(string message) => Console.WriteLine($"ERROR: {message}");
    }
}
