namespace ECommerce.Entities.Concrete
{
    public class ErrorLog : BaseEntity
    {
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? Path { get; set; }
        public string? Method { get; set; }
        public int? UserId { get; set; }
    }
}
