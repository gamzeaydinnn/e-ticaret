
using ECommerce.Core.DTOs.Order;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IOrderService
    {
        /// <summary>
        /// Tüm siparişleri listele
        /// </summary>
        Task<IEnumerable<OrderListDto>> GetOrdersAsync(int? userId = null);

        /// <summary>
        /// Siparişi ID ile getir
        /// </summary>
        Task<OrderListDto?> GetByIdAsync(int id);

        /// <summary>
        /// Yeni sipariş oluştur
        /// </summary>
        Task<OrderListDto> CreateAsync(OrderCreateDto dto);

        /// <summary>
        /// Siparişi güncelle
        /// </summary>
        Task UpdateAsync(int id, OrderUpdateDto dto);

        /// <summary>
        /// Siparişi sil
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Sipariş durumunu değiştir
        /// </summary>
        Task<bool> ChangeOrderStatusAsync(int id, string newStatus);
        Task<OrderListDto> CheckoutAsync(OrderCreateDto dto);



        
        // Admin methods
        Task<int> GetOrderCountAsync();
        Task<int> GetTodayOrderCountAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task<IEnumerable<OrderListDto>> GetAllOrdersAsync(int page = 1, int size = 20);
        Task<OrderListDto> GetOrderByIdAsync(int id);
        Task UpdateOrderStatusAsync(int id, string status);
        Task<IEnumerable<OrderListDto>> GetRecentOrdersAsync(int count = 10);
    }
}
