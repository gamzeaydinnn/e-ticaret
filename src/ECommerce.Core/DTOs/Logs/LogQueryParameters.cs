using System;

namespace ECommerce.Core.DTOs.Logs
{
    public class LogQueryParameters
    {
        private const int MaxTake = 200;

        public int Skip { get; set; } = 0;

        private int _take = 20;
        public int Take
        {
            get => _take;
            set => _take = value <= 0 ? 20 : Math.Min(value, MaxTake);
        }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Search { get; set; }
    }

    public class AuditLogQueryParameters : LogQueryParameters
    {
        public string? EntityType { get; set; }
        public string? Action { get; set; }
    }

    public class ErrorLogQueryParameters : LogQueryParameters
    {
        public string? Path { get; set; }
        public string? Method { get; set; }
    }

    public class SystemLogQueryParameters : LogQueryParameters
    {
        public string? EntityType { get; set; }
        public string? Status { get; set; }
        public string? Direction { get; set; }
    }
}
