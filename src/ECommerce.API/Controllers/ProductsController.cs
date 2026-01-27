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
        public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int size = 100, [FromQuery] int? categoryId = null)
        {
            // Default size 100 - ana sayfa tüm ürünleri gösterebilsin
            // Kampanya bilgileriyle birlikte getir
            var products = await _productService.GetActiveProductsWithCampaignAsync(page, size, categoryId);
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
            // Kampanya bilgileriyle birlikte getir
            var product = await _productService.GetProductByIdWithCampaignAsync(id);
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
        // Desteklenen formatlar: .xlsx, .xls, .csv
        // Şablon sütunları: Ürün Adı, Açıklama, Fiyat, Stok, Kategori ID, Görsel URL, İndirimli Fiyat, Ağırlık
        // Maksimum 500 ürün yüklenebilir (performans sınırı)
        [HttpPost("import/excel")]
        [Authorize(Roles = Roles.AdminLike)]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50MB - büyük Excel dosyaları için
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            _logger.LogInformation("📥 Excel import işlemi başlatılıyor");
            
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls" && extension != ".csv")
                return BadRequest(new { message = "Sadece Excel (.xlsx, .xls) veya CSV (.csv) dosyaları kabul edilir." });

            try
            {
                var products = new List<ProductCreateDto>();
                const int maxProducts = 500; // Performans sınırı

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    if (extension == ".csv")
                    {
                        // CSV işleme - UTF-8 encoding ile Türkçe karakter desteği
                        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                        var headerLine = await reader.ReadLineAsync();
                        if (headerLine == null)
                            return BadRequest(new { message = "CSV dosyası boş." });

                        int lineNumber = 1;
                        while (!reader.EndOfStream && products.Count < maxProducts)
                        {
                            lineNumber++;
                            var line = await reader.ReadLineAsync();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var values = line.Split(',');
                            if (values.Length < 5)
                            {
                                _logger.LogWarning("⚠️ Satır {Line}: Yetersiz sütun sayısı, atlanıyor", lineNumber);
                                continue;
                            }

                            // ImageUrl işleme - sadece dosya adı verilmişse /uploads/products/ ekle
                            var imageUrl = values.Length > 5 ? values[5].Trim().Trim('"') : null;
                            if (!string.IsNullOrWhiteSpace(imageUrl))
                            {
                                // Eğer tam yol değilse (http/https veya / ile başlamıyorsa) uploads/products/ ekle
                                if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) && 
                                    !imageUrl.StartsWith("/"))
                                {
                                    imageUrl = $"/uploads/products/{imageUrl}";
                                }
                            }

                            var productDto = new ProductCreateDto
                            {
                                Name = values[0].Trim().Trim('"'),
                                Description = values.Length > 1 ? values[1].Trim().Trim('"') : "",
                                Price = decimal.TryParse(values[2].Trim().Replace(',', '.'), 
                                    System.Globalization.NumberStyles.Any, 
                                    System.Globalization.CultureInfo.InvariantCulture, 
                                    out var price) ? price : 0,
                                Stock = int.TryParse(values[3].Trim(), out var stock) ? stock : 0,
                                CategoryId = int.TryParse(values[4].Trim(), out var catId) ? catId : 1,
                                ImageUrl = imageUrl,
                                SpecialPrice = values.Length > 6 && decimal.TryParse(values[6].Trim().Replace(',', '.'),
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out var specialPrice) ? specialPrice : (decimal?)null
                            };

                            // Validasyon: İsim zorunlu, fiyat pozitif olmalı
                            if (string.IsNullOrWhiteSpace(productDto.Name))
                            {
                                _logger.LogWarning("⚠️ Satır {Line}: Ürün adı boş, atlanıyor", lineNumber);
                                continue;
                            }
                            if (productDto.Price <= 0)
                            {
                                _logger.LogWarning("⚠️ Satır {Line}: Geçersiz fiyat ({Price}), atlanıyor", lineNumber, values[2]);
                                continue;
                            }

                            products.Add(productDto);
                        }
                    }
                    else
                    {
                        // Excel işleme (.xlsx, .xls)
                        using var workbook = new XLWorkbook(stream);
                        var worksheet = workbook.Worksheets.First();
                        var rows = worksheet.RowsUsed().Skip(1); // İlk satır başlık

                        int rowNumber = 1;
                        foreach (var row in rows)
                        {
                            rowNumber++;
                            if (products.Count >= maxProducts) break;

                            var name = row.Cell(1).GetString()?.Trim();
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                _logger.LogWarning("⚠️ Excel satır {Row}: Ürün adı boş, atlanıyor", rowNumber);
                                continue;
                            }

                            // Fiyat kontrolü
                            if (!row.Cell(3).TryGetValue<decimal>(out var price) || price <= 0)
                            {
                                _logger.LogWarning("⚠️ Excel satır {Row}: Geçersiz fiyat, atlanıyor", rowNumber);
                                continue;
                            }

                            // İndirimli fiyat (opsiyonel - 7. sütun)
                            decimal? specialPrice = null;
                            if (row.Cell(7).TryGetValue<decimal>(out var sp) && sp > 0)
                            {
                                specialPrice = sp;
                            }

                            // ImageUrl işleme - sadece dosya adı verilmişse /uploads/products/ ekle
                            var imageUrl = row.Cell(6).GetString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(imageUrl))
                            {
                                // Eğer tam yol değilse (http/https veya / ile başlamıyorsa) uploads/products/ ekle
                                if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) && 
                                    !imageUrl.StartsWith("/"))
                                {
                                    imageUrl = $"/uploads/products/{imageUrl}";
                                }
                            }

                            products.Add(new ProductCreateDto
                            {
                                Name = name,
                                Description = row.Cell(2).GetString()?.Trim() ?? "",
                                Price = price,
                                Stock = row.Cell(4).TryGetValue<int>(out var stock) ? stock : 0,
                                CategoryId = row.Cell(5).TryGetValue<int>(out var catId) ? catId : 1,
                                ImageUrl = imageUrl,
                                SpecialPrice = specialPrice
                                // Not: Ağırlık (8. sütun) ProductCreateDto'ya eklenebilir
                            });
                        }
                    }
                }

                if (products.Count == 0)
                    return BadRequest(new { message = "Dosyada geçerli ürün bulunamadı. Lütfen şablonu kontrol edin." });

                _logger.LogInformation("📋 {Count} ürün işlenecek", products.Count);

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
                        var errorMsg = $"'{productDto.Name}': {ex.Message}";
                        errors.Add(errorMsg);
                        _logger.LogWarning("⚠️ Ürün oluşturulamadı: {Error}", errorMsg);
                    }
                }

                _logger.LogInformation("✅ Excel import tamamlandı. {Success}/{Total} ürün eklendi", 
                    createdCount, products.Count);

                return Ok(new
                {
                    success = true,
                    message = $"{createdCount} ürün başarıyla eklendi.",
                    totalProcessed = products.Count,
                    successCount = createdCount,
                    errorCount = errors.Count,
                    errors = errors.Take(20).ToList(), // İlk 20 hatayı göster
                    warning = products.Count >= 500 ? "Maksimum 500 ürün limiti nedeniyle bazı satırlar atlanmış olabilir." : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excel import sırasında hata oluştu");
                return BadRequest(new { message = $"Dosya işlenirken hata oluştu: {ex.Message}" });
            }
        }

        // Excel şablonu indir
        // Türkçe karakterli örnek veriler ve detaylı açıklamalar içerir
        [HttpGet("import/template")]
        [Authorize(Roles = Roles.AdminLike)]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            
            // Ana şablon sayfası
            var worksheet = workbook.Worksheets.Add("Ürünler");

            // Başlıklar - 1. satır
            worksheet.Cell(1, 1).Value = "Ürün Adı *";
            worksheet.Cell(1, 2).Value = "Açıklama";
            worksheet.Cell(1, 3).Value = "Fiyat (TL) *";
            worksheet.Cell(1, 4).Value = "Stok Adedi *";
            worksheet.Cell(1, 5).Value = "Kategori ID *";
            worksheet.Cell(1, 6).Value = "Görsel URL";
            worksheet.Cell(1, 7).Value = "İndirimli Fiyat (TL)";
            worksheet.Cell(1, 8).Value = "Ağırlık (gram)";

            // Başlık stilini ayarla - Koyu mavi arkaplan, beyaz yazı
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Zorunlu alan başlıklarını kırmızı yap
            worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.DarkRed;
            worksheet.Cell(1, 3).Style.Fill.BackgroundColor = XLColor.DarkRed;
            worksheet.Cell(1, 4).Style.Fill.BackgroundColor = XLColor.DarkRed;
            worksheet.Cell(1, 5).Style.Fill.BackgroundColor = XLColor.DarkRed;

            // Türkçe karakterli örnek veriler - 2-6. satırlar
            // Örnek 1: Dana Kuşbaşı
            worksheet.Cell(2, 1).Value = "Dana Kuşbaşı 500gr";
            worksheet.Cell(2, 2).Value = "Taze kesim dana kuşbaşı eti, günlük taze";
            worksheet.Cell(2, 3).Value = 289.90;
            worksheet.Cell(2, 4).Value = 50;
            worksheet.Cell(2, 5).Value = 1;
            worksheet.Cell(2, 6).Value = "/uploads/products/dana-kusbaşı.jpg";
            worksheet.Cell(2, 7).Value = 259.90;
            worksheet.Cell(2, 8).Value = 500;

            // Örnek 2: Şekerpare
            worksheet.Cell(3, 1).Value = "Şekerpare 1kg";
            worksheet.Cell(3, 2).Value = "Geleneksel Türk tatlısı, taze üretim";
            worksheet.Cell(3, 3).Value = 149.90;
            worksheet.Cell(3, 4).Value = 30;
            worksheet.Cell(3, 5).Value = 2;
            worksheet.Cell(3, 6).Value = "/uploads/products/sekerpare.jpg";
            worksheet.Cell(3, 7).Value = "";
            worksheet.Cell(3, 8).Value = 1000;

            // Örnek 3: Çökelek Peyniri
            worksheet.Cell(4, 1).Value = "Çökelek Peyniri 250gr";
            worksheet.Cell(4, 2).Value = "Köy tipi doğal çökelek, katkısız";
            worksheet.Cell(4, 3).Value = 59.90;
            worksheet.Cell(4, 4).Value = 100;
            worksheet.Cell(4, 5).Value = 3;
            worksheet.Cell(4, 6).Value = "";
            worksheet.Cell(4, 7).Value = "";
            worksheet.Cell(4, 8).Value = 250;

            // Örnek 4: Tütsülenmiş Sığır Pastırması
            worksheet.Cell(5, 1).Value = "Tütsülenmiş Sığır Pastırması 200gr";
            worksheet.Cell(5, 2).Value = "Özel baharatlarla hazırlanmış pastırma";
            worksheet.Cell(5, 3).Value = 189.90;
            worksheet.Cell(5, 4).Value = 25;
            worksheet.Cell(5, 5).Value = 1;
            worksheet.Cell(5, 6).Value = "/uploads/products/pastirma.jpg";
            worksheet.Cell(5, 7).Value = 169.90;
            worksheet.Cell(5, 8).Value = 200;

            // Örnek 5: Kaşar Peyniri
            worksheet.Cell(6, 1).Value = "Şek Kaşar Peyniri 500gr";
            worksheet.Cell(6, 2).Value = "Taze inek sütünden üretilmiş kaşar";
            worksheet.Cell(6, 3).Value = 129.90;
            worksheet.Cell(6, 4).Value = 75;
            worksheet.Cell(6, 5).Value = 3;
            worksheet.Cell(6, 6).Value = "";
            worksheet.Cell(6, 7).Value = "";
            worksheet.Cell(6, 8).Value = 500;

            // Sütun genişliklerini ayarla
            worksheet.Column(1).Width = 35; // Ürün Adı
            worksheet.Column(2).Width = 50; // Açıklama
            worksheet.Column(3).Width = 15; // Fiyat
            worksheet.Column(4).Width = 15; // Stok
            worksheet.Column(5).Width = 15; // Kategori ID
            worksheet.Column(6).Width = 40; // Görsel URL
            worksheet.Column(7).Width = 20; // İndirimli Fiyat
            worksheet.Column(8).Width = 15; // Ağırlık

            // Açıklamalar sayfası
            var helpSheet = workbook.Worksheets.Add("Açıklamalar");
            helpSheet.Cell(1, 1).Value = "ALAN AÇIKLAMALARI";
            helpSheet.Cell(1, 1).Style.Font.Bold = true;
            helpSheet.Cell(1, 1).Style.Font.FontSize = 14;

            helpSheet.Cell(3, 1).Value = "Alan Adı";
            helpSheet.Cell(3, 2).Value = "Zorunlu";
            helpSheet.Cell(3, 3).Value = "Açıklama";
            helpSheet.Cell(3, 4).Value = "Örnek Değer";
            
            var helpHeaderRow = helpSheet.Row(3);
            helpHeaderRow.Style.Font.Bold = true;
            helpHeaderRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Alan açıklamaları
            helpSheet.Cell(4, 1).Value = "Ürün Adı";
            helpSheet.Cell(4, 2).Value = "EVET";
            helpSheet.Cell(4, 3).Value = "Ürünün tam adı (max 200 karakter). Türkçe karakterler desteklenir.";
            helpSheet.Cell(4, 4).Value = "Dana Kuşbaşı 500gr";

            helpSheet.Cell(5, 1).Value = "Açıklama";
            helpSheet.Cell(5, 2).Value = "HAYIR";
            helpSheet.Cell(5, 3).Value = "Ürün açıklaması (max 1000 karakter)";
            helpSheet.Cell(5, 4).Value = "Taze kesim dana eti";

            helpSheet.Cell(6, 1).Value = "Fiyat (TL)";
            helpSheet.Cell(6, 2).Value = "EVET";
            helpSheet.Cell(6, 3).Value = "Normal satış fiyatı (ondalıklı sayı, örn: 99.90)";
            helpSheet.Cell(6, 4).Value = "289.90";

            helpSheet.Cell(7, 1).Value = "Stok Adedi";
            helpSheet.Cell(7, 2).Value = "EVET";
            helpSheet.Cell(7, 3).Value = "Mevcut stok miktarı (tam sayı)";
            helpSheet.Cell(7, 4).Value = "50";

            helpSheet.Cell(8, 1).Value = "Kategori ID";
            helpSheet.Cell(8, 2).Value = "EVET";
            helpSheet.Cell(8, 3).Value = "Ürünün ait olduğu kategori numarası";
            helpSheet.Cell(8, 4).Value = "1";

            helpSheet.Cell(9, 1).Value = "Görsel URL";
            helpSheet.Cell(9, 2).Value = "HAYIR";
            helpSheet.Cell(9, 3).Value = "Ürün görseli yolu. Boş bırakılabilir, sonra eklenebilir.";
            helpSheet.Cell(9, 4).Value = "/uploads/products/ornek.jpg";

            helpSheet.Cell(10, 1).Value = "İndirimli Fiyat";
            helpSheet.Cell(10, 2).Value = "HAYIR";
            helpSheet.Cell(10, 3).Value = "Varsa indirimli fiyat (boş = indirim yok)";
            helpSheet.Cell(10, 4).Value = "259.90";

            helpSheet.Cell(11, 1).Value = "Ağırlık (gram)";
            helpSheet.Cell(11, 2).Value = "HAYIR";
            helpSheet.Cell(11, 3).Value = "Ürün ağırlığı gram cinsinden";
            helpSheet.Cell(11, 4).Value = "500";

            // Önemli notlar
            helpSheet.Cell(13, 1).Value = "ÖNEMLİ NOTLAR:";
            helpSheet.Cell(13, 1).Style.Font.Bold = true;
            helpSheet.Cell(14, 1).Value = "• Zorunlu alanlar (*) mutlaka doldurulmalıdır";
            helpSheet.Cell(15, 1).Value = "• Türkçe karakterler (ğ, ü, ş, ö, ç, ı, İ) desteklenir";
            helpSheet.Cell(16, 1).Value = "• Fiyatlar için nokta (.) kullanın, virgül (,) değil";
            helpSheet.Cell(17, 1).Value = "• İlk satır başlık satırıdır, silmeyin";
            helpSheet.Cell(18, 1).Value = "• Maksimum 500 ürün yüklenebilir";

            helpSheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(), 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                "urun_sablonu.xlsx");
        }

        /// <summary>
        /// Mevcut ürünleri Excel dosyası olarak dışa aktarır.
        /// Admin panelinden tüm ürünleri indirmek için kullanılır.
        /// Türkçe karakterler UTF-8 encoding ile doğru şekilde kaydedilir.
        /// </summary>
        [HttpGet("export/excel")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> ExportToExcel()
        {
            try
            {
                _logger.LogInformation("📥 Ürün Excel export işlemi başlatılıyor");
                
                // Tüm ürünleri çek (sayfalama yok, tamamı)
                var products = await _productService.GetAllProductsAsync(1, 10000);
                var productList = products.ToList();
                
                if (productList == null || !productList.Any())
                {
                    _logger.LogWarning("⚠️ Export için ürün bulunamadı");
                    return NotFound(new { message = "Dışa aktarılacak ürün bulunamadı." });
                }

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Ürünler");

                // Başlıklar
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Ürün Adı";
                worksheet.Cell(1, 3).Value = "Açıklama";
                worksheet.Cell(1, 4).Value = "Fiyat (TL)";
                worksheet.Cell(1, 5).Value = "İndirimli Fiyat (TL)";
                worksheet.Cell(1, 6).Value = "Stok Adedi";
                worksheet.Cell(1, 7).Value = "Kategori ID";
                worksheet.Cell(1, 8).Value = "Kategori Adı";
                worksheet.Cell(1, 9).Value = "Görsel URL";
                worksheet.Cell(1, 10).Value = "SKU";
                worksheet.Cell(1, 11).Value = "Ağırlık (gram)";
                worksheet.Cell(1, 12).Value = "Aktif";
                worksheet.Cell(1, 13).Value = "Oluşturma Tarihi";

                // Başlık stili
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Font.FontColor = XLColor.White;
                headerRow.Style.Fill.BackgroundColor = XLColor.DarkBlue;
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Ürünleri yaz
                int row = 2;
                foreach (var product in productList)
                {
                    worksheet.Cell(row, 1).Value = product.Id;
                    worksheet.Cell(row, 2).Value = product.Name ?? "";
                    worksheet.Cell(row, 3).Value = product.Description ?? "";
                    worksheet.Cell(row, 4).Value = product.Price;
                    worksheet.Cell(row, 5).Value = product.SpecialPrice ?? 0;
                    worksheet.Cell(row, 6).Value = product.StockQuantity;
                    worksheet.Cell(row, 7).Value = product.CategoryId ?? 0;
                    worksheet.Cell(row, 8).Value = product.CategoryName ?? "";
                    worksheet.Cell(row, 9).Value = product.ImageUrl ?? "";
                    worksheet.Cell(row, 10).Value = ""; // SKU - ProductListDto'da yok
                    worksheet.Cell(row, 11).Value = 0; // Ağırlık - ProductListDto'da yok
                    worksheet.Cell(row, 12).Value = "Evet"; // IsActive - varsayılan
                    worksheet.Cell(row, 13).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm"); // Tarih
                    row++;
                }

                // Sütun genişliklerini ayarla
                worksheet.Column(1).Width = 8;   // ID
                worksheet.Column(2).Width = 40;  // Ürün Adı
                worksheet.Column(3).Width = 50;  // Açıklama
                worksheet.Column(4).Width = 15;  // Fiyat
                worksheet.Column(5).Width = 18;  // İndirimli Fiyat
                worksheet.Column(6).Width = 12;  // Stok
                worksheet.Column(7).Width = 12;  // Kategori ID
                worksheet.Column(8).Width = 20;  // Kategori Adı
                worksheet.Column(9).Width = 45;  // Görsel URL
                worksheet.Column(10).Width = 15; // SKU
                worksheet.Column(11).Width = 15; // Ağırlık
                worksheet.Column(12).Width = 10; // Aktif
                worksheet.Column(13).Width = 18; // Oluşturma Tarihi

                // Alternatif satır renklendirme (okunabilirlik için)
                for (int i = 2; i <= row - 1; i++)
                {
                    if (i % 2 == 0)
                    {
                        worksheet.Row(i).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }
                }

                // Dosya adı: urunler_YYYYMMDD_HHMMSS.xlsx
                var fileName = $"urunler_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                _logger.LogInformation("✅ Excel export tamamlandı. {Count} ürün dışa aktarıldı", row - 2);

                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excel export sırasında hata oluştu");
                return StatusCode(500, new { message = "Ürünler dışa aktarılırken bir hata oluştu." });
            }
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
