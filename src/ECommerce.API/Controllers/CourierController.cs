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
using ECommerce.Core.Constants;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;


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
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly ECommerceDbContext _context;

        public CourierController(
            ICourierService courierService, 
            UserManager<User> userManager, 
            IHostEnvironment env,
            IWeightService weightService,
            IWeightReportRepository weightReportRepository,
            ILogger<CourierController> logger,
            RoleManager<IdentityRole<int>> roleManager,
            ECommerceDbContext context)
        {
            _courierService = courierService;
            _userManager = userManager;
            _env = env;
            _weightService = weightService;
            _weightReportRepository = weightReportRepository;
            _logger = logger;
            _roleManager = roleManager;
            _context = context;
        }
        // DEVELOPMENT: Demo kurye ve user ekler
        [HttpPost("seed-demo")]
        public async Task<IActionResult> SeedDemoCourier()
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var email = "ahmett@courier.com";
            var password = "ahmet.123";
            var phone = "05321234567";
            var courierRole = Roles.Courier;

            // Courier rolü yoksa oluştur
            if (!await _roleManager.RoleExistsAsync(courierRole))
            {
                await _roleManager.CreateAsync(new IdentityRole<int>(courierRole));
            }

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
            else
            {
                if (!existingUser.EmailConfirmed)
                {
                    existingUser.EmailConfirmed = true;
                }
                if (!existingUser.IsActive)
                {
                    existingUser.IsActive = true;
                }
                await _userManager.UpdateAsync(existingUser);
            }

            // Role alanını ve role membership'i güncelle
            if (!string.Equals(existingUser.Role, courierRole, StringComparison.OrdinalIgnoreCase))
            {
                existingUser.Role = courierRole;
                await _userManager.UpdateAsync(existingUser);
            }
            if (!await _userManager.IsInRoleAsync(existingUser, courierRole))
            {
                await _userManager.AddToRoleAsync(existingUser, courierRole);
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
            var couriers = await _context.Couriers
                .Include(c => c.User)
                .Where(c => c.IsActive)
                .ToListAsync();
            var result = couriers.Select(c => new
            {
                id = c.Id,
                userId = c.UserId,
                name = c.User?.FullName ?? string.Empty,
                email = c.User?.Email ?? string.Empty,
                phone = c.Phone ?? c.User?.PhoneNumber ?? string.Empty,
                vehicle = c.VehicleType ?? c.Vehicle ?? "motorcycle",
                status = c.Status,
                location = c.Location,
                rating = c.Rating,
                activeOrders = c.ActiveOrders,
                completedToday = c.CompletedToday,
                isOnline = c.IsOnline
            });
            return Ok(result);
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
        public async Task<IActionResult> Add([FromBody] CourierCreateRequestDto dto)
        {
            _logger.LogInformation("Kurye oluşturma isteği alındı: {@Dto}", new { dto.Name, dto.Email, dto.Phone, dto.Vehicle });
            
            if (dto == null)
            {
                _logger.LogWarning("Dto null");
                return BadRequest(new { message = "Geçersiz istek" });
            }
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                _logger.LogWarning("İsim boş");
                return BadRequest(new { message = "İsim zorunludur." });
            }
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                _logger.LogWarning("Email boş");
                return BadRequest(new { message = "E-posta zorunludur." });
            }
            if (string.IsNullOrWhiteSpace(dto.Phone))
            {
                _logger.LogWarning("Telefon boş");
                return BadRequest(new { message = "Telefon zorunludur." });
            }
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            {
                _logger.LogWarning("Şifre hatalı: {Length}", dto.Password?.Length ?? 0);
                return BadRequest(new { message = "Şifre en az 6 karakter olmalıdır." });
            }

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Bu e-posta zaten kayıtlı." });
            }

            var courierRole = Roles.Courier;
            if (!await _roleManager.RoleExistsAsync(courierRole))
            {
                await _roleManager.CreateAsync(new IdentityRole<int>(courierRole));
            }

            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.Phone,
                EmailConfirmed = true,
                FullName = dto.Name,
                IsActive = true,
                Role = courierRole
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                _logger.LogError("User oluşturulamadı: {Errors}", errors);
                return BadRequest(new { message = "Kurye kullanıcı oluşturulamadı.", errors = createResult.Errors.Select(e => e.Description).ToList() });
            }

            _logger.LogInformation("User oluşturuldu: {UserId}", user.Id);

            if (!await _userManager.IsInRoleAsync(user, courierRole))
            {
                await _userManager.AddToRoleAsync(user, courierRole);
            }

            var courier = new Courier
            {
                UserId = user.Id,
                Phone = dto.Phone,
                Vehicle = dto.Vehicle,
                VehicleType = dto.Vehicle,
                Status = "active",
                IsActive = true
            };

            try
            {
                await _courierService.AddAsync(courier);
                _logger.LogInformation("Kurye oluşturuldu: CourierId={CourierId}, UserId={UserId}", courier.Id, user.Id);
                return CreatedAtAction(nameof(GetById), new { id = courier.Id }, courier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye eklenirken hata: UserId={UserId}", user.Id);
                return StatusCode(500, new { message = "Kurye eklenirken hata oluştu.", error = ex.Message });
            }
        }

        // PUT: api/courier/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CourierUpdateRequestDto dto)
        {
            // Courier'ı User ile birlikte yükle
            var existing = await _context.Couriers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (existing == null)
            {
                _logger.LogWarning("Güncellenecek kurye bulunamadı: {CourierId}", id);
                return NotFound(new { message = "Kurye bulunamadı." });
            }

            // Courier bilgilerini güncelle
            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                existing.Phone = dto.Phone;
            }
            if (!string.IsNullOrWhiteSpace(dto.Vehicle))
            {
                existing.Vehicle = dto.Vehicle;
                existing.VehicleType = dto.Vehicle;
            }

            existing.UpdatedAt = DateTime.UtcNow;

            // User bilgilerini güncelle
            if (existing.User != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.Name))
                {
                    existing.User.FullName = dto.Name;
                }
                if (!string.IsNullOrWhiteSpace(dto.Email))
                {
                    existing.User.Email = dto.Email;
                    existing.User.UserName = dto.Email;
                    existing.User.NormalizedEmail = dto.Email.ToUpperInvariant();
                    existing.User.NormalizedUserName = dto.Email.ToUpperInvariant();
                }
                if (!string.IsNullOrWhiteSpace(dto.Phone))
                {
                    existing.User.PhoneNumber = dto.Phone;
                }
                existing.User.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Kurye güncellendi: {CourierId}, UserId: {UserId}", id, existing.UserId);
            return NoContent();
        }

        // DELETE: api/courier/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Courier'ı User ile birlikte yükle
            var courier = await _context.Couriers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (courier == null) return NotFound();

            // Soft delete: Hem Courier hem User'ı pasif yap
            courier.IsActive = false;
            courier.UpdatedAt = DateTime.UtcNow;
            
            if (courier.User != null)
            {
                courier.User.IsActive = false;
                courier.User.UpdatedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Kurye silindi (soft delete): {CourierId}, UserId: {UserId}", id, courier.UserId);
            return NoContent();
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetCourierPasswordDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "Şifre en az 6 karakter olmalıdır." });
            }

            // Courier'ı User ile birlikte yükle
            var courier = await _context.Couriers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (courier == null)
            {
                _logger.LogWarning("Kurye bulunamadı: {CourierId}", id);
                return NotFound(new { message = "Kurye bulunamadı." });
            }

            if (courier.User == null)
            {
                _logger.LogError("Kurye'ye bağlı User bulunamadı: CourierId={CourierId}, UserId={UserId}", id, courier.UserId);
                return NotFound(new { message = "Kurye kullanıcısı bulunamadı." });
            }

            // Şifre sıfırlama token'ı oluştur ve şifreyi değiştir
            var token = await _userManager.GeneratePasswordResetTokenAsync(courier.User);
            var result = await _userManager.ResetPasswordAsync(courier.User, token, dto.NewPassword);
            
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Şifre sıfırlanamadı: CourierId={CourierId}, Errors={Errors}", id, errors);
                return BadRequest(new { message = "Şifre sıfırlanamadı.", errors = result.Errors.Select(e => e.Description) });
            }

            _logger.LogInformation("Kurye şifresi sıfırlandı: {CourierId}, UserId: {UserId}", id, courier.UserId);
            return Ok(new { success = true, message = "Şifre başarıyla sıfırlandı." });
        }

        public class CourierCreateRequestDto
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Vehicle { get; set; } = "motorcycle";
            public string? PlateNumber { get; set; }
        }

        public class ResetCourierPasswordDto
        {
            public string NewPassword { get; set; } = string.Empty;
        }

        public class CourierUpdateRequestDto
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Vehicle { get; set; } = "motorcycle";
            public string? PlateNumber { get; set; }
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
