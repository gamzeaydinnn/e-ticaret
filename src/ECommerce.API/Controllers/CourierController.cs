/*CourierController
•	GET /api/courier/orders -> (courier auth) kendisine atanmış siparişleri listeler
•	POST /api/courier/orders/{orderId}/status -> teslim edildi / teslim edilemedi gibi status güncellemesi
*/
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourierController : ControllerBase
    {
        private readonly ICourierService _courierService;

        public CourierController(ICourierService courierService)
        {
            _courierService = courierService;
        }

        // GET: api/courier
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var couriers = await _courierService.GetAllAsync();
            return Ok(couriers);
        }

        // GET: api/courier/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var courier = await _courierService.GetByIdAsync(id);
            if (courier == null) return NotFound();
            return Ok(courier);
        }

        // POST: api/courier
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] Courier courier)
        {
            await _courierService.AddAsync(courier);
            return CreatedAtAction(nameof(GetById), new { id = courier.Id }, courier);
        }

        // PUT: api/courier/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Courier courier)
        {
            var existing = await _courierService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.UserId = courier.UserId;
            // Diğer alanlar eklenirse buraya

            await _courierService.UpdateAsync(existing);
            return NoContent();
        }

        // DELETE: api/courier/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var courier = await _courierService.GetByIdAsync(id);
            if (courier == null) return NotFound();

            await _courierService.DeleteAsync(courier);
            return NoContent();
        }

        // GET: api/courier/count
        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var count = await _courierService.GetCourierCountAsync();
            return Ok(count);
        }

        // Ek olarak, kurye siparişleri ve durum güncelleme gibi endpoint'ler de eklenebilir
        // GET: api/courier/orders
        [HttpGet("orders")]
        public IActionResult GetMyOrders()
        {
            // Burada örnek bir liste döndürebilirsin veya OrderService üzerinden getir
            return Ok(new List<object>()); 
        }

        // POST: api/courier/orders/5/status
        [HttpPost("orders/{orderId}/status")]
        public IActionResult UpdateOrderStatus(int orderId, [FromBody] dynamic body)
        {
            string status = body.status;
            // OrderService kullanarak durumu güncelle
            return Ok(new { orderId, status });
        }
    }
}
