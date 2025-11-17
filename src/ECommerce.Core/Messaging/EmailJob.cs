namespace ECommerce.Core.Messaging
{
    public class EmailJob
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public int Attempts { get; set; } = 0;
    }
}
