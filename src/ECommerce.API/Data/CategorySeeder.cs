using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ECommerce.API.Data
{
    /// <summary>
    /// Uygulama başlangıcında kritik kategori verilerinin varlığını garanti eder.
    /// 
    /// NEDEN: "Diğer" kategorisi ve wildcard mapping olmadan,
    /// Mikro'dan gelen eşlenememiş ürünler FK violation'a neden olur.
    /// Bu seeder her startup'ta idempotent çalışarak veri bütünlüğünü sağlar.
    /// </summary>
    public class CategorySeeder
    {
        // Sabit slug — kod genelinde bu slug ile "Diğer" kategorisi aranır
        public const string UncategorizedSlug = "diger";
        public const string UncategorizedName = "Diğer";
        private const string FrozenCategorySlug = "dondurma-ve-dondurulmus-gida";

        private static readonly string[] FrozenCategoryAnagrupKodlari =
        {
            "DONDURMA",
            "DONMUS URUN",
            "DONMUŞ ÜRÜN",
            "DONDURULMUS GIDA",
            "DONDURULMUŞ GIDA",
            "1300"
        };

        private static readonly string[] FrozenCategoryNameHints =
        {
            "ALGIDA",
            "MAGNUM",
            "CORNETTO",
            "CALIPPO",
            "CLASSICS",
            "CARTE D OR",
            "CARTE D'OR",
            "BOOM BOOM",
            "SUPERFRESH",
            "SUPER FRESH",
            "DONDURMA",
            "DONMUS",
            "DONMUŞ",
            "DONDURULMUS",
            "DONDURULMUŞ"
        };

        private static readonly (string Slug, string Name, string Description, int SortOrder)[] StorefrontCategories =
        {
            ("et-ve-et-urunleri", "Et ve Et Ürünleri", "Taze et, tavuk, balık, şarküteri ve deniz ürünleri", 1),
            ("sut-ve-sut-urunleri", "Süt Ürünleri", "Süt, peynir, yoğurt, tereyağı ve kahvaltılık ürünler", 2),
            ("meyve-ve-sebze", "Meyve ve Sebze", "Taze meyve, sebze ve yeşillikler", 3),
            ("icecekler", "İçecekler", "Su, meyve suyu, gazlı içecekler, çay ve kahve", 4),
            ("atistirmalik", "Atıştırmalık", "Cips, çikolata, kuruyemiş, bisküvi ve gofret çeşitleri", 5),
            ("dondurma-ve-dondurulmus-gida", "Dondurma & Dondurulmuş Gıda", "Dondurma, donuk ürünler ve SuperFresh benzeri dondurulmuş gıdalar", 6),
            ("temizlik", "Temizlik", "Ev temizlik, kişisel bakım ve bebek ürünleri", 7),
            ("temel-gida", "Temel Gıda", "Un, şeker, bakliyat, yağ, baharat ve temel mutfak ürünleri", 8),
            ("ev-ve-mutfak", "Ev & Mutfak Gereçleri", "Tencere, bardak, mutfak gereçleri ve ev ihtiyaçları", 9),
        };

        public static IReadOnlyCollection<string> PublicStorefrontCategorySlugs { get; } =
            Array.AsReadOnly(StorefrontCategories.Select(item => item.Slug).ToArray());

        private static readonly (string MikroAnagrupKod, string CategorySlug, int Priority, string Description)[] CanonicalCategoryMappings =
        {
            ("ET URUNLERI", "et-ve-et-urunleri", 1, "Ana et grubu"),
            ("ET ÜRÜNLERİ", "et-ve-et-urunleri", 1, "Ana et grubu"),
            ("ET VE ET URUNLERI", "et-ve-et-urunleri", 2, "Et üst kategorisi"),
            ("ET VE ET ÜRÜNLERİ", "et-ve-et-urunleri", 2, "Et üst kategorisi"),
            ("SARKUTERI", "et-ve-et-urunleri", 3, "Şarküteri ana grubu"),
            ("ŞARKÜTERİ", "et-ve-et-urunleri", 3, "Şarküteri ana grubu"),
            ("TAVUK", "et-ve-et-urunleri", 4, "Tavuk ana grubu"),
            ("PILIC", "et-ve-et-urunleri", 4, "Piliç ana grubu"),
            ("PİLİÇ", "et-ve-et-urunleri", 4, "Piliç ana grubu"),
            ("BALIK", "et-ve-et-urunleri", 5, "Balık ana grubu"),
            ("400", "et-ve-et-urunleri", 10, "Legacy numeric et/piliç kodu"),

            ("SUT URUNLERI", "sut-ve-sut-urunleri", 1, "Süt ürünleri ana grubu"),
            ("SÜT ÜRÜNLERİ", "sut-ve-sut-urunleri", 1, "Süt ürünleri ana grubu"),
            ("SUT VE YOGURT URUNLERI", "sut-ve-sut-urunleri", 2, "Süt ve yoğurt ana grubu"),
            ("SÜT VE YOĞURT ÜRÜNLERİ", "sut-ve-sut-urunleri", 2, "Süt ve yoğurt ana grubu"),
            ("PEYNIR", "sut-ve-sut-urunleri", 3, "Peynir ana grubu"),
            ("PEYNİR", "sut-ve-sut-urunleri", 3, "Peynir ana grubu"),
            ("KAHVALTILIK", "sut-ve-sut-urunleri", 4, "Kahvaltılık ana grubu"),
            ("500", "sut-ve-sut-urunleri", 10, "Legacy numeric süt kodu"),
            // NEDEN: Legacy 800 kodu sahada Algida/dondurma ürünleri için kullanılıyor.
            // Süt ürünlerine map edildiğinde storefront'ta yanlış raf oluşuyor.
            ("800", "dondurma-ve-dondurulmus-gida", 11, "Legacy numeric dondurma kodu"),

            ("MEYVE VE SEBZE", "meyve-ve-sebze", 1, "Meyve ve sebze ana grubu"),
            ("MEYVE-SEBZE", "meyve-ve-sebze", 1, "Meyve ve sebze ana grubu"),
            ("MEYVE", "meyve-ve-sebze", 2, "Meyve ana grubu"),
            ("SEBZE", "meyve-ve-sebze", 2, "Sebze ana grubu"),
            ("2200", "meyve-ve-sebze", 10, "Legacy numeric meyve-sebze kodu"),

            ("ICECEKLER", "icecekler", 1, "İçecekler ana grubu"),
            ("İÇECEKLER", "icecekler", 1, "İçecekler ana grubu"),
            ("ICECEK", "icecekler", 2, "İçecek ana grubu"),
            ("İÇECEK", "icecekler", 2, "İçecek ana grubu"),
            ("100", "icecekler", 10, "Legacy numeric içecek kodu"),

            ("ATISTIRMALIK", "atistirmalik", 1, "Atıştırmalık ana grubu"),
            ("ATIŞTIRMALIK", "atistirmalik", 1, "Atıştırmalık ana grubu"),
            ("KURUYEMIS", "atistirmalik", 2, "Kuruyemiş ana grubu"),
            ("KURUYEMİŞ", "atistirmalik", 2, "Kuruyemiş ana grubu"),
            ("DONDURMA", "dondurma-ve-dondurulmus-gida", 3, "Dondurma ana grubu"),
            ("1400", "atistirmalik", 10, "Legacy numeric atıştırmalık kodu"),
            ("12", "atistirmalik", 11, "Legacy numeric atıştırmalık kodu"),

            ("TEMIZLIK", "temizlik", 1, "Temizlik ana grubu"),
            ("TEMİZLİK", "temizlik", 1, "Temizlik ana grubu"),
            ("TEMIZLIK URUNLERI", "temizlik", 2, "Temizlik ürünleri ana grubu"),
            ("TEMİZLİK ÜRÜNLERİ", "temizlik", 2, "Temizlik ürünleri ana grubu"),
            ("KISISEL BAKIM", "temizlik", 3, "Kişisel bakım ana grubu"),
            ("KİŞİSEL BAKIM", "temizlik", 3, "Kişisel bakım ana grubu"),
            ("900", "temizlik", 10, "Legacy numeric kişisel bakım kodu"),
            ("1000", "temizlik", 11, "Legacy numeric temizlik kodu"),
            ("1500", "temizlik", 12, "Legacy numeric kişisel bakım kodu"),
            ("1600", "temizlik", 13, "Legacy numeric kişisel bakım kodu"),

            ("TEMEL GIDA", "temel-gida", 1, "Temel gıda ana grubu"),
            ("BAKLIYAT", "temel-gida", 2, "Bakliyat ana grubu"),
            ("EKMEK VE FIRINCI URUNLER", "temel-gida", 3, "Fırın ürünleri ana grubu"),
            ("EKMEK VE FIRINCI ÜRÜNLER", "temel-gida", 3, "Fırın ürünleri ana grubu"),
            ("HAZIR YEMEK", "temel-gida", 4, "Hazır yemek ana grubu"),
            ("DONMUS URUN", "dondurma-ve-dondurulmus-gida", 5, "Donmuş ürün ana grubu"),
            ("DONMUŞ ÜRÜN", "dondurma-ve-dondurulmus-gida", 5, "Donmuş ürün ana grubu"),
            ("DONDURULMUS GIDA", "dondurma-ve-dondurulmus-gida", 6, "Dondurulmuş gıda ana grubu"),
            ("DONDURULMUŞ GIDA", "dondurma-ve-dondurulmus-gida", 6, "Dondurulmuş gıda ana grubu"),
            ("600", "temel-gida", 10, "Legacy numeric ekmek/fırın kodu"),
            ("700", "temel-gida", 11, "Legacy numeric temel gıda kodu"),
            ("1200", "temel-gida", 12, "Legacy numeric hazır yemek kodu"),
            ("1300", "dondurma-ve-dondurulmus-gida", 13, "Legacy numeric donmuş ürün kodu"),

            ("EV VE MUTFAK", "ev-ve-mutfak", 1, "Ev ve mutfak ana grubu"),
            ("EV & MUTFAK", "ev-ve-mutfak", 1, "Ev ve mutfak ana grubu"),
            ("MUTFAK GERECLERI", "ev-ve-mutfak", 2, "Mutfak gereçleri ana grubu"),
            ("MUTFAK GEREÇLERİ", "ev-ve-mutfak", 2, "Mutfak gereçleri ana grubu"),
            ("EV GERECLERI", "ev-ve-mutfak", 3, "Ev gereçleri ana grubu"),
            ("EV GEREÇLERİ", "ev-ve-mutfak", 3, "Ev gereçleri ana grubu"),
            ("ELEKTRIKLI EV ALETLERI", "ev-ve-mutfak", 4, "Elektrikli ev aletleri ana grubu"),
            ("ELEKTRİKLİ EV ALETLERİ", "ev-ve-mutfak", 4, "Elektrikli ev aletleri ana grubu"),
            ("PIL VE ELEKTRONIK", "ev-ve-mutfak", 5, "Pil ve elektronik ana grubu"),
            ("PİL VE ELEKTRONİK", "ev-ve-mutfak", 5, "Pil ve elektronik ana grubu"),
            ("1900", "ev-ve-mutfak", 10, "Legacy numeric elektrikli ev aletleri kodu"),
            ("2000", "ev-ve-mutfak", 11, "Legacy numeric pil/elektronik kodu"),
        };

        private static readonly (string MikroAnagrupKod, string MikroAltgrupKod, string CategorySlug, int Priority, string Description)[] CanonicalAltGroupMappings =
        {
            ("500", "506", "temel-gida", 100, "Reçel, pekmez ve benzeri kahvaltılık şekerli ürünler"),
        };

        private static readonly string[] MeatCategoryNameHints =
        {
            "SUCUK", "SALAM", "SOSIS", "SOSİS", "PASTIRMA", "PASTİRMA", "KAVURMA",
            "JAMBON", "FUME ET", "FÜME ET", "DANA", "KUZU", "KOFTE", "KÖFTE",
            "KIYMA", "KIYMA", "ANTRIKOT", "BONFILE", "BIFTEK", "TAVUK", "HINDI", "HİNDİ"
        };

        private static readonly string[] MilkCategoryNameHints =
        {
            " SUT", " SÜT", "YOGURT", "YOĞURT", "AYRAN", "KEFIR", "KEFİR", "PEYNIR", "PEYNİR",
            "KASAR", "KAŞAR", "LABNE", "TEREYAG", "TEREYAĞ", "KREMA", "KAYMAK",
            "HINDISTAN CEVIZI SUTU", "HİNDİSTAN CEVİZİ SÜTÜ", "BITKISEL SUT", "BİTKİSEL SÜT"
        };

        public static async Task SeedAsync(ECommerceDbContext context)
        {
            try
            {
                Console.WriteLine("[CategorySeeder] 🔍 Kategoriler kontrol ediliyor...");

                // 0. Ana mağaza kategorilerini garanti et — seed mapping'ler bu slug'lara bağlanır
                var storefrontCategories = await EnsureStorefrontCategoriesAsync(context);

                // 1. "Diğer" kategorisini garanti et — eşlenemeyen ürünlerin güvenli limanı
                var digerCategory = await EnsureUncategorizedCategoryAsync(context);

                // 2. Kanonik MikroCategoryMapping seed verisini garanti et
                await EnsureCanonicalMappingsAsync(context, storefrontCategories);

                // 2.1 Alt grup override mapping'leri garanti et.
                await EnsureCanonicalAltGroupMappingsAsync(context, storefrontCategories);

                // 2.2 Yeni donmuş kategoriye ait mevcut ürünleri güvenli biçimde taşı.
                await ReassignFrozenCategoryProductsAsync(context, storefrontCategories);

                // 2.3 Et ürünleri yanlış raflara düşmüşse isim ve ERP ipuçlarıyla düzelt.
                await ReassignMeatCategoryProductsAsync(context, storefrontCategories);

                // 2.4 Süt geçen ürünleri içecek/diğer raflarından süt ürünlerine çek.
                await ReassignMilkCategoryProductsAsync(context, storefrontCategories);

                // 3. Wildcard (*) mapping'i garanti et — tüm eşlenemeyen ürünler "Diğer"e düşer
                await EnsureWildcardMappingAsync(context, digerCategory.Id);

                // 4. IsActive null/false olanları düzelt (legacy uyumluluk)
                await FixInactiveCategoriesAsync(context);

                // 5. Eski auto-create akışından kalmış sayısal kategorileri temizle.
                // NEDEN: Mikro kodları numerik olabilir, fakat storefront kategori kümesi sabit ve yazılı isimlerle yönetilir.
                await CollapseNumericCategoriesAsync(context, digerCategory.Id);

                // ADIM 13: Startup safety — geçersiz CategoryId'li ürünleri "Diğer"e taşı
                // NEDEN: Uygulama yeniden başladığında, arada silinmiş kategorilere 
                // referans veren ürünler FK violation'a yol açabilir.
                await FixOrphanProductsAsync(context, digerCategory.Id);

                // ADIM 13: ParentId bütünlüğü — silinmiş parent'a referans veren kategorileri düzelt
                await FixOrphanSubcategoriesAsync(context);

                Console.WriteLine("[CategorySeeder] ✅ Kategori seed + validasyon tamamlandı.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CategorySeeder] ❌ Hata: {ex.Message}");
                throw;
            }
        }

        private static async Task<Dictionary<string, Category>> EnsureStorefrontCategoriesAsync(ECommerceDbContext context)
        {
            var existingCategories = await context.Categories.ToListAsync();
            var categoryBySlug = existingCategories.ToDictionary(c => c.Slug, StringComparer.OrdinalIgnoreCase);
            var now = DateTime.UtcNow;
            var createdCount = 0;
            var updatedCount = 0;

            foreach (var (slug, name, description, sortOrder) in StorefrontCategories)
            {
                if (!categoryBySlug.TryGetValue(slug, out var category))
                {
                    category = new Category
                    {
                        Name = name,
                        Description = description,
                        Slug = slug,
                        SortOrder = sortOrder,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                    };

                    context.Categories.Add(category);
                    categoryBySlug[slug] = category;
                    createdCount++;
                    continue;
                }

                var changed = false;

                if (!category.IsActive)
                {
                    category.IsActive = true;
                    changed = true;
                }

                if (category.SortOrder != sortOrder)
                {
                    category.SortOrder = sortOrder;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(category.Name))
                {
                    category.Name = name;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(category.Description))
                {
                    category.Description = description;
                    changed = true;
                }

                if (changed)
                {
                    category.UpdatedAt = now;
                    updatedCount++;
                }
            }

            if (createdCount > 0 || updatedCount > 0)
            {
                await context.SaveChangesAsync();
            }

            if (createdCount > 0)
            {
                Console.WriteLine($"[CategorySeeder] ✅ {createdCount} ana kategori oluşturuldu");
            }

            if (updatedCount > 0)
            {
                Console.WriteLine($"[CategorySeeder] 🔄 {updatedCount} ana kategori normalize edildi");
            }

            return await context.Categories
                .Where(c => StorefrontCategories.Select(item => item.Slug).Contains(c.Slug))
                .ToDictionaryAsync(c => c.Slug, StringComparer.OrdinalIgnoreCase);
        }

        private static async Task EnsureCanonicalMappingsAsync(
            ECommerceDbContext context,
            IReadOnlyDictionary<string, Category> storefrontCategories)
        {
            var now = DateTime.UtcNow;
            var baseMappings = await context.Set<MikroCategoryMapping>()
                .Where(m => m.MikroAltgrupKod == null && m.MikroMarkaKod == null && m.MikroAnagrupKod != "*")
                .ToListAsync();

            var mappingsByCode = baseMappings
                .GroupBy(m => NormalizeSeedKey(m.MikroAnagrupKod), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderBy(m => m.Id).ToList(), StringComparer.OrdinalIgnoreCase);

            var createdCount = 0;
            var updatedCount = 0;
            var deactivatedCount = 0;

            foreach (var seed in CanonicalCategoryMappings)
            {
                if (!storefrontCategories.TryGetValue(seed.CategorySlug, out var category))
                {
                    throw new InvalidOperationException($"Seed category slug bulunamadı: {seed.CategorySlug}");
                }

                var key = NormalizeSeedKey(seed.MikroAnagrupKod);
                mappingsByCode.TryGetValue(key, out var existingMappings);
                existingMappings ??= new List<MikroCategoryMapping>();

                var keeper = existingMappings.FirstOrDefault();
                if (keeper == null)
                {
                    var mapping = new MikroCategoryMapping
                    {
                        MikroAnagrupKod = seed.MikroAnagrupKod,
                        MikroAltgrupKod = null,
                        MikroMarkaKod = null,
                        CategoryId = category.Id,
                        Priority = seed.Priority,
                        IsActive = true,
                        MikroGrupAciklama = seed.Description,
                        Notes = "CategorySeeder canonical seed",
                        CreatedAt = now,
                        UpdatedAt = now,
                    };

                    context.Set<MikroCategoryMapping>().Add(mapping);
                    mappingsByCode[key] = new List<MikroCategoryMapping> { mapping };
                    createdCount++;
                    continue;
                }

                var changed = false;

                if (!string.Equals(keeper.MikroAnagrupKod, seed.MikroAnagrupKod, StringComparison.Ordinal))
                {
                    keeper.MikroAnagrupKod = seed.MikroAnagrupKod;
                    changed = true;
                }

                if (keeper.CategoryId != category.Id)
                {
                    keeper.CategoryId = category.Id;
                    changed = true;
                }

                if (keeper.Priority != seed.Priority)
                {
                    keeper.Priority = seed.Priority;
                    changed = true;
                }

                if (!keeper.IsActive)
                {
                    keeper.IsActive = true;
                    changed = true;
                }

                if (!string.Equals(keeper.MikroGrupAciklama, seed.Description, StringComparison.Ordinal))
                {
                    keeper.MikroGrupAciklama = seed.Description;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(keeper.Notes))
                {
                    keeper.Notes = "CategorySeeder canonical seed";
                    changed = true;
                }

                if (changed)
                {
                    keeper.UpdatedAt = now;
                    updatedCount++;
                }

                foreach (var duplicate in existingMappings.Skip(1))
                {
                    if (duplicate.IsActive)
                    {
                        duplicate.IsActive = false;
                        duplicate.UpdatedAt = now;
                        if (string.IsNullOrWhiteSpace(duplicate.Notes))
                        {
                            duplicate.Notes = "CategorySeeder canonical seed duplicate disabled";
                        }
                        deactivatedCount++;
                    }
                }
            }

            if (createdCount > 0 || updatedCount > 0 || deactivatedCount > 0)
            {
                await context.SaveChangesAsync();
            }

            Console.WriteLine(
                $"[CategorySeeder] ✅ Kanonik mapping seed tamamlandı. Yeni={createdCount}, Güncel={updatedCount}, Pasif={deactivatedCount}");
        }

        private static async Task EnsureCanonicalAltGroupMappingsAsync(
            ECommerceDbContext context,
            IReadOnlyDictionary<string, Category> storefrontCategories)
        {
            var now = DateTime.UtcNow;
            var existingMappings = await context.Set<MikroCategoryMapping>()
                .Where(m => m.MikroAltgrupKod != null && m.MikroMarkaKod == null)
                .ToListAsync();

            var mappingsByKey = existingMappings
                .GroupBy(m => $"{NormalizeSeedKey(m.MikroAnagrupKod)}|{NormalizeSeedKey(m.MikroAltgrupKod!)}", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderBy(m => m.Id).ToList(), StringComparer.OrdinalIgnoreCase);

            var createdCount = 0;
            var updatedCount = 0;
            var deactivatedCount = 0;

            foreach (var seed in CanonicalAltGroupMappings)
            {
                if (!storefrontCategories.TryGetValue(seed.CategorySlug, out var category))
                {
                    throw new InvalidOperationException($"Seed category slug bulunamadı: {seed.CategorySlug}");
                }

                var key = $"{NormalizeSeedKey(seed.MikroAnagrupKod)}|{NormalizeSeedKey(seed.MikroAltgrupKod)}";
                mappingsByKey.TryGetValue(key, out var candidates);
                candidates ??= new List<MikroCategoryMapping>();

                var keeper = candidates.FirstOrDefault();
                if (keeper == null)
                {
                    context.Set<MikroCategoryMapping>().Add(new MikroCategoryMapping
                    {
                        MikroAnagrupKod = seed.MikroAnagrupKod,
                        MikroAltgrupKod = seed.MikroAltgrupKod,
                        MikroMarkaKod = null,
                        CategoryId = category.Id,
                        Priority = seed.Priority,
                        IsActive = true,
                        MikroGrupAciklama = seed.Description,
                        Notes = "CategorySeeder canonical alt-group seed",
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                    createdCount++;
                    continue;
                }

                var changed = false;

                if (keeper.CategoryId != category.Id)
                {
                    keeper.CategoryId = category.Id;
                    changed = true;
                }

                if (keeper.Priority != seed.Priority)
                {
                    keeper.Priority = seed.Priority;
                    changed = true;
                }

                if (!keeper.IsActive)
                {
                    keeper.IsActive = true;
                    changed = true;
                }

                if (!string.Equals(keeper.MikroGrupAciklama, seed.Description, StringComparison.Ordinal))
                {
                    keeper.MikroGrupAciklama = seed.Description;
                    changed = true;
                }

                if (changed)
                {
                    keeper.UpdatedAt = now;
                    updatedCount++;
                }

                foreach (var duplicate in candidates.Skip(1))
                {
                    if (duplicate.IsActive)
                    {
                        duplicate.IsActive = false;
                        duplicate.UpdatedAt = now;
                        deactivatedCount++;
                    }
                }
            }

            if (createdCount > 0 || updatedCount > 0 || deactivatedCount > 0)
            {
                await context.SaveChangesAsync();
            }

            Console.WriteLine(
                $"[CategorySeeder] ✅ Alt grup mapping seed tamamlandı. Yeni={createdCount}, Güncel={updatedCount}, Pasif={deactivatedCount}");
        }

        private static async Task ReassignFrozenCategoryProductsAsync(
            ECommerceDbContext context,
            IReadOnlyDictionary<string, Category> storefrontCategories)
        {
            if (!storefrontCategories.TryGetValue(FrozenCategorySlug, out var frozenCategory))
                return;

            var broadCategoryIds = storefrontCategories
                .Where(entry =>
                    entry.Key == "atistirmalik" ||
                    entry.Key == "temel-gida" ||
                    entry.Key == "sut-ve-sut-urunleri")
                .Select(entry => entry.Value.Id)
                .ToHashSet();

            var cacheItems = await context.MikroProductCaches
                .AsNoTracking()
                .Where(item => item.LocalProductId.HasValue)
                .Select(item => new { ProductId = item.LocalProductId!.Value, item.AnagrupKod, item.StokAd })
                .ToListAsync();

            var frozenProductIds = cacheItems
                .Where(item => IsFrozenCategoryMatch(item.AnagrupKod, item.StokAd))
                .Select(item => item.ProductId)
                .ToHashSet();

            var products = await context.Products
                .Where(product =>
                    frozenProductIds.Contains(product.Id) ||
                    (broadCategoryIds.Contains(product.CategoryId) && product.Name != null))
                .ToListAsync();

            var movedCount = 0;
            var now = DateTime.UtcNow;

            foreach (var product in products)
            {
                var shouldMove = frozenProductIds.Contains(product.Id) ||
                                 (broadCategoryIds.Contains(product.CategoryId) && IsFrozenCategoryMatch(null, product.Name));

                if (!shouldMove || product.CategoryId == frozenCategory.Id)
                    continue;

                product.CategoryId = frozenCategory.Id;
                product.UpdatedAt = now;
                movedCount++;
            }

            if (movedCount > 0)
            {
                await context.SaveChangesAsync();
                Console.WriteLine($"[CategorySeeder] 🔄 {movedCount} ürün '{frozenCategory.Name}' kategorisine taşındı");
            }
        }

        private static async Task ReassignMeatCategoryProductsAsync(
            ECommerceDbContext context,
            IReadOnlyDictionary<string, Category> storefrontCategories)
        {
            var meatCategory = await context.Categories
                .FirstOrDefaultAsync(category => category.Slug == "et-ve-et-urunleri");
            if (meatCategory == null)
                return;

            var broadCategoryIds = await context.Categories
                .Where(category =>
                    category.Slug == "sut-ve-sut-urunleri" ||
                    category.Slug == "temel-gida" ||
                    category.Slug == "atistirmalik" ||
                    category.Slug == UncategorizedSlug)
                .Select(category => category.Id)
                .ToHashSetAsync();

            var cacheItems = await context.MikroProductCaches
                .AsNoTracking()
                .Where(item => item.LocalProductId.HasValue)
                .Select(item => new { ProductId = item.LocalProductId!.Value, item.AnagrupKod, item.StokAd })
                .ToListAsync();

            var meatProductIds = cacheItems
                .Where(item => IsMeatCategoryMatch(item.AnagrupKod, item.StokAd))
                .Select(item => item.ProductId)
                .ToHashSet();

            var products = await context.Products
                .Where(product => product.Name != null)
                .ToListAsync();

            var movedCount = 0;
            var now = DateTime.UtcNow;

            foreach (var product in products)
            {
                var shouldMove = meatProductIds.Contains(product.Id) ||
                                 (broadCategoryIds.Contains(product.CategoryId) && IsMeatCategoryMatch(null, product.Name));

                if (!shouldMove || product.CategoryId == meatCategory.Id)
                    continue;

                product.CategoryId = meatCategory.Id;
                product.UpdatedAt = now;
                movedCount++;
            }

            if (movedCount > 0)
            {
                await context.SaveChangesAsync();
                Console.WriteLine($"[CategorySeeder] 🔄 {movedCount} ürün '{meatCategory.Name}' kategorisine taşındı");
            }
        }

        private static async Task ReassignMilkCategoryProductsAsync(
            ECommerceDbContext context,
            IReadOnlyDictionary<string, Category> storefrontCategories)
        {
            var milkCategory = await context.Categories
                .FirstOrDefaultAsync(category => category.Slug == "sut-ve-sut-urunleri");
            if (milkCategory == null)
                return;

            var broadCategoryIds = await context.Categories
                .Where(category =>
                    category.Slug == "icecekler" ||
                    category.Slug == "temel-gida" ||
                    category.Slug == "atistirmalik" ||
                    category.Slug == UncategorizedSlug)
                .Select(category => category.Id)
                .ToHashSetAsync();

            var cacheItems = await context.MikroProductCaches
                .AsNoTracking()
                .Where(item => item.LocalProductId.HasValue)
                .Select(item => new { ProductId = item.LocalProductId!.Value, item.AnagrupKod, item.StokAd })
                .ToListAsync();

            var milkProductIds = cacheItems
                .Where(item => IsMilkCategoryMatch(item.AnagrupKod, item.StokAd))
                .Select(item => item.ProductId)
                .ToHashSet();

            var products = await context.Products
                .Where(product => product.Name != null)
                .ToListAsync();

            var movedCount = 0;
            var now = DateTime.UtcNow;

            foreach (var product in products)
            {
                if (IsMeatCategoryMatch(null, product.Name))
                    continue;

                var shouldMove = milkProductIds.Contains(product.Id) ||
                                 (broadCategoryIds.Contains(product.CategoryId) && IsMilkCategoryMatch(null, product.Name));

                if (!shouldMove || product.CategoryId == milkCategory.Id)
                    continue;

                product.CategoryId = milkCategory.Id;
                product.UpdatedAt = now;
                movedCount++;
            }

            if (movedCount > 0)
            {
                await context.SaveChangesAsync();
                Console.WriteLine($"[CategorySeeder] 🔄 {movedCount} ürün '{milkCategory.Name}' kategorisine taşındı");
            }
        }

        private static string NormalizeSeedKey(string mikroAnagrupKod)
        {
            return mikroAnagrupKod.Trim().ToUpperInvariant();
        }

        private static string NormalizeHintText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = Regex.Replace(value.ToUpperInvariant(), @"[^\p{L}\p{Nd}]+", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            return $" {normalized} ";
        }

        private static bool ContainsHint(string? value, IEnumerable<string> hints)
        {
            var normalizedValue = NormalizeHintText(value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
                return false;

            return hints.Any(hint => normalizedValue.Contains(NormalizeHintText(hint), StringComparison.Ordinal));
        }

        private static bool IsFrozenCategoryMatch(string? anagrupKod, string? productName)
        {
            if (!string.IsNullOrWhiteSpace(anagrupKod) &&
                FrozenCategoryAnagrupKodlari.Contains(anagrupKod.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(productName))
                return false;

            return ContainsHint(productName, FrozenCategoryNameHints);
        }

        private static bool IsMeatCategoryMatch(string? anagrupKod, string? productName)
        {
            if (!string.IsNullOrWhiteSpace(anagrupKod))
            {
                var normalizedGroup = NormalizeSeedKey(anagrupKod);
                if (normalizedGroup.Contains("ET") ||
                    normalizedGroup.Contains("SARKUTERI") ||
                    normalizedGroup.Contains("ŞARKÜTERİ") ||
                    normalizedGroup.Contains("TAVUK") ||
                    normalizedGroup.Contains("PILIC") ||
                    normalizedGroup.Contains("PİLİÇ") ||
                    normalizedGroup.Contains("BALIK"))
                {
                    return true;
                }
            }

            if (string.IsNullOrWhiteSpace(productName))
                return false;

            return ContainsHint(productName, MeatCategoryNameHints);
        }

        private static bool IsMilkCategoryMatch(string? anagrupKod, string? productName)
        {
            if (!string.IsNullOrWhiteSpace(anagrupKod))
            {
                var normalizedGroup = NormalizeSeedKey(anagrupKod);
                if (normalizedGroup.Contains("SUT") ||
                    normalizedGroup.Contains("SÜT") ||
                    normalizedGroup.Contains("PEYNIR") ||
                    normalizedGroup.Contains("PEYNİR") ||
                    normalizedGroup.Contains("YOGURT") ||
                    normalizedGroup.Contains("YOĞURT") ||
                    normalizedGroup.Contains("KAHVALTILIK"))
                {
                    return true;
                }
            }

            if (string.IsNullOrWhiteSpace(productName))
                return false;

            return ContainsHint(productName, MilkCategoryNameHints);
        }

        /// <summary>
        /// "Diğer" kategorisinin varlığını garanti eder.
        /// NEDEN: Mikro'dan gelen hiçbir mapping'e uymayan ürünler bu kategoriye atanır.
        /// Böylece CategoryId FK violation asla oluşmaz.
        /// </summary>
        private static async Task<Category> EnsureUncategorizedCategoryAsync(ECommerceDbContext context)
        {
            var existing = await context.Categories
                .FirstOrDefaultAsync(c => c.Slug == UncategorizedSlug);

            if (existing != null)
            {
                Console.WriteLine($"[CategorySeeder] ℹ️ '{UncategorizedName}' kategorisi zaten mevcut (Id={existing.Id})");
                return existing;
            }

            var diger = new Category
            {
                Name = UncategorizedName,
                Description = "Henüz kategorize edilmemiş ürünler. Mikro'dan gelen ve eşlenememiş ürünler burada toplanır.",
                Slug = UncategorizedSlug,
                SortOrder = 999,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Categories.Add(diger);
            await context.SaveChangesAsync();

            Console.WriteLine($"[CategorySeeder] ✅ '{UncategorizedName}' kategorisi oluşturuldu (Id={diger.Id})");
            return diger;
        }

        /// <summary>
        /// Wildcard (*) MikroCategoryMapping kaydının varlığını garanti eder.
        /// NEDEN: CategoryMappingService.FindMappingAsync() son adımda "*" wildcard'ını arar.
        /// Bu kayıt yoksa eşlenememiş ürünler null mapping alır → hardcode CategoryId=1 fallback'i çalışır.
        /// Bu seed ile artık tüm eşlenememiş ürünler "Diğer"e yönlendirilir.
        /// </summary>
        private static async Task EnsureWildcardMappingAsync(ECommerceDbContext context, int digerCategoryId)
        {
            var existing = await context.Set<MikroCategoryMapping>()
                .FirstOrDefaultAsync(m => m.MikroAnagrupKod == "*");

            if (existing != null)
            {
                // CategoryId güncel mi kontrol et
                if (existing.CategoryId != digerCategoryId)
                {
                    existing.CategoryId = digerCategoryId;
                    existing.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    Console.WriteLine($"[CategorySeeder] 🔄 Wildcard mapping CategoryId güncellendi → {digerCategoryId}");
                }
                else
                {
                    Console.WriteLine("[CategorySeeder] ℹ️ Wildcard (*) mapping zaten mevcut");
                }
                return;
            }

            var wildcardMapping = new MikroCategoryMapping
            {
                MikroAnagrupKod = "*",
                MikroAltgrupKod = null,
                MikroMarkaKod = null,
                CategoryId = digerCategoryId,
                Priority = 999,
                IsActive = true,
                MikroGrupAciklama = "Varsayılan eşleme — hiçbir kurala uymayan ürünler 'Diğer' kategorisine atanır",
                Notes = "CategorySeeder tarafından otomatik oluşturuldu",
                CreatedAt = DateTime.UtcNow
            };

            context.Set<MikroCategoryMapping>().Add(wildcardMapping);
            await context.SaveChangesAsync();

            Console.WriteLine($"[CategorySeeder] ✅ Wildcard (*) mapping oluşturuldu → CategoryId={digerCategoryId}");
        }

        /// <summary>
        /// IsActive durumu hatalı kategorileri düzeltir (legacy uyumluluk).
        /// </summary>
        private static async Task FixInactiveCategoriesAsync(ECommerceDbContext context)
        {
            var categories = await context.Categories.ToListAsync();
            Console.WriteLine($"[CategorySeeder] 📊 Toplam {categories.Count} kategori bulundu");

            int updatedCount = 0;
            foreach (var category in categories)
            {
                if (!category.IsActive)
                {
                    category.IsActive = true;
                    updatedCount++;
                    Console.WriteLine($"  ✅ Kategori güncelleştirildi: {category.Name} -> IsActive = true");
                }
            }

            if (updatedCount > 0)
            {
                await context.SaveChangesAsync();
                Console.WriteLine($"[CategorySeeder] ✅ {updatedCount} kategori güncellendi");
            }
        }

        private static async Task CollapseNumericCategoriesAsync(ECommerceDbContext context, int digerCategoryId)
        {
            var protectedSlugs = new HashSet<string>(PublicStorefrontCategorySlugs, StringComparer.OrdinalIgnoreCase)
            {
                UncategorizedSlug
            };

            var candidateCategories = await context.Categories
                .Where(category =>
                    category.Id != digerCategoryId &&
                    !protectedSlugs.Contains(category.Slug))
                .ToListAsync();

            var numericCategories = candidateCategories
                .Where(category => IsDigitsOnly(category.Name) || IsDigitsOnly(category.Slug))
                .ToList();

            if (numericCategories.Count == 0)
                return;

            var numericCategoryIds = numericCategories.Select(category => category.Id).ToList();
            var now = DateTime.UtcNow;

            var productsToMove = await context.Products
                .Where(product => numericCategoryIds.Contains(product.CategoryId))
                .ToListAsync();

            foreach (var product in productsToMove)
            {
                product.CategoryId = digerCategoryId;
                product.UpdatedAt = now;
            }

            var childCategories = await context.Categories
                .Where(category => category.ParentId.HasValue && numericCategoryIds.Contains(category.ParentId.Value))
                .ToListAsync();

            foreach (var childCategory in childCategories)
            {
                childCategory.ParentId = null;
                childCategory.UpdatedAt = now;
            }

            var mappingsToDelete = await context.Set<MikroCategoryMapping>()
                .Where(mapping => numericCategoryIds.Contains(mapping.CategoryId))
                .ToListAsync();

            context.Set<MikroCategoryMapping>().RemoveRange(mappingsToDelete);
            context.Categories.RemoveRange(numericCategories);
            await context.SaveChangesAsync();

            Console.WriteLine(
                $"[CategorySeeder] ⚠ {numericCategories.Count} sayısal kategori silindi, {productsToMove.Count} ürün 'Diğer'e taşındı, {mappingsToDelete.Count} mapping kaldırıldı");
        }

        public static bool IsPublicStorefrontCategorySlug(string? slug)
        {
            return !string.IsNullOrWhiteSpace(slug) &&
                PublicStorefrontCategorySlugs.Contains(slug.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsDigitsOnly(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return value.Trim().All(char.IsDigit);
        }

        /// <summary>
        /// ADIM 13: Geçersiz CategoryId'li ürünleri "Diğer" kategorisine taşır.
        /// NEDEN: Silinmiş veya varolmayan kategorilere referans veren ürünler 
        /// runtime'da FK violation veya frontend'de boş kategori gösterimine neden olur.
        /// Bu kontrol her startup'ta çalışarak veri bütünlüğünü sağlar.
        /// </summary>
        private static async Task FixOrphanProductsAsync(ECommerceDbContext context, int digerCategoryId)
        {
            var validCategoryIds = await context.Categories
                .Select(c => c.Id)
                .ToListAsync();
            var validSet = new HashSet<int>(validCategoryIds);

            var orphanProducts = await context.Products
                .Where(p => !validSet.Contains(p.CategoryId))
                .ToListAsync();

            if (orphanProducts.Count == 0)
                return;

            foreach (var product in orphanProducts)
            {
                product.CategoryId = digerCategoryId;
                product.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[CategorySeeder] ⚠ ADIM 13: {orphanProducts.Count} yetim ürün 'Diğer'e taşındı");
        }

        /// <summary>
        /// ADIM 13: ParentId'si geçersiz olan alt kategorileri düzeltir.
        /// NEDEN: Parent kategori silinirse alt kategoriler orphan kalır.
        /// Bu kategorileri root seviyeye taşıyoruz (ParentId=null).
        /// </summary>
        private static async Task FixOrphanSubcategoriesAsync(ECommerceDbContext context)
        {
            var allCategoryIds = await context.Categories
                .Select(c => c.Id)
                .ToListAsync();
            var validSet = new HashSet<int>(allCategoryIds);

            var orphanSubcategories = await context.Categories
                .Where(c => c.ParentId.HasValue && !validSet.Contains(c.ParentId.Value))
                .ToListAsync();

            if (orphanSubcategories.Count == 0)
                return;

            foreach (var cat in orphanSubcategories)
            {
                cat.ParentId = null; // Root seviyeye taşı
                cat.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[CategorySeeder] ⚠ ADIM 13: {orphanSubcategories.Count} yetim alt kategori root'a taşındı");
        }
    }
}
