namespace ECommerce.Core.Messaging
{
    public class SmsJob
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int Attempts { get; set; } = 0;
    }
}
