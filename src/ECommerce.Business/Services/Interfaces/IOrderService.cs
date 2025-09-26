
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

        
    }
}
