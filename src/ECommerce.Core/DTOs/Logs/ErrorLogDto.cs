using System;

namespace ECommerce.Core.DTOs.Logs
{
    public class ErrorLogDto
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? Path { get; set; }
        public string? Method { get; set; }
        public int? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
