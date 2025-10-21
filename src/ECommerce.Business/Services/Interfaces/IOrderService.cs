using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Order;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderListDto>> GetOrdersAsync(int? userId = null);
        Task<OrderListDto?> GetByIdAsync(int id);
        Task<OrderListDto> CreateAsync(OrderCreateDto dto);
        Task UpdateAsync(int id, OrderUpdateDto dto);
        Task DeleteAsync(int id);
        Task<bool> ChangeOrderStatusAsync(int id, string newStatus);
        Task<OrderListDto> CheckoutAsync(OrderCreateDto dto);
        Task<bool> CancelOrderAsync(int orderId, int userId);
        Task<int> GetOrderCountAsync();
        Task<int> GetTodayOrderCountAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task<IEnumerable<OrderListDto>> GetAllOrdersAsync(int page = 1, int size = 20);
        Task<OrderListDto> GetOrderByIdAsync(int id);
        Task UpdateOrderStatusAsync(int id, string status);
        Task<IEnumerable<OrderListDto>> GetRecentOrdersAsync(int count = 10);
        Task<OrderDetailDto?> GetDetailByIdAsync(int id);
    }
}
/* Task<OrderSummaryDto> CreateOrderAsync(OrderCreateDto dto, Guid userId, CancellationToken ct = default);
    Task<OrderDetailDto> GetOrderAsync(Guid orderId);
    Task<IEnumerable<OrderListDto>> GetOrdersAsync(int? userId = null);
    Task CancelOrderAsync(Guid orderId, string reason);
    Task ConfirmPaymentAsync(Guid orderId, PaymentResultDto paymentResult); // payment webhook tetikler
}*/