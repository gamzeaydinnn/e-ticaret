using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services.Logging
{
    // Microsoft.Extensions.Logging tabanlı basit sarmalayıcı
    public class LoggerService
    {
        private readonly ILogger<LoggerService> _logger;

        public LoggerService(ILogger<LoggerService> logger)
        {
            _logger = logger;
        }

        public void LogInfo(string message) => _logger.LogInformation(message);
        public void LogError(string message, Exception? ex = null)
            => _logger.LogError(ex, message);
    }
}
