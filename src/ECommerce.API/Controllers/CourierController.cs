/*CourierController
•	GET /api/courier/orders -> (courier auth) kendisine atanmış siparişleri listeler
•	POST /api/courier/orders/{orderId}/status -> teslim edildi / teslim edilemedi gibi status güncellemesi
*/
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Interfaces;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourierController : ControllerBase
    {
    private readonly ICourierService _courierService;
    private readonly UserManager<User> _userManager;
    private readonly IHostEnvironment _env;

        public CourierController(ICourierService courierService, UserManager<User> userManager, IHostEnvironment env)
        {
            _courierService = courierService;
            _userManager = userManager;
            _env = env;
        }
        // DEVELOPMENT: Demo kurye ve user ekler
        [HttpPost("seed-demo")]
        public async Task<IActionResult> SeedDemoCourier()
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var email = "ahmet@courier.com";
            var password = "123456";
            var phone = "05321234567";

            // User zaten var mı?
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                var user = new User
                {
                    UserName = email,
                    Email = email,
                    PhoneNumber = phone,
                    EmailConfirmed = true,
                    FullName = "Ahmet Yılmaz"
                };
                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                    return BadRequest(result.Errors);
                existingUser = user;
            }

            // Courier zaten var mı?
            var couriers = await _courierService.GetAllAsync();
            var existingCourier = couriers.FirstOrDefault(c => c.UserId == existingUser.Id);
            if (existingCourier == null)
            {
                var courier = new Courier
                {
                    UserId = existingUser.Id,
                    Status = "active",
                    Phone = phone,
                    Vehicle = "Motosiklet",
                    Location = "Kadıköy, İstanbul",
                    Rating = 4.8m,
                    ActiveOrders = 3,
                    CompletedToday = 12
                };
                await _courierService.AddAsync(courier);
            }

            return Ok(new { message = "Demo kurye ve user başarıyla eklendi." });
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

        // POST: api/courier/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] CourierLoginRequest request)
        {
            try
            {
                // Basit kurye authentication - gerçek projede JWT token kullanın
                var courier = await _courierService.GetAllAsync();
                var foundCourier = courier.FirstOrDefault(c => 
                    c.User?.Email == request.Email && 
                    request.Password == "123456" // Demo şifre
                );

                if (foundCourier == null)
                    return Unauthorized(new { message = "Geçersiz giriş bilgileri" });

                return Ok(new { 
                    success = true, 
                    courier = new {
                        foundCourier.Id,
                        Name = foundCourier.User?.FullName,
                        Email = foundCourier.User?.Email,
                        foundCourier.Phone,
                        foundCourier.Vehicle,
                        foundCourier.Status,
                        foundCourier.Location,
                        foundCourier.Rating,
                        foundCourier.ActiveOrders,
                        foundCourier.CompletedToday,
                        Token = $"courier-token-{foundCourier.Id}" // Mock token
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/courier/{courierId}/orders
        [HttpGet("{courierId}/orders")]
        public async Task<IActionResult> GetAssignedOrders(int courierId)
        {
            try
            {
                // Mock sipariş verisi döndür - gerçek projede OrderService kullanın
                var mockOrders = new List<object>
                {
                    new {
                        Id = 1,
                        CustomerName = "Ayşe Kaya",
                        CustomerPhone = "0534 555 1234",
                        Address = "Atatürk Cad. No: 45/3 Kadıköy, İstanbul",
                        Items = new[] { "Domates 1kg", "Ekmek 2 adet", "Süt 1lt" },
                        TotalAmount = 45.50,
                        Status = "preparing",
                        OrderTime = DateTime.Now.AddMinutes(-30),
                        EstimatedDelivery = DateTime.Now.AddMinutes(40),
                        Priority = "normal"
                    }
                };

                return Ok(mockOrders);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH: api/courier/orders/{orderId}/status
        [HttpPatch("orders/{orderId}/status")]
        public IActionResult UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                // Mock update - gerçek projede OrderService kullanın
                return Ok(new { 
                    success = true, 
                    orderId, 
                    status = request.Status,
                    notes = request.Notes,
                    updatedAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/courier/{courierId}/performance
        [HttpGet("{courierId}/performance")]
        public async Task<IActionResult> GetCourierPerformance(int courierId, [FromQuery] string period = "today")
        {
            try
            {
                var courier = await _courierService.GetByIdAsync(courierId);
                if (courier == null) return NotFound();

                // Mock performans verisi
                var performance = new {
                    Courier = new {
                        courier.Id,
                        Name = courier.User?.FullName,
                        courier.Rating
                    },
                    Deliveries = new {
                        Total = 12,
                        OnTime = 10,
                        Delayed = 2,
                        Cancelled = 0
                    },
                    Rating = courier.Rating,
                    Timeline = new[] {
                        new { Time = "09:00", Action = "Vardiya başladı", Status = "active" },
                        new { Time = "09:15", Action = "Sipariş #1001 teslim alındı", Status = "picked_up" },
                        new { Time = "09:45", Action = "Sipariş #1001 teslim edildi", Status = "delivered" }
                    }
                };

                return Ok(performance);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // Request models
    public class CourierLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
