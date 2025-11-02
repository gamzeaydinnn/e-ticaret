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
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;


namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourierController : ControllerBase
    {
    private readonly ICourierService _courierService;
    private readonly UserManager<User> _userManager;
    private readonly IHostEnvironment _env;
    private readonly IWeightService _weightService;
    private readonly IWeightReportRepository _weightReportRepository;
    private readonly ILogger<CourierController> _logger;

        public CourierController(
            ICourierService courierService, 
            UserManager<User> userManager, 
            IHostEnvironment env,
            IWeightService weightService,
            IWeightReportRepository weightReportRepository,
            ILogger<CourierController> logger)
        {
            _courierService = courierService;
            _userManager = userManager;
            _env = env;
            _weightService = weightService;
            _weightReportRepository = weightReportRepository;
            _logger = logger;
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
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Status))
                {
                    return BadRequest(new { message = "Status gereklidir." });
                }

                _logger.LogInformation("Sipariş #{OrderId} durumu güncelleniyor: {Status}", orderId, request.Status);

                object response = new 
                { 
                    success = true, 
                    orderId, 
                    status = request.Status,
                    notes = request.Notes,
                    updatedAt = DateTime.Now,
                    paymentProcessed = false,
                    paymentAmount = 0m,
                    paymentDetails = new List<string>(),
                    message = string.Empty
                };

                // Eğer sipariş "delivered" (teslim edildi) durumuna geçiyorsa
                if (request.Status.Equals("delivered", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Sipariş #{OrderId} teslim edildi durumuna geçiyor. Ağırlık raporları kontrol ediliyor...", orderId);

                    // Onaylanmış (Approved) ağırlık raporlarını kontrol et
                    var reports = await _weightReportRepository.GetByOrderIdAsync(orderId);
                    var approvedReports = reports
                        .Where(r => r.Status == WeightReportStatus.Approved && r.OverageAmount > 0)
                        .ToList();

                    if (approvedReports.Any())
                    {
                        _logger.LogInformation("Sipariş #{OrderId} için {Count} adet onaylı ağırlık raporu bulundu. Ödeme tahsilatı başlatılıyor...", 
                            orderId, approvedReports.Count);

                        decimal totalCharged = 0;
                        var paymentDetailsList = new List<string>();
                        var allPaymentsSuccessful = true;

                        foreach (var report in approvedReports)
                        {
                            try
                            {
                                _logger.LogInformation("Ağırlık raporu #{ReportId} için ödeme tahsilatı yapılıyor...", report.Id);
                                
                                // Ödeme servisini çağır
                                var charged = await _weightService.ChargeOverageAsync(report.Id);
                                
                                if (charged)
                                {
                                    totalCharged += report.OverageAmount;
                                    paymentDetailsList.Add($"Rapor #{report.Id}: +{report.OverageAmount:F2} ₺ tahsil edildi");
                                    _logger.LogInformation("✅ Ağırlık raporu #{ReportId} için {Amount:C} başarıyla tahsil edildi.", 
                                        report.Id, report.OverageAmount);
                                }
                                else
                                {
                                    allPaymentsSuccessful = false;
                                    paymentDetailsList.Add($"Rapor #{report.Id}: Ödeme başarısız");
                                    _logger.LogWarning("⚠️ Ağırlık raporu #{ReportId} için ödeme başarısız oldu.", report.Id);
                                }
                            }
                            catch (Exception ex)
                            {
                                allPaymentsSuccessful = false;
                                _logger.LogError(ex, "❌ Ağırlık raporu #{ReportId} için ödeme alınırken hata oluştu.", report.Id);
                                paymentDetailsList.Add($"Rapor #{report.Id}: Hata - {ex.Message}");
                            }
                        }

                        response = new
                        {
                            success = allPaymentsSuccessful,
                            orderId,
                            status = request.Status,
                            notes = request.Notes,
                            updatedAt = DateTime.Now,
                            paymentProcessed = true,
                            paymentAmount = totalCharged,
                            paymentDetails = paymentDetailsList,
                            message = allPaymentsSuccessful 
                                ? $"✅ Teslimat tamamlandı. Toplam {totalCharged:F2} ₺ ek ücret tahsil edildi."
                                : $"⚠️ Teslimat tamamlandı ancak bazı ödemeler başarısız oldu. Toplam {totalCharged:F2} ₺ tahsil edildi."
                        };

                        _logger.LogInformation("Sipariş #{OrderId} teslimat ve ödeme işlemi tamamlandı. Toplam tahsilat: {Amount:C}", 
                            orderId, totalCharged);
                    }
                    else
                    {
                        _logger.LogInformation("Sipariş #{OrderId} için ödeme gerektiren onaylı ağırlık raporu bulunamadı. Normal teslimat işleniyor.", orderId);
                        
                        response = new
                        {
                            success = true,
                            orderId,
                            status = request.Status,
                            notes = request.Notes,
                            updatedAt = DateTime.Now,
                            paymentProcessed = false,
                            paymentAmount = 0m,
                            paymentDetails = new List<string>(),
                            message = "✅ Teslimat başarıyla tamamlandı."
                        };
                    }
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} durumu güncellenirken beklenmeyen hata oluştu.", orderId);
                return StatusCode(500, new { 
                    success = false,
                    message = "Sipariş durumu güncellenirken hata oluştu.",
                    error = ex.Message 
                });
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

        // GET: api/courier/orders/{orderId}/weight-reports
        [HttpGet("orders/{orderId}/weight-reports")]
        public async Task<IActionResult> GetOrderWeightReports(int orderId)
        {
            try
            {
                _logger.LogInformation("Sipariş #{OrderId} için ağırlık raporları istendi", orderId);

                var reports = await _weightReportRepository.GetByOrderIdAsync(orderId);
                
                if (reports == null || !reports.Any())
                {
                    return Ok(new List<object>());
                }

                var reportDtos = reports.Select(r => new
                {
                    r.Id,
                    r.OrderId,
                    r.ExpectedWeightGrams,
                    r.ReportedWeightGrams,
                    r.OverageGrams,
                    r.OverageAmount,
                    r.Currency,
                    Status = r.Status.ToString(),
                    r.ReceivedAt,
                    r.ProcessedAt,
                    r.CourierNote
                }).ToList();

                _logger.LogInformation("Sipariş #{OrderId} için {Count} adet ağırlık raporu döndürüldü", 
                    orderId, reportDtos.Count);

                return Ok(reportDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} için ağırlık raporları alınırken hata oluştu", orderId);
                return StatusCode(500, new { error = "Ağırlık raporları alınamadı", details = ex.Message });
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
