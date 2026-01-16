using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ECommerce.Business.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using ECommerce.Core.Extensions;
using ECommerce.Core.DTOs.ProductReview;
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.Constants;
using ECommerce.Core.Interfaces;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Ürün yönetimi için controller.
    /// Public endpoint'ler + Admin CRUD + Resim yükleme desteği sağlar.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IWebHostEnvironment _environment;
        private readonly IFileStorage _fileStorage;
        private readonly ILogger<ProductsController> _logger;

        // İzin verilen dosya türleri (güvenlik için whitelist yaklaşımı)
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
        
        // Maksimum dosya boyutu: 10MB
        private const long MaxFileSize = 10 * 1024 * 1024;

        public ProductsController(
            IProductService productService, 
            IWebHostEnvironment environment,
            IFileStorage fileStorage,
            ILogger<ProductsController> logger)
        {
            _productService = productService;
            _environment = environment;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                var allProducts = await _productService.GetActiveProductsAsync(page, size);
                return Ok(allProducts);
            }

            var products = await _productService.SearchProductsAsync(query, page, size);
            return Ok(products);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] int? categoryId = null)
        {
            var products = await _productService.GetActiveProductsAsync(page, size, categoryId);
            return Ok(products);
        }

        // Admin panel için tüm ürünleri getir (aktif ve pasif)
        [HttpGet("admin/all")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> GetAllProductsForAdmin([FromQuery] int page = 1, [FromQuery] int size = 100)
        {
            var products = await _productService.GetAllProductsAsync(page, size);
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        // Yeni ürün oluştur
        [HttpPost]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto dto)
        {
            if (dto == null) return BadRequest("Ürün bilgileri gerekli.");
            
            try
            {
                var product = await _productService.CreateProductAsync(dto);
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Ürün güncelle
        [HttpPut("{id}")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDto dto)
        {
            if (dto == null) return BadRequest("Ürün bilgileri gerekli.");
            
            try
            {
                var product = await _productService.UpdateProductAsync(id, dto);
                if (product == null) return NotFound();
                return Ok(product);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Ürün sil
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var result = await _productService.DeleteProductAsync(id);
                if (!result) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Stok güncelle
        [HttpPatch("{id}/stock")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] StockUpdateDto dto)
        {
            try
            {
                var result = await _productService.UpdateStockAsync(id, dto.Stock);
                if (!result) return NotFound();
                return Ok(new { message = "Stok güncellendi" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Excel/CSV dosyasından toplu ürün yükleme
        [HttpPost("import/excel")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls" && extension != ".csv")
                return BadRequest(new { message = "Sadece Excel (.xlsx, .xls) veya CSV (.csv) dosyaları kabul edilir." });

            try
            {
                var products = new List<ProductCreateDto>();

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    if (extension == ".csv")
                    {
                        // CSV işleme
                        using var reader = new StreamReader(stream);
                        var headerLine = await reader.ReadLineAsync();
                        if (headerLine == null)
                            return BadRequest(new { message = "CSV dosyası boş." });

                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var values = line.Split(',');
                            if (values.Length < 5) continue;

                            products.Add(new ProductCreateDto
                            {
                                Name = values[0].Trim().Trim('"'),
                                Description = values[1].Trim().Trim('"'),
                                Price = decimal.TryParse(values[2].Trim(), out var price) ? price : 0,
                                Stock = int.TryParse(values[3].Trim(), out var stock) ? stock : 0,
                                CategoryId = int.TryParse(values[4].Trim(), out var catId) ? catId : 1,
                                ImageUrl = values.Length > 5 ? values[5].Trim().Trim('"') : null
                            });
                        }
                    }
                    else
                    {
                        // Excel işleme
                        using var workbook = new XLWorkbook(stream);
                        var worksheet = workbook.Worksheets.First();
                        var rows = worksheet.RowsUsed().Skip(1); // İlk satır başlık

                        foreach (var row in rows)
                        {
                            var name = row.Cell(1).GetString();
                            if (string.IsNullOrWhiteSpace(name)) continue;

                            products.Add(new ProductCreateDto
                            {
                                Name = name,
                                Description = row.Cell(2).GetString(),
                                Price = row.Cell(3).TryGetValue<decimal>(out var price) ? price : 0,
                                Stock = row.Cell(4).TryGetValue<int>(out var stock) ? stock : 0,
                                CategoryId = row.Cell(5).TryGetValue<int>(out var catId) ? catId : 1,
                                ImageUrl = row.Cell(6).GetString()
                            });
                        }
                    }
                }

                if (products.Count == 0)
                    return BadRequest(new { message = "Dosyada geçerli ürün bulunamadı." });

                var createdCount = 0;
                var errors = new List<string>();

                foreach (var productDto in products)
                {
                    try
                    {
                        await _productService.CreateProductAsync(productDto);
                        createdCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"'{productDto.Name}': {ex.Message}");
                    }
                }

                return Ok(new
                {
                    message = $"{createdCount} ürün başarıyla eklendi.",
                    totalProcessed = products.Count,
                    successCount = createdCount,
                    errorCount = errors.Count,
                    errors = errors.Take(10).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Dosya işlenirken hata oluştu: {ex.Message}" });
            }
        }

        // Excel şablonu indir
        [HttpGet("import/template")]
        [Authorize(Roles = Roles.AdminLike)]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Ürünler");

            // Başlıklar
            worksheet.Cell(1, 1).Value = "Ürün Adı";
            worksheet.Cell(1, 2).Value = "Açıklama";
            worksheet.Cell(1, 3).Value = "Fiyat";
            worksheet.Cell(1, 4).Value = "Stok";
            worksheet.Cell(1, 5).Value = "Kategori ID";
            worksheet.Cell(1, 6).Value = "Görsel URL";

            // Başlık stilini ayarla
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

            // Örnek veri
            worksheet.Cell(2, 1).Value = "Örnek Ürün";
            worksheet.Cell(2, 2).Value = "Ürün açıklaması";
            worksheet.Cell(2, 3).Value = 99.99;
            worksheet.Cell(2, 4).Value = 100;
            worksheet.Cell(2, 5).Value = 1;
            worksheet.Cell(2, 6).Value = "/uploads/products/ornek.jpg";

            // Sütun genişliklerini ayarla
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(), 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                "urun_sablonu.xlsx");
        }

        [HttpPost("{id}/review")]
        [Authorize]
        public async Task<IActionResult> AddReview(int id, [FromBody] ProductReviewCreateDto reviewDto)
        {
            if (reviewDto == null) return BadRequest("Review body is required.");

            reviewDto.ProductId = id;
            var userId = User.GetUserId();

            try
            {
                await _productService.AddProductReviewAsync(id, userId, reviewDto);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/favorite")]
        [Authorize]
        public async Task<IActionResult> AddToFavorite(int id)
        {
            await _productService.AddFavoriteAsync(User.GetUserId(), id);
            return Ok();
        }

        /// <summary>
        /// Ürün resmi yükler (multipart/form-data)
        /// Bilgisayardan resim dosyası seçilerek uploads/products klasörüne kaydedilir.
        /// </summary>
        /// <param name="image">Yüklenecek resim dosyası (jpg, jpeg, png, gif, webp)</param>
        /// <returns>Yüklenen dosyanın URL'ini döner</returns>
        [HttpPost("upload/image")]
        [Authorize(Roles = Roles.AdminLike)]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            _logger.LogInformation("📤 Ürün resmi yükleme başlatılıyor");
            
            try
            {
                // Dosya var mı kontrolü
                if (image == null || image.Length == 0)
                {
                    _logger.LogWarning("⚠️ Dosya seçilmedi");
                    return BadRequest(new { message = "Lütfen bir resim dosyası seçin." });
                }

                // Dosya boyutu kontrolü
                if (image.Length > MaxFileSize)
                {
                    _logger.LogWarning("⚠️ Dosya çok büyük: {Size}MB", image.Length / (1024 * 1024));
                    return BadRequest(new { message = $"Dosya boyutu maksimum {MaxFileSize / (1024 * 1024)}MB olabilir." });
                }

                // Dosya uzantısı kontrolü (whitelist yaklaşımı)
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("⚠️ Geçersiz dosya uzantısı: {Extension}", extension);
                    return BadRequest(new { message = $"Desteklenen dosya türleri: {string.Join(", ", AllowedExtensions)}" });
                }

                // MIME type kontrolü (güvenlik için ek katman)
                var mimeType = image.ContentType.ToLowerInvariant();
                if (!AllowedMimeTypes.Contains(mimeType))
                {
                    _logger.LogWarning("⚠️ Geçersiz MIME type: {MimeType}", mimeType);
                    return BadRequest(new { message = "Geçersiz dosya türü. Sadece resim dosyaları kabul edilir." });
                }

                // Dosyayı LocalFileStorage üzerinden yükle
                // Dosya adı: product_{timestamp}_{guid}.{ext} formatında oluşturulur
                string imageUrl;
                using (var stream = image.OpenReadStream())
                {
                    var fileName = $"product_{image.FileName}";
                    imageUrl = await _fileStorage.UploadAsync(stream, fileName, image.ContentType);
                }

                _logger.LogInformation("✅ Ürün resmi yüklendi: {ImageUrl}", imageUrl);

                // Başarılı yanıt - yüklenen dosyanın URL'ini döndür
                return Ok(new { 
                    success = true,
                    imageUrl = imageUrl,
                    message = "Resim başarıyla yüklendi."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ürün resmi yüklenirken hata oluştu");
                return StatusCode(500, new { message = "Resim yüklenirken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }
    }

    // DTO sınıfları
    public class StockUpdateDto
    {
        public int Stock { get; set; }
    }
}
