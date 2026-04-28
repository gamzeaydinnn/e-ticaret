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
using ECommerce.Infrastructure.Services.MicroServices;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
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
        private readonly IMikroDbService _mikroDbService;
        private readonly IProductRepository _productRepository;
        private readonly ECommerceDbContext _dbContext;

        // İzin verilen dosya türleri (güvenlik için whitelist yaklaşımı)
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff", ".tif", ".avif", ".svg" };
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp", "image/tiff", "image/avif", "image/svg+xml", "image/x-bmp" };
        
        // Maksimum dosya boyutu: 10MB
        private const long MaxFileSize = 10 * 1024 * 1024;
        // Import sırasında indirilen görsel başına max boyut: 15MB
        private const long MaxImportImageSize = 15 * 1024 * 1024;

        private readonly IHttpClientFactory _httpClientFactory;

        public ProductsController(
            IProductService productService, 
            IWebHostEnvironment environment,
            IFileStorage fileStorage,
            ILogger<ProductsController> logger,
            IMikroDbService mikroDbService,
            IProductRepository productRepository,
            ECommerceDbContext dbContext,
            IHttpClientFactory httpClientFactory)
        {
            _productService = productService;
            _mikroDbService = mikroDbService;
            _productRepository = productRepository;
            _environment = environment;
            _fileStorage = fileStorage;
            _logger = logger;
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
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
            // Mikro ERP aktifse: Mikro ürünlerini local DB kategori bilgisiyle birleştir
            if (_mikroDbService.IsConfigured)
            {
                var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);

                // Local DB'deki ürünleri SKU bazlı eşle (kategori, isim, fiyat override)
                var localAll = await _dbContext.Products
                    .Include(p => p.Category)
                    .AsNoTracking()
                    .ToListAsync();

                var skuToLocal = localAll
                    .Where(p => !string.IsNullOrEmpty(p.SKU))
                    .GroupBy(p => p.SKU!)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                // Slug → CategoryId eşlemesi (DB'deki gerçek kategori id'leri)
                var slugToId = await _dbContext.Categories
                    .AsNoTracking()
                    .ToDictionaryAsync(c => c.Slug, c => c.Id, StringComparer.OrdinalIgnoreCase);

                // Slug → CategoryName eşlemesi
                var slugToName = await _dbContext.Categories
                    .AsNoTracking()
                    .ToDictionaryAsync(c => c.Slug, c => c.Name, StringComparer.OrdinalIgnoreCase);

                // CategoryId → Slug ters eşleme (doğrulama için)
                var idToSlug = slugToId.ToDictionary(kv => kv.Value, kv => kv.Key);

                // NEDEN: MikroCategoryMapping tablosundan AnagrupKod → CategoryId eşlemesi.
                // Otomatik kategori sistemi (ADIM 4-10) bu tabloya yazar.
                var anagrupMappings = await _dbContext.MikroCategoryMappings
                    .AsNoTracking()
                    .Where(m => m.IsActive)
                    .ToDictionaryAsync(
                        m => m.MikroAnagrupKod,
                        m => m.CategoryId,
                        StringComparer.OrdinalIgnoreCase);

                var merged = unified
                    .Select(p =>
                    {
                        var hasLocal = skuToLocal.TryGetValue(p.StokKod, out var local);

                        // NEDEN: Kategori çözümleme önceliği:
                        // 1. Mikro AnagrupKod → MikroCategoryMapping tablosu (en güvenilir)
                        // 2. Ürün adından keyword eşleme (MatchCategorySlug)
                        // 3. Local DB CategoryId KULLANILMAZ — eski bug'lı sync yanlış atamış
                        int? catId = null;
                        string catName = string.Empty;

                        // Öncelik 1: AnagrupKod mapping
                        if (!string.IsNullOrWhiteSpace(p.AnagrupKod) &&
                            anagrupMappings.TryGetValue(p.AnagrupKod, out var mappedCatId))
                        {
                            catId = mappedCatId;
                            if (idToSlug.TryGetValue(mappedCatId, out var mappedSlug))
                                catName = slugToName.GetValueOrDefault(mappedSlug, string.Empty);
                        }
                        else
                        {
                            // Öncelik 2: Keyword tabanlı anlık eşleme
                            var matchedSlug = MatchCategorySlug(p.StokAd);
                            if (slugToId.TryGetValue(matchedSlug, out var resolvedId))
                            {
                                catId = resolvedId;
                                catName = slugToName.GetValueOrDefault(matchedSlug, string.Empty);
                            }
                        }

                        return new ProductListDto
                        {
                            Id = hasLocal ? local!.Id : 0,
                            // NEDEN: SKU, Id=0 ürünlerde detay sayfasına yönlendirmek için kullanılır
                            Sku = p.StokKod,
                            Name = hasLocal && !string.IsNullOrEmpty(local!.Name) ? local.Name : p.StokAd,
                            Slug = hasLocal && !string.IsNullOrEmpty(local!.Slug) ? local.Slug : GenerateSlug(p.StokAd),
                            Description = hasLocal && !string.IsNullOrEmpty(local!.Description) ? local.Description : string.Empty,
                            Price = hasLocal && local!.Price > 0 ? local.Price : p.Fiyat,
                            SpecialPrice = hasLocal ? local!.SpecialPrice : null,
                            StockQuantity = (int)Math.Max(0, p.StokMiktar),
                            ImageUrl = hasLocal && !string.IsNullOrEmpty(local!.ImageUrl) ? local.ImageUrl : string.Empty,
                            // NEDEN: Mikro ERP birim bilgisi — KG ürünlerde frontend ağırlık seçici gösterir
                            Unit = string.IsNullOrWhiteSpace(p.Birim) ? "ADET" : p.Birim.Trim().ToUpperInvariant(),
                            CategoryId = catId,
                            CategoryName = catName,
                        };
                    })
                    .Where(p => p.Price > 0) // Fiyatsız ürünleri gösterme
                    .ToList();

                // Kategoriye göre filtrele
                if (categoryId.HasValue)
                    merged = merged.Where(p => p.CategoryId == categoryId.Value).ToList();

                // Sırala ve sayfalandır
                var result = merged
                    .OrderByDescending(p => p.HasActiveCampaign)
                    .ThenByDescending(p => p.DiscountPercentage ?? 0)
                    .ThenBy(p => p.Name)
                    .Skip((Math.Max(1, page) - 1) * Math.Max(1, size))
                    .Take(Math.Max(1, size))
                    .ToList();

                return Ok(result);
            }

            // Mikro yoksa sadece local DB
            var products = await _productService.GetActiveProductsWithCampaignAsync(page, size, categoryId);
            return Ok(products);
        }

        // Admin panel için tüm ürünleri getir — Mikro ERP SQL tabanlı (aktif ürünler)
        [HttpGet("admin/all")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> GetAllProductsForAdmin([FromQuery] int page = 1, [FromQuery] int size = 100)
        {
            if (_mikroDbService.IsConfigured)
            {
                var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);

                // Yerel DB'deki ürünleri SKU bazlı eşle — yerel override katmanı
                var localAll = (await _productRepository.GetAllAsync()).ToList();
                var skuToLocal = localAll
                    .Where(p => !string.IsNullOrEmpty(p.SKU))
                    .GroupBy(p => p.SKU!)
                    .ToDictionary(g => g.Key, g => g.First());

                var paged = unified
                    .Where(p => p.WebeGonderilecekFl)  // sadece web'e gönderilecek (aktif) ürünler
                    .OrderBy(p => p.StokKod)
                    .Skip((Math.Max(1, page) - 1) * Math.Max(1, size))
                    .Take(Math.Max(1, size))
                    .Select(p =>
                    {
                        // Yerel DB kaydı varsa override olarak kullan
                        var hasLocal = skuToLocal.TryGetValue(p.StokKod, out var local);
                        return new
                        {
                            id = hasLocal ? local!.Id : 0,
                            sku = p.StokKod,
                            // Yerel DB'de isim değiştirilmişse onu göster
                            name = hasLocal && !string.IsNullOrEmpty(local!.Name) ? local.Name : p.StokAd,
                            // Fiyat: Mikro ERP her zaman esas kaynak — yerel DB sadece Mikro 0 ise fallback
                            // NEDEN: Local DB eski sync verisini tutabilir; admin panelde güncel Mikro fiyatı gösterilmeli
                            price = p.Fiyat > 0 ? p.Fiyat : (hasLocal ? local!.Price : 0m),
                            specialPrice = hasLocal ? local!.SpecialPrice : (decimal?)null,
                            stockQuantity = (int)Math.Max(0, p.StokMiktar),
                            stock = (int)Math.Max(0, p.StokMiktar),
                            isActive = p.WebeGonderilecekFl,
                            // Kategori: yerel override varsa yerel
                            categoryId = hasLocal && local!.CategoryId > 0 ? (int?)local.CategoryId : (int?)null,
                            categoryCode = p.GrupKod,
                            anagrupCode = p.AnagrupKod,
                            // Açıklama: yerel override
                            description = hasLocal && !string.IsNullOrEmpty(local!.Description) ? local.Description : string.Empty,
                            imageUrl = hasLocal && !string.IsNullOrEmpty(local!.ImageUrl) ? local.ImageUrl : string.Empty,
                            source = "mikro-erp"
                        };
                    })
                    .ToList();

                return Ok(paged);
            }

            var products = await _productService.GetAllProductsAsync(page, size);
            return Ok(products);
        }

        /// <summary>
        /// Mikro ERP ürünlerini otomatik kategorize eder.
        /// Ürün adına göre akıllı eşleme yapar, eksik kategorileri oluşturur,
        /// yerel DB'de ürün kaydı yoksa SKU bazlı upsert yapar.
        /// </summary>
        [HttpPost("admin/auto-categorize")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> AutoCategorizeProducts()
        {
            if (!_mikroDbService.IsConfigured)
                return BadRequest(new { message = "Mikro ERP bağlantısı yapılandırılmamış." });

            var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
            if (unified.Count == 0)
                return Ok(new { message = "Mikro ERP'de ürün bulunamadı.", created = 0, updated = 0 });

            // 1) Mevcut kategorileri al, eksik olanları oluştur
            var existingCategories = await _dbContext.Categories.AsNoTracking().ToListAsync();
            var catBySlug = existingCategories.ToDictionary(c => c.Slug, c => c, StringComparer.OrdinalIgnoreCase);

            // Sayfadaki 8 ana kategori — tüm ürünler bunlara dağıtılır
            var requiredCategories = new Dictionary<string, (string Name, string Desc, int Sort)>
            {
                ["et-ve-et-urunleri"]     = ("Et ve Et Ürünleri",     "Taze et, tavuk, balık, şarküteri ve deniz ürünleri", 1),
                ["sut-ve-sut-urunleri"]   = ("Süt Ürünleri",          "Süt, peynir, yoğurt, tereyağı, kahvaltılık ürünler", 2),
                ["meyve-ve-sebze"]        = ("Meyve ve Sebze",        "Taze meyve, sebze ve yeşillikler", 3),
                ["icecekler"]             = ("İçecekler",             "Su, meyve suyu, gazlı içecek, çay, kahve", 4),
                ["atistirmalik"]          = ("Atıştırmalık",          "Cips, çikolata, kuruyemiş, bisküvi, dondurma", 5),
                ["temizlik"]              = ("Temizlik",              "Ev temizlik, kişisel bakım ve bebek ürünleri", 6),
                ["temel-gida"]            = ("Temel Gıda",           "Un, şeker, makarna, bakliyat, baharat, konserve, yağ, unlu mamüller", 7),
                ["ev-ve-mutfak"]          = ("Ev & Mutfak Gereçleri", "Tencere, bardak, ev gereçleri, mutfak malzemeleri", 8),
            };

            var newCategories = new List<Category>();
            foreach (var (slug, info) in requiredCategories)
            {
                if (!catBySlug.ContainsKey(slug))
                {
                    var cat = new Category
                    {
                        Name = info.Name,
                        Description = info.Desc,
                        Slug = slug,
                        SortOrder = info.Sort,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    newCategories.Add(cat);
                }
            }

            if (newCategories.Count > 0)
            {
                await _dbContext.Categories.AddRangeAsync(newCategories);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("[AutoCategorize] {Count} yeni kategori oluşturuldu", newCategories.Count);
            }

            // Güncel kategori listesini tekrar çek
            var allCategories = await _dbContext.Categories.ToListAsync();
            var slugToId = allCategories.ToDictionary(c => c.Slug, c => c.Id, StringComparer.OrdinalIgnoreCase);

            // 2) Ürün adı anahtar kelime → 8 ana kategori eşleme kuralları
            // Öncelik sırasına göre — ilk eşleşen kazanır
            // NEDEN: MatchesKeyword fonksiyonu kullanılıyor — kısa keyword'ler (ET, SU, UN) için
            // tam kelime eşleme yapılır, "ADET"→"ET" gibi yanlış eşleşmeler önlenir
            var keywordRules = new List<(string[] Keywords, string CategorySlug)>
            {
                // 1) Et ve Et Ürünleri — tüm et, tavuk, balık, şarküteri
                (new[] { "DANA", "KUZU", "KOYUN", "KÖFTE", "SUCUK", "SOSIS", "PASTIRMA", "SALAM", "JAMBON",
                         "KAVURMA", "BUT", "PIRZOLA", "ANTRIKOT", "BIFTEK", "KUŞBAŞI", "KIYMA", "BONFILE",
                         "TAVUK", "HINDI", "PİLİÇ", "PILIC", "KANAT", "GÖĞÜS", "BAGET", "BALIK", "SOMON", "TON BALIK",
                         "LEVREK", "ÇUPRA", "HAMSI", "KARIDES", "MİDYE", "ALABALIK", "BANVIT" }, "et-ve-et-urunleri"),

                // 2) Süt Ürünleri — süt, peynir, yoğurt + kahvaltılık
                (new[] { "SÜT", "PEYNİR", "YOĞURT", "YOGURT", "AYRAN", "KEFIR", "KEFİR", "KREMA", "KAYMAK",
                         "TEREYAĞ", "MARGARİN", "LABNE", "LOR", "KAŞAR", "BEYAZ PEYNİR", "TULUM",
                         "ÇÖKELEK", "KEÇİ PEYNİR", "ERİTME", "ÇEDDİR", "MOZZARELLA",
                         "ZEYTİN", "REÇEL", "TAHİN", "PEKMEZ", "HELVA", "NUTELLA", "FINDIK EZM",
                         "FISTIK EZM", "KAKAOLU", "ÇOKREM", "YUMURTA" }, "sut-ve-sut-urunleri"),

                // 3) Meyve ve Sebze
                (new[] { "MARUL", "DOMATES", "BİBER", "PATLICAN", "KABAK", "SALATALIK", "SOĞAN", "PATATES",
                         "HAVUÇ", "LAHANA", "ISPANAK", "MAYDANOZ", "DEREOTU", "NANE", "ROKA", "MEYVE",
                         "ELMA", "ARMUT", "PORTAKAL", "MANDALİNA", "LİMON", "MUZ", "ÜZÜ", "KARPUZ",
                         "KAVUN", "ÇİLEK", "VİŞNE", "KİRAZ", "KAYISI", "ŞEFTALİ", "ERIK", "İNCİR",
                         "NAR", "AVOKADO", "KİVİ", "ANANAS", "TURP", "KEREVIZ", "ENGINAR", "FASULYE YEŞ",
                         "BEZELYE", "BAMYA", "MANTAR", "BROKOLI", "KARNABAHAR", "PAZI", "SEMİZOTU",
                         "SEBZE", "SARIMSAK", "ZENCEFİL" }, "meyve-ve-sebze"),

                // 4) İçecekler
                (new[] { "MADEN SU", "SODA", "KOLA", "FANTA", "SPRITE", "PEPSI", "MEYVE SUYU",
                         "ÇAY", "KAHVE", "NESCAFE", "LİMONATA", "ŞALGAM", "ENERJİ",
                         "GAZLI", "GAZOZ", "ULUDAĞ", "İÇECEK", "JUS", "ŞERBET" }, "icecekler"),

                // 5) Atıştırmalık
                (new[] { "CİPS", "ÇİKOLATA", "DRAJE", "LOKUM", "SAKIZ", "ATIŞT", "JELIBON",
                         "BADEM", "CEVİZ", "KAJU", "AYÇEKİRDEĞİ", "KABAK ÇEKİR",
                         "LEBLEBI", "KURUYEMİŞ",
                         "DONDURMA", "DONMUŞ", "DONDURULMUŞ", "FROZEN", "BUZLU",
                         "BİSKÜVİ", "GOFRET", "KRAKER", "KURABİYE", "GALETA",
                         "WAFER" }, "atistirmalik"),

                // 6) Temizlik
                (new[] { "DETERJAN", "YUMUŞAT", "ÇAMAŞIR", "BULAŞIK", "CİF", "HİJYEN",
                         "ÇÖP POŞET", "TUVALET KAĞ", "PEÇETE", "HAVLU KAĞIT", "MENDIL",
                         "TEMİZLİK", "DEZENFEKTAN",
                         "ŞAMPUAN", "SABUN", "DİŞ MACUNU", "DEODORANT", "TRAŞ", "DUŞ JEL",
                         "SAÇKREM", "LOSYON", "PARFÜM",
                         "BEBEK", "MAMA", "BİBERON", "ÇOCUK BEZ" }, "temizlik"),

                // 7) Ev & Mutfak Gereçleri — gıda dışı ürünler
                (new[] { "TENCERE", "TAVA", "BARDAK", "TABAK", "ÇATAL", "KAŞIK", "BIÇAK",
                         "KASE", "SÜZGEÇ", "KEPÇE", "SPATULA", "RENDE", "TIRBUŞON",
                         "PORSELEN", "CAM", "TERMOS", "MATARA", "ŞİŞE",
                         "İĞNE", "ÇENGEL", "MANDAL", "SÜNGER", "FIRÇA", "PASPAS",
                         "ÇAKMAK", "MUM", "POŞET", "STREÇ", "FOLYO", "ALÜMİNYUM",
                         "MEZZE", "SERVİS", "TEPSI", "SAHAN", "GÜVEÇ" }, "ev-ve-mutfak"),

                // 8) Temel Gıda — un, şeker, makarna, bakliyat, baharat, konserve, yağ, ekmek, börek
                (new[] { "ŞEKER", "TUZ", "PİRİNÇ", "MAKARNA", "BULGUR", "YAYLA", "NİŞASTA",
                         "MAYA", "İRMİK", "EKMEK UNU", "ÇORBA", "YULAF", "MÜSLI",
                         "MERCİMEK", "NOHUT", "FASULYE KURU", "KURU FASULYE", "BARBUNYA", "BÖRÜLCE", "BAKLA",
                         "BAHARAT", "KARABIBER", "KIMYON", "PUL BİBER", "KEKIK", "TARÇIN", "ZENCEFİL TOZ",
                         "ZERDEÇAL", "SUMAK", "DEFNE", "SALÇA", "KETÇAP", "MAYONEZ", "HARDAL",
                         "SOYA SOSU", "BİBER TOZ",
                         "ZEYTİNYAĞ", "AYÇİÇEK", "SIVI YAĞ", "MISIR YAĞI", "SİRKE", "ELMA SİRKE",
                         "ÜZÜM SİRKE", "BİTKİ YAĞI",
                         "KONSERVE", "TURŞU",
                         "EKMEK", "SİMİT", "POĞAÇA", "BÖREK", "PİDE", "LAVAS", "LAVAŞ", "BAZLAMA",
                         "AÇMA", "ÇÖREK" }, "temel-gida"),
            };

            // 3) Mevcut yerel ürünleri SKU bazlı indexle
            var localProducts = await _dbContext.Products.ToListAsync();
            var skuToLocal = localProducts
                .Where(p => !string.IsNullOrEmpty(p.SKU))
                .GroupBy(p => p.SKU!)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            int created = 0, updated = 0, categorized = 0;

            foreach (var mikro in unified)
            {
                // NEDEN: MatchCategorySlug ile aynı MatchesKeyword fonksiyonunu kullan
                // Eski Contains(kw.TrimEnd()) — "ADET"→"ET" yanlış eşleşmesine neden oluyordu
                string matchedSlug = MatchCategorySlug(mikro.StokAd);

                if (!slugToId.TryGetValue(matchedSlug, out var categoryId))
                    categoryId = slugToId.GetValueOrDefault("temel-gida", 1);

                if (skuToLocal.TryGetValue(mikro.StokKod, out var local))
                {
                    // Mevcut yerel ürün — kategoriyi her zaman güncelle (7 kategoriye yeniden dağıtım)
                    local.CategoryId = categoryId;
                    local.UpdatedAt = DateTime.UtcNow;
                    updated++;
                    categorized++;
                }
                else
                {
                    // Yeni yerel kayıt oluştur — Mikro verisi + doğru kategori
                    var newProduct = new Product
                    {
                        Name = mikro.StokAd,
                        SKU = mikro.StokKod,
                        Price = mikro.Fiyat,
                        StockQuantity = (int)Math.Max(0, mikro.StokMiktar),
                        CategoryId = categoryId,
                        Description = string.Empty,
                        ImageUrl = string.Empty,
                        IsActive = mikro.WebeGonderilecekFl,
                        Slug = GenerateSlug(mikro.StokAd),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.Products.Add(newProduct);
                    skuToLocal[mikro.StokKod] = newProduct; // Duplicate engelle
                    created++;
                    categorized++;
                }
            }

            await _dbContext.SaveChangesAsync();

            // NEDEN: SKU'su boş olan eski ürünler skuToLocal ile yakalanamaz
            // İkinci geçiş — tüm local ürünleri isimle yeniden kategorize et
            var allLocalProducts = await _dbContext.Products.ToListAsync();
            int recategorized = 0;
            foreach (var lp in allLocalProducts)
            {
                var correctSlug = MatchCategorySlug(lp.Name);
                if (slugToId.TryGetValue(correctSlug, out var correctCatId) && lp.CategoryId != correctCatId)
                {
                    lp.CategoryId = correctCatId;
                    lp.UpdatedAt = DateTime.UtcNow;
                    recategorized++;
                }
            }
            if (recategorized > 0)
            {
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("[AutoCategorize] İkinci geçiş: {Count} SKU'suz/yanlış kategorili ürün düzeltildi", recategorized);
            }

            // Eski kategori sisteminden kalan gereksiz kategorileri temizle
            // NOT: "diger" artık obsolete değil — MatchCategorySlug "temel-gida" fallback kullanır
            var obsoleteSlugs = new[] {
                "bakliyat-ve-tahil", "baharat-ve-sos", "kahvaltilik", "dondurulmus",
                "unlu-mamuller", "kisisel-bakim", "bebek-urunleri", "konserve-ve-hazir",
                "yag-ve-sirkeler", "kuruyemis"
            };
            var obsoleteCats = await _dbContext.Categories
                .Where(c => obsoleteSlugs.Contains(c.Slug))
                .ToListAsync();

            // Sadece ürün bağlı olmayanları sil (ürünler zaten 7 kategoriye taşındı)
            foreach (var obs in obsoleteCats)
            {
                var hasProducts = await _dbContext.Products.AnyAsync(p => p.CategoryId == obs.Id);
                if (!hasProducts)
                {
                    _dbContext.Categories.Remove(obs);
                    _logger.LogInformation("[AutoCategorize] Eski kategori silindi: {Name}", obs.Name);
                }
            }
            await _dbContext.SaveChangesAsync();

            // 4) Kategori bazlı dağılım raporu
            var distribution = unified
                .GroupBy(p => MatchCategorySlug(p.StokAd))
                .Select(g => new
                {
                    category = requiredCategories.TryGetValue(g.Key, out var info) ? info.Name : g.Key,
                    slug = g.Key,
                    productCount = g.Count()
                })
                .OrderByDescending(x => x.productCount)
                .ToList();

            _logger.LogInformation(
                "[AutoCategorize] Tamamlandı. Toplam: {Total}, Yeni: {Created}, Güncellenen: {Updated}, Kategorize: {Categorized}",
                unified.Count, created, updated, categorized);

            return Ok(new
            {
                message = $"Otomatik kategorileme tamamlandı.",
                totalProducts = unified.Count,
                created,
                updated,
                categorized,
                newCategories = newCategories.Count,
                distribution
            });
        }

        /// <summary>
        /// URL-friendly slug oluşturur (Türkçe karakter desteği).
        /// </summary>
        private static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var slug = text.ToLowerInvariant()
                .Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c")
                .Replace("İ", "i").Replace("Ğ", "g").Replace("Ü", "u")
                .Replace("Ş", "s").Replace("Ö", "o").Replace("Ç", "c");

            // Alfanumerik ve tire dışı karakterleri temizle
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s]+", "-");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
            return slug.Trim('-');
        }

        /// <summary>
        /// Ürün adını 7 ana kategoriden birine eşler (keyword tabanlı).
        /// Auto-categorize çalışmadan önce de anlık eşleme yapabilmek için.
        /// </summary>
        private static string MatchCategorySlug(string productName)
        {
            var name = productName.ToUpperInvariant();

            // NEDEN: Ürün adındaki kelimelere ayır — kısa keyword'ler (ET, SU, UN, BAL)
            // substring olarak eşleşmesin. "CORNETTO" → "ET" eşleşmesi bu yüzden oluyordu.
            var words = name.Split(
                new[] { ' ', ',', '.', '-', '/', '(', ')', '+', '*', '&', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            // NEDEN: Öncelik sırası önemli — ev-mutfak et'ten ÖNCE kontrol edilmeli
            // Aksi halde "BALIK TAVASI" → BALIK → et kategorisine düşer
            // Doğru: TAVA → ev-mutfak → sonra BALIK araması et'e düşmez
            var rules = new (string[] Keywords, string Slug)[]
            {
                // 1) Ev & Mutfak Gereçleri — EN ÖNCE kontrol et, gıda dışı ürünleri ayıkla
                // NEDEN: "BALIK TAVASI", "BALIK DESENLİ HAVUZ" gibi ürünler BALIK keyword'ü ile
                // et kategorisine düşmesin diye mutfak gereçleri önce kontrol edilir
                (new[] { "TENCERE", "TAVA", "BARDAK", "TABAK", "ÇATAL", "KAŞIK", "BIÇAK",
                         "KASE", "SÜZGEÇ", "KEPÇE", "SPATULA", "RENDE", "TIRBUŞON",
                         "PORSELEN", "CAM", "TERMOS", "MATARA", "ŞİŞE",
                         "İĞNE", "ÇENGEL", "MANDAL", "SÜNGER", "FIRÇA", "PASPAS",
                         "ÇAKMAK", "MUM", "POŞET", "STREÇ", "FOLYO", "ALÜMİNYUM",
                         "MEZZE", "SERVİS", "TEPSI", "SAHAN", "GÜVEÇ",
                         "HAVUZ", "DESENLİ" }, "ev-ve-mutfak"),

                // 2) Et ve Et Ürünleri — tavuk, balık, şarküteri
                (new[] { "DANA", "KUZU", "KOYUN", "KÖFTE", "SUCUK", "SOSIS", "PASTIRMA", "SALAM", "JAMBON",
                         "KAVURMA", "BUT", "PIRZOLA", "ANTRIKOT", "BIFTEK", "KUŞBAŞI", "KIYMA", "BONFILE",
                         "TAVUK", "HINDI", "PİLİÇ", "PILIC", "KANAT", "GÖĞÜS", "BAGET", "BALIK", "SOMON", "TON BALIK",
                         "LEVREK", "ÇUPRA", "HAMSI", "KARIDES", "MİDYE", "ALABALIK", "BANVIT" }, "et-ve-et-urunleri"),

                // 3) Süt Ürünleri — süt, peynir, yoğurt + kahvaltılık
                (new[] { "SÜT", "PEYNİR", "YOĞURT", "YOGURT", "AYRAN", "KEFIR", "KEFİR", "KREMA", "KAYMAK",
                         "TEREYAĞ", "MARGARİN", "LABNE", "LOR", "KAŞAR", "BEYAZ PEYNİR", "TULUM",
                         "ÇÖKELEK", "KEÇİ PEYNİR", "ERİTME", "ÇEDDİR", "MOZZARELLA",
                         "ZEYTİN", "REÇEL", "TAHİN", "PEKMEZ", "HELVA", "NUTELLA", "FINDIK EZM",
                         "FISTIK EZM", "KAKAOLU", "ÇOKREM", "YUMURTA" }, "sut-ve-sut-urunleri"),

                (new[] { "MARUL", "DOMATES", "BİBER", "PATLICAN", "KABAK", "SALATALIK", "SOĞAN", "PATATES",
                         "HAVUÇ", "LAHANA", "ISPANAK", "MAYDANOZ", "DEREOTU", "NANE", "ROKA", "MEYVE",
                         "ELMA", "ARMUT", "PORTAKAL", "MANDALİNA", "LİMON", "MUZ", "ÜZÜ", "KARPUZ",
                         "KAVUN", "ÇİLEK", "VİŞNE", "KİRAZ", "KAYISI", "ŞEFTALİ", "ERIK", "İNCİR",
                         "NAR", "AVOKADO", "KİVİ", "ANANAS", "TURP", "KEREVIZ", "ENGINAR", "FASULYE YEŞ",
                         "BEZELYE", "BAMYA", "MANTAR", "BROKOLI", "KARNABAHAR", "PAZI", "SEMİZOTU",
                         "SEBZE", "SARIMSAK", "ZENCEFİL" }, "meyve-ve-sebze"),

                (new[] { "MADEN SU", "SODA", "KOLA", "FANTA", "SPRITE", "PEPSI", "MEYVE SUYU",
                         "ÇAY", "KAHVE", "NESCAFE", "LİMONATA", "ŞALGAM", "ENERJİ",
                         "GAZLI", "GAZOZ", "ULUDAĞ", "İÇECEK", "JUS", "ŞERBET" }, "icecekler"),

                (new[] { "CİPS", "ÇİKOLATA", "DRAJE", "LOKUM", "SAKIZ", "ATIŞT", "JELIBON",
                         "BADEM", "CEVİZ", "KAJU", "AYÇEKİRDEĞİ", "KABAK ÇEKİR",
                         "LEBLEBI", "KURUYEMİŞ",
                         "DONDURMA", "DONMUŞ", "DONDURULMUŞ", "FROZEN", "BUZLU",
                         "BİSKÜVİ", "GOFRET", "KRAKER", "KURABİYE", "GALETA",
                         "WAFER" }, "atistirmalik"),

                (new[] { "DETERJAN", "YUMUŞAT", "ÇAMAŞIR", "BULAŞIK", "CİF", "HİJYEN",
                         "ÇÖP POŞET", "TUVALET KAĞ", "PEÇETE", "HAVLU KAĞIT", "MENDIL",
                         "TEMİZLİK", "DEZENFEKTAN",
                         "ŞAMPUAN", "SABUN", "DİŞ MACUNU", "DEODORANT", "TRAŞ", "DUŞ JEL",
                         "SAÇKREM", "LOSYON", "PARFÜM",
                         "BEBEK", "MAMA", "BİBERON", "ÇOCUK BEZ" }, "temizlik"),

                (new[] { "ŞEKER", "TUZ", "PİRİNÇ", "MAKARNA", "BULGUR", "YAYLA", "NİŞASTA",
                         "MAYA", "İRMİK", "EKMEK UNU", "ÇORBA", "YULAF", "MÜSLI",
                         "MERCİMEK", "NOHUT", "FASULYE KURU", "KURU FASULYE", "BARBUNYA", "BÖRÜLCE", "BAKLA",
                         "BAHARAT", "KARABIBER", "KIMYON", "PUL BİBER", "KEKIK", "TARÇIN", "ZENCEFİL TOZ",
                         "ZERDEÇAL", "SUMAK", "DEFNE", "SALÇA", "KETÇAP", "MAYONEZ", "HARDAL",
                         "SOYA SOSU", "BİBER TOZ",
                         "ZEYTİNYAĞ", "AYÇİÇEK", "SIVI YAĞ", "MISIR YAĞI", "SİRKE", "ELMA SİRKE",
                         "ÜZÜM SİRKE", "BİTKİ YAĞI",
                         "KONSERVE", "TURŞU",
                         "EKMEK", "SİMİT", "POĞAÇA", "BÖREK", "PİDE", "LAVAS", "LAVAŞ", "BAZLAMA",
                         "AÇMA", "ÇÖREK" }, "temel-gida"),
            };

            foreach (var (keywords, slug) in rules)
            {
                if (keywords.Any(kw => MatchesKeyword(name, words, kw)))
                    return slug;
            }
            // NEDEN: Eşleşmeyen ürünler "Temel Gıda"ya düşer — "diger" kategorisi silindiği için
            return "temel-gida";
        }

        /// <summary>
        /// Keyword eşleme: kısa keyword'ler (≤3 karakter) tam kelime eşleme,
        /// uzun keyword'ler substring eşleme kullanır.
        /// NEDEN: "ET" → "CORNETTO" false-positive'ini önlemek için.
        /// "ET" sadece bağımsız kelime olarak eşleşir: "DANA ET 500G" ✓, "CORNETTO" ✗
        /// </summary>
        private static bool MatchesKeyword(string fullName, string[] words, string keyword)
        {
            var kw = keyword.Trim();
            if (string.IsNullOrEmpty(kw)) return false;

            // Çok kelimeli keyword → substring (ör: "MADEN SU", "BEYAZ PEYNİR")
            if (kw.Contains(' '))
                return fullName.Contains(kw, StringComparison.OrdinalIgnoreCase);

            // Kısa keyword (≤3 karakter) → tam kelime eşleme
            // "ET" → words içinde "ET" var mı? "CORNETTO" kelimesinde yok.
            if (kw.Length <= 3)
                return words.Any(w => w.Equals(kw, StringComparison.OrdinalIgnoreCase));

            // Uzun keyword (>3 karakter) → substring eşleme güvenli
            return fullName.Contains(kw, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sayfalı ürün listesi döndürür (PagedResult formatında).
        /// Toplu export işlemleri için optimize edilmiştir.
        /// </summary>
        /// <param name="page">Sayfa numarası (1'den başlar)</param>
        /// <param name="size">Sayfa başına ürün sayısı (max: 100)</param>
        /// <returns>PagedResult formatında ürün listesi</returns>
        [HttpGet("admin/paged")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> GetProductsPaged([FromQuery] int page = 1, [FromQuery] int size = 50)
        {
            // Güvenlik: max sayfa boyutunu sınırla
            size = Math.Min(size, 100);
            page = Math.Max(page, 1);

            var pagedResult = await _productService.GetProductsPagedAsync(page, size);
            return Ok(pagedResult);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            // Kampanya bilgileriyle birlikte getir
            var product = await _productService.GetProductByIdWithCampaignAsync(id);
            if (product == null) return NotFound();

            // Local DB stok bilgisi stale olabilir — Mikro'dan gerçek zamanlı stok al
            if (_mikroDbService.IsConfigured && !string.IsNullOrEmpty(product.Sku))
            {
                var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
                var mikro = unified.FirstOrDefault(u =>
                    string.Equals(u.StokKod, product.Sku, StringComparison.OrdinalIgnoreCase));
                if (mikro != null)
                {
                    product.StockQuantity = (int)Math.Max(0, mikro.StokMiktar);
                    product.Unit = string.IsNullOrWhiteSpace(mikro.Birim) ? "ADET" : mikro.Birim.Trim().ToUpperInvariant();
                }
            }

            return Ok(product);
        }

        /// <summary>
        /// SKU ile ürün detayı getir — Mikro ERP'de local DB kaydı olmayan (Id=0) ürünler için.
        /// Frontend, Id=0 ürünlerde SKU ile bu endpoint'i çağırır.
        /// </summary>
        [HttpGet("sku/{sku}")]
        public async Task<IActionResult> GetProductBySku(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku)) return BadRequest();
            sku = sku.Trim();

            // Önce local DB'de ara
            var localProduct = await _dbContext.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SKU == sku, HttpContext.RequestAborted);

            if (localProduct != null)
            {
                var localDto = await _productService.GetProductByIdWithCampaignAsync(localProduct.Id);
                if (localDto != null)
                {
                    // Local DB stok bilgisi stale olabilir — Mikro'dan gerçek zamanlı stok al
                    if (_mikroDbService.IsConfigured)
                    {
                        var unifiedLocal = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
                        var mikroLocal = unifiedLocal.FirstOrDefault(u =>
                            string.Equals(u.StokKod, sku, StringComparison.OrdinalIgnoreCase));
                        if (mikroLocal != null)
                        {
                            localDto.StockQuantity = (int)Math.Max(0, mikroLocal.StokMiktar);
                            localDto.Unit = string.IsNullOrWhiteSpace(mikroLocal.Birim) ? "ADET" : mikroLocal.Birim.Trim().ToUpperInvariant();
                        }
                    }
                    return Ok(localDto);
                }
            }

            // Local DB'de yoksa Mikro'dan getir
            if (!_mikroDbService.IsConfigured) return NotFound();

            var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
            var mikro = unified.FirstOrDefault(u =>
                string.Equals(u.StokKod, sku, StringComparison.OrdinalIgnoreCase));

            if (mikro == null) return NotFound();

            // Mikro verisinden ProductListDto inşa et (detay sayfası bu formatı kullanır)
            var slugToId = await _dbContext.Categories
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Slug, c => c.Id, StringComparer.OrdinalIgnoreCase);
            var slugToName = await _dbContext.Categories
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Slug, c => c.Name, StringComparer.OrdinalIgnoreCase);

            var matchedSlug = MatchCategorySlug(mikro.StokAd);
            slugToId.TryGetValue(matchedSlug, out var catId);

            var dto = new ProductListDto
            {
                Id = 0,
                Sku = mikro.StokKod,
                Name = mikro.StokAd,
                Slug = GenerateSlug(mikro.StokAd),
                Description = string.Empty,
                Price = mikro.Fiyat,
                SpecialPrice = null,
                StockQuantity = (int)Math.Max(0, mikro.StokMiktar),
                ImageUrl = string.Empty,
                Unit = string.IsNullOrWhiteSpace(mikro.Birim) ? "ADET" : mikro.Birim.Trim().ToUpperInvariant(),
                CategoryId = catId > 0 ? catId : null,
                CategoryName = catId > 0 ? slugToName.GetValueOrDefault(matchedSlug, string.Empty) : string.Empty,
            };

            return Ok(dto);
        }

        /// <summary>
        /// Mikro ERP'den gelen (Id=0) ürünü local DB'ye kaydeder veya mevcutsa bulur.
        /// Sepete ekle öncesi frontend tarafından çağrılır — AllowAnonymous çünkü misafir sepeti destekler.
        /// </summary>
        [HttpPost("ensure-local/{sku}")]
        [AllowAnonymous]
        public async Task<IActionResult> EnsureLocalProduct(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku)) return BadRequest(new { message = "SKU gerekli." });
            sku = sku.Trim();

            // Önce local DB'de ara — zaten varsa direkt döndür
            var existing = await _dbContext.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SKU == sku, HttpContext.RequestAborted);

            if (existing != null)
                return Ok(new { id = existing.Id, sku = existing.SKU, name = existing.Name });

            // Mikro'dan getir
            if (!_mikroDbService.IsConfigured)
                return NotFound(new { message = "Mikro ERP bağlantısı yok." });

            var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
            var mikro = unified.FirstOrDefault(u =>
                string.Equals(u.StokKod, sku, StringComparison.OrdinalIgnoreCase));

            if (mikro == null)
                return NotFound(new { message = $"Ürün bulunamadı: {sku}" });

            // Kategori eşleme
            var slugToId = await _dbContext.Categories
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Slug, c => c.Id, StringComparer.OrdinalIgnoreCase);

            // MikroCategoryMapping önce, sonra keyword fallback
            var anagrupMappings = await _dbContext.MikroCategoryMappings
                .AsNoTracking()
                .ToDictionaryAsync(m => m.MikroAnagrupKod, m => m.CategoryId, StringComparer.OrdinalIgnoreCase);

            int categoryId = 0;
            var categoryCode = !string.IsNullOrWhiteSpace(mikro.AnagrupKod) ? mikro.AnagrupKod : mikro.GrupKod;
            if (!string.IsNullOrWhiteSpace(categoryCode) && anagrupMappings.TryGetValue(categoryCode, out var mappedCatId))
                categoryId = mappedCatId;

            if (categoryId == 0)
            {
                var matchedSlug = MatchCategorySlug(mikro.StokAd);
                slugToId.TryGetValue(matchedSlug, out categoryId);
            }

            // Fallback: ilk aktif kategori
            if (categoryId == 0)
                categoryId = await _dbContext.Categories.Where(c => c.IsActive).Select(c => c.Id).FirstOrDefaultAsync();

            // Slug çakışmasını önle — aynı isimli ürün varsa sonuna SKU ekle
            var baseSlug = GenerateSlug(mikro.StokAd);
            var slug = baseSlug;
            if (await _dbContext.Products.AnyAsync(p => p.Slug == slug))
                slug = $"{baseSlug}-{sku.ToLowerInvariant()}";

            var product = new Product
            {
                Name = mikro.StokAd,
                SKU = mikro.StokKod,
                Price = mikro.Fiyat,
                StockQuantity = (int)Math.Max(0, mikro.StokMiktar),
                CategoryId = categoryId,
                Description = string.Empty,
                ImageUrl = string.Empty,
                IsActive = true,
                Slug = slug,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("[EnsureLocal] Mikro ürün local DB'ye eklendi: {Sku} → Id={Id}", sku, product.Id);

            return Ok(new { id = product.Id, sku = product.SKU, name = product.Name });
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

        // SKU bazlı ürün güncelle/oluştur — Mikro ERP ürünlerinde id=0 olduğundan SKU ile eşleştir
        [HttpPut("by-sku/{sku}")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> UpdateProductBySku(string sku, [FromBody] ProductUpdateDto dto)
        {
            if (dto == null) return BadRequest("Ürün bilgileri gerekli.");
            if (string.IsNullOrWhiteSpace(sku)) return BadRequest(new { message = "SKU gerekli" });

            try
            {
                var product = await _productService.UpdateBySkuAsync(sku.Trim(), dto);
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

        // Excel/CSV dosyasından toplu ürün yükleme + görsel yükleme
        // Desteklenen formatlar: .xlsx, .xls, .csv
        // Görsel URL sütunu: HTTP/HTTPS URL (otomatik indirilir), yüklenen dosya adı, veya mevcut /uploads/ yolu
        // Aynı anda birden fazla görsel dosyası gönderilebilir — CSV'deki dosya adıyla eşleştirilir
        [HttpPost("import/excel")]
        [Authorize(Roles = Roles.AdminLike)]
        [RequestSizeLimit(200 * 1024 * 1024)] // 200MB — görseller dahil
        public async Task<IActionResult> ImportFromExcel(IFormFile file, [FromForm] IFormFileCollection? images = null)
        {
            _logger.LogInformation("📥 Excel import başlatılıyor — görsel sayısı: {ImageCount}", images?.Count ?? 0);

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls" && extension != ".csv")
                return BadRequest(new { message = "Sadece Excel (.xlsx, .xls) veya CSV (.csv) dosyaları kabul edilir." });

            try
            {
                // Yüklenen görselleri dosya adına göre indexle (büyük/küçük harf duyarsız)
                // CSV'deki ImageUrl sütunundaki dosya adıyla eşleştirilmek için
                var uploadedImageMap = new Dictionary<string, IFormFile>(StringComparer.OrdinalIgnoreCase);
                if (images != null)
                {
                    foreach (var img in images)
                    {
                        if (img.Length > 0)
                            uploadedImageMap[Path.GetFileName(img.FileName)] = img;
                    }
                }

                var products = new List<ProductCreateDto>();
                const int maxProducts = 500;

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    if (extension == ".csv")
                    {
                        // CSV işleme — UTF-8 + BOM desteği ile Türkçe karakter desteği
                        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                        var headerLine = await reader.ReadLineAsync();
                        if (headerLine == null)
                            return BadRequest(new { message = "CSV dosyası boş." });

                        int lineNumber = 1;
                        while (!reader.EndOfStream && products.Count < maxProducts)
                        {
                            lineNumber++;
                            var line = await reader.ReadLineAsync();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            // Virgül içeren değerleri desteklemek için RFC 4180 parse
                            var values = ParseCsvLine(line);
                            if (values.Length < 5)
                            {
                                _logger.LogWarning("⚠️ Satır {Line}: Yetersiz sütun sayısı, atlanıyor", lineNumber);
                                continue;
                            }

                            var rawImageUrl = values.Length > 5 ? values[5].Trim().Trim('"') : null;
                            var imageUrl = await ResolveImageUrlAsync(rawImageUrl, uploadedImageMap);

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
                        var rows = worksheet.RowsUsed().Skip(1);

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

                            if (!row.Cell(3).TryGetValue<decimal>(out var price) || price <= 0)
                            {
                                _logger.LogWarning("⚠️ Excel satır {Row}: Geçersiz fiyat, atlanıyor", rowNumber);
                                continue;
                            }

                            decimal? specialPrice = null;
                            if (row.Cell(7).TryGetValue<decimal>(out var sp) && sp > 0)
                                specialPrice = sp;

                            var rawImageUrl = row.Cell(6).GetString()?.Trim();
                            var imageUrl = await ResolveImageUrlAsync(rawImageUrl, uploadedImageMap);

                            products.Add(new ProductCreateDto
                            {
                                Name = name,
                                Description = row.Cell(2).GetString()?.Trim() ?? "",
                                Price = price,
                                Stock = row.Cell(4).TryGetValue<int>(out var stockVal) ? stockVal : 0,
                                CategoryId = row.Cell(5).TryGetValue<int>(out var catId) ? catId : 1,
                                ImageUrl = imageUrl,
                                SpecialPrice = specialPrice
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

                _logger.LogInformation("✅ Import tamamlandı: {Success}/{Total} ürün eklendi, {Images} görsel işlendi",
                    createdCount, products.Count, uploadedImageMap.Count);

                return Ok(new
                {
                    success = true,
                    message = $"{createdCount} ürün başarıyla eklendi.",
                    totalProcessed = products.Count,
                    successCount = createdCount,
                    errorCount = errors.Count,
                    imagesProcessed = uploadedImageMap.Count,
                    errors = errors.Take(20).ToList(),
                    warning = products.Count >= 500 ? "Maksimum 500 ürün limiti nedeniyle bazı satırlar atlanmış olabilir." : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excel import sırasında hata oluştu");
                return BadRequest(new { message = $"Dosya işlenirken hata oluştu: {ex.Message}" });
            }
        }

        /// <summary>
        /// CSV satırını RFC 4180'e göre ayrıştırır (tırnak içindeki virgülleri korur).
        /// </summary>
        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    // Çift tırnak escape: "" → "
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }

        /// <summary>
        /// Görsel URL'ini çözümler:
        /// 1) HTTP/HTTPS URL → görsel indirilir, /uploads/products/ altına kaydedilir
        /// 2) Yüklenen dosya adıyla eşleşirse → dosya kaydedilir, yol döndürülür
        /// 3) Sadece dosya adıysa → /uploads/products/{ad} yolu döndürülür
        /// 4) Zaten /uploads/ yoluyla başlıyorsa → olduğu gibi bırakılır
        /// </summary>
        private async Task<string?> ResolveImageUrlAsync(
            string? rawUrl,
            Dictionary<string, IFormFile> uploadedImages)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
                return null;

            // Durum 1: HTTP/HTTPS URL — görsel indir ve sisteme kaydet (SSRF korumalı)
            if (rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return await DownloadAndSaveImageAsync(rawUrl);
            }

            // Durum 2: Yüklenen görsel dosyalarından biriyle eşleşiyor
            var fileName = Path.GetFileName(rawUrl); // Tam yol verilmişse sadece dosya adını al
            if (!string.IsNullOrEmpty(fileName) && uploadedImages.TryGetValue(fileName, out var uploadedFile))
            {
                return await SaveUploadedImageAsync(uploadedFile);
            }

            // Durum 3: Zaten sistemdeki bir yol (/uploads/... ile başlıyor)
            if (rawUrl.StartsWith("/"))
                return rawUrl;

            // Durum 4: Sadece dosya adı — /uploads/products/ altında olduğu varsayılır
            return $"/uploads/products/{rawUrl}";
        }

        /// <summary>
        /// Harici URL'den görsel indirir, MIME tipi doğrular ve sisteme kaydeder.
        /// SSRF koruması: özel IP aralıkları engellenir.
        /// </summary>
        private async Task<string?> DownloadAndSaveImageAsync(string url)
        {
            try
            {
                // SSRF koruması: URI parse et ve özel ağ adreslerini engelle
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    _logger.LogWarning("⚠️ Geçersiz görsel URL'i: {Url}", url);
                    return null;
                }

                if (IsPrivateOrLocalUri(uri))
                {
                    _logger.LogWarning("⚠️ Güvenlik: İç ağ adresine görsel isteği engellendi: {Host}", uri.Host);
                    return null;
                }

                var httpClient = _httpClientFactory.CreateClient("ImageDownload");
                using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("⚠️ Görsel indirilemedi ({Status}): {Url}", (int)response.StatusCode, url);
                    return null;
                }

                // Content-Type doğrulaması — sadece bilinen resim formatları
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                if (!AllowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
                {
                    // Content-Type güvenilmez olabilir — uzantıdan da dene
                    var ext = Path.GetExtension(uri.LocalPath).ToLowerInvariant();
                    if (!AllowedExtensions.Contains(ext))
                    {
                        _logger.LogWarning("⚠️ Desteklenmeyen görsel tipi ({Type}): {Url}", contentType, url);
                        return null;
                    }
                    // Uzantı geçerliyse devam et, content type olarak text/plain gibi değerler olabilir
                    contentType = ext switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".png" => "image/png",
                        ".gif" => "image/gif",
                        ".webp" => "image/webp",
                        ".bmp" => "image/bmp",
                        ".tiff" or ".tif" => "image/tiff",
                        ".avif" => "image/avif",
                        ".svg" => "image/svg+xml",
                        _ => contentType
                    };
                }

                // Boyut kontrolü (max 15MB)
                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength.HasValue && contentLength.Value > MaxImportImageSize)
                {
                    _logger.LogWarning("⚠️ Görsel çok büyük ({Size}MB): {Url}",
                        contentLength.Value / 1024 / 1024, url);
                    return null;
                }

                await using var imageStream = await response.Content.ReadAsStreamAsync();

                // Dosya adını URL'den türet
                var originalName = Path.GetFileNameWithoutExtension(uri.LocalPath);
                if (string.IsNullOrWhiteSpace(originalName) || originalName == "/")
                    originalName = "product";

                var ext2 = Path.GetExtension(uri.LocalPath).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext2) || !AllowedExtensions.Contains(ext2))
                    ext2 = ".jpg";

                var savedUrl = await _fileStorage.UploadAsync(imageStream, $"{originalName}{ext2}", contentType);
                _logger.LogInformation("✅ Görsel indirildi ve kaydedildi: {Url} → {SavedPath}", url, savedUrl);
                return savedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Görsel indirme hatası ({Url}): {Error}", url, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Yüklenen IFormFile görselini kaydeder.
        /// </summary>
        private async Task<string?> SaveUploadedImageAsync(IFormFile imageFile)
        {
            try
            {
                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext))
                {
                    _logger.LogWarning("⚠️ Desteklenmeyen görsel uzantısı: {Ext}", ext);
                    return null;
                }

                if (imageFile.Length > MaxImportImageSize)
                {
                    _logger.LogWarning("⚠️ Görsel çok büyük: {Name} ({Size}MB)",
                        imageFile.FileName, imageFile.Length / 1024 / 1024);
                    return null;
                }

                // İçerik tipi: form gönderiminden gelen veya uzantıdan tahmin edilen
                var contentType = imageFile.ContentType ?? "image/jpeg";

                await using var stream = imageFile.OpenReadStream();
                var savedUrl = await _fileStorage.UploadAsync(stream, imageFile.FileName, contentType);
                _logger.LogInformation("✅ Görsel kaydedildi: {Name} → {Path}", imageFile.FileName, savedUrl);
                return savedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Görsel kaydetme hatası ({Name}): {Error}", imageFile.FileName, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// SSRF koruması: URI'nin özel/yerel ağ adresine işaret edip etmediğini kontrol eder.
        /// </summary>
        private static bool IsPrivateOrLocalUri(Uri uri)
        {
            var host = uri.Host;
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!System.Net.IPAddress.TryParse(host, out var ip))
                return false; // DNS adı — genel kabul

            var bytes = ip.GetAddressBytes();
            // IPv4 kontrolü
            if (bytes.Length == 4)
            {
                return bytes[0] == 10                                   // 10.x.x.x
                    || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)  // 172.16-31.x.x
                    || (bytes[0] == 192 && bytes[1] == 168)              // 192.168.x.x
                    || bytes[0] == 127                                   // 127.x.x.x loopback
                    || bytes[0] == 169 && bytes[1] == 254;               // 169.254.x.x link-local
            }
            // IPv6 loopback (::1)
            if (bytes.Length == 16)
            {
                return ip.Equals(System.Net.IPAddress.IPv6Loopback)
                    || ip.IsIPv6LinkLocal
                    || ip.IsIPv6SiteLocal;
            }
            return false;
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
            helpSheet.Cell(9, 3).Value = "Görsel yolu. 3 seçenek: 1) HTTP/HTTPS URL (otomatik indirilir), 2) Yüklenen görsel dosyasının adı (örn: urun.jpg), 3) /uploads/products/... yolu";
            helpSheet.Cell(9, 4).Value = "https://site.com/urun.jpg  VEYA  urun.jpg  VEYA  /uploads/products/urun.jpg";

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
            helpSheet.Cell(19, 1).Value = "• GÖRSEL SEÇENEKLERİ:";
            helpSheet.Cell(19, 1).Style.Font.Bold = true;
            helpSheet.Cell(20, 1).Value = "  1) HTTP/HTTPS URL yazın → görsel otomatik indirilir (örn: https://example.com/resim.jpg)";
            helpSheet.Cell(21, 1).Value = "  2) Görsel dosya adını yazın (örn: kasar.jpg) → aynı anda görsel dosyalarını seçerek yükleyin";
            helpSheet.Cell(22, 1).Value = "  3) /uploads/products/... yolu yazın → mevcut sistemdeki yolu kullanır";
            helpSheet.Cell(23, 1).Value = "• Desteklenen görsel formatları: JPG, JPEG, PNG, GIF, WEBP, BMP, TIFF, AVIF, SVG";

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

                // Mikro ERP bağlıysa: sadece web aktif ürünleri (WebeGonderilecekFl=true) al
                IEnumerable<object> exportRows;
                if (_mikroDbService.IsConfigured)
                {
                    var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
                    var localAll = (await _productRepository.GetAllAsync()).ToList();
                    var skuToLocal = localAll
                        .Where(p => !string.IsNullOrEmpty(p.SKU))
                        .GroupBy(p => p.SKU!)
                        .ToDictionary(g => g.Key, g => g.First());

                    exportRows = unified
                        .Where(p => p.WebeGonderilecekFl)  // sadece web aktif ürünler
                        .OrderBy(p => p.StokKod)
                        .Select(p =>
                        {
                            var hasLocal = skuToLocal.TryGetValue(p.StokKod, out var local);
                            return (object)new
                            {
                                Id    = hasLocal ? local!.Id : 0,
                                Name  = hasLocal && !string.IsNullOrEmpty(local!.Name) ? local.Name : p.StokAd,
                                Description = hasLocal && !string.IsNullOrEmpty(local!.Description) ? local.Description : "",
                                // Fiyat: Mikro ERP esas kaynak — yerel DB sadece Mikro 0 ise fallback
                                Price = p.Fiyat > 0 ? p.Fiyat : (hasLocal ? local!.Price : 0m),
                                SpecialPrice = hasLocal ? local!.SpecialPrice : (decimal?)null,
                                StockQuantity = (int)Math.Max(0, p.StokMiktar),
                                CategoryId    = hasLocal && local!.CategoryId > 0 ? (int?)local.CategoryId : (int?)null,
                                CategoryName  = (string?)null,
                                ImageUrl      = hasLocal && !string.IsNullOrEmpty(local!.ImageUrl) ? local.ImageUrl : "",
                                Sku           = p.StokKod,
                                IsActive      = p.WebeGonderilecekFl
                            };
                        })
                        .ToList();
                }
                else
                {
                    var dbProducts = await _productService.GetActiveProductsAsync(1, 50000);
                    exportRows = dbProducts.Select(p => (object)new
                    {
                        p.Id, p.Name, p.Description, p.Price, p.SpecialPrice,
                        p.StockQuantity, p.CategoryId, p.CategoryName, p.ImageUrl,
                        Sku = "", IsActive = true
                    }).ToList();
                }

                var productList = exportRows.ToList();
                
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

                // Ürünleri yaz — dynamic ile exportRows'dan alan oku
                int row = 2;
                foreach (dynamic product in productList)
                {
                    worksheet.Cell(row, 1).Value  = (int)(product.Id ?? 0);
                    worksheet.Cell(row, 2).Value  = (string)(product.Name ?? "");
                    worksheet.Cell(row, 3).Value  = (string)(product.Description ?? "");
                    worksheet.Cell(row, 4).Value  = (decimal)(product.Price ?? 0m);
                    worksheet.Cell(row, 5).Value  = (decimal)(product.SpecialPrice ?? 0m);
                    worksheet.Cell(row, 6).Value  = (int)(product.StockQuantity ?? 0);
                    worksheet.Cell(row, 7).Value  = (int)(product.CategoryId ?? 0);
                    worksheet.Cell(row, 8).Value  = (string)(product.CategoryName ?? "");
                    worksheet.Cell(row, 9).Value  = (string)(product.ImageUrl ?? "");
                    worksheet.Cell(row, 10).Value = (string)(product.Sku ?? "");
                    worksheet.Cell(row, 11).Value = 0;
                    worksheet.Cell(row, 12).Value = (bool)(product.IsActive ?? true) ? "Evet" : "Hayır";
                    worksheet.Cell(row, 13).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
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
