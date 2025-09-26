using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Core.Interfaces;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Order;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "Admin")] // sadece admin erişebilir
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
            var orders = await _orderService.GetOrdersAsync(); // düzeltilmiş
            return Ok(orders);
                }

            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteOrder(int id)
                {
                await _orderService.DeleteAsync(id); // zaten tek DeleteAsync var
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
