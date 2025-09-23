namespace ECommerce.Infrastructure.Services.Payment
{
    public class StripePaymentService
    {
        public bool ProcessPayment(decimal amount, string cardNumber, string expiry, string cvv)
        {
            // Mock Stripe payment API call
            return true;
        }
    }
}

