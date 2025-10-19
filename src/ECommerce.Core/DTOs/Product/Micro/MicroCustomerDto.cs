namespace ECommerce.Core.DTOs.Micro
{
    public class MicroCustomerDto
    {
        public string ExternalId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}

