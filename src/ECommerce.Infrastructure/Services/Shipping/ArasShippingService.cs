namespace ECommerce.Infrastructure.Services.Shipping
{
    public class ArasShippingService
    {
        public string CreateShipment(string address, int orderId)
        {
            return $"ARAS-{orderId}-{System.DateTime.Now.Ticks}";
        }
    }
}
