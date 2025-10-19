using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Core.Interfaces;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Order;
using System.Threading.Tasks;
/*OrdersController
•	POST /api/orders (guest veya userId ile) -> sipariş oluşturma. Eğer guest ise, zorunlu alanlar: name, phone, email, address, paymentMethod
•	GET /api/orders/{id} -> sipariş detay (admin, ilgili kullanıcı veya kurye görmeli)
•	GET /api/orders/user/{userId} -> kullanıcı siparişleri
*/
namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = Roles.Admin)] // sadece admin erişebilir
    [Route("api/admin/orders")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public AdminOrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _orderService.GetOrdersAsync();
            return Ok(orders);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var existing = await _orderService.GetByIdAsync(id);
            if (existing == null) return NotFound();
            await _orderService.DeleteAsync(id);
            return NoContent();
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
        {
            var order = await _orderService.CreateAsync(dto);
            return Ok(order);
        }

      
    }
}
