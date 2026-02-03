using ECommerce.Core.Interfaces.Mapping;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Mapping
{
    /// <summary>
    /// Mikro kategori kodları ile e-ticaret kategori ID'leri arasındaki eşlemeyi yönetir.
    /// 
    /// NEDEN: Mikro ERP'de kategoriler kod bazlı (sto_anagrup_kod, sto_altgrup_kod),
    /// e-ticaret'te ise ID bazlı. Bu servis ikisi arasında köprü kurar.
    /// 
    /// EŞLEŞTİRME ÖNCELİK SIRASI:
    /// 1. AltgrupKod + AnagrupKod (en spesifik)
    /// 2. Sadece AnagrupKod
    /// 3. Varsayılan kategori
    /// 
    /// CACHE: Sık kullanılan eşlemeler memory'de tutulur.
    /// </summary>
    public class CategoryMappingService : IMikroCategoryMappingService
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<CategoryMappingService> _logger;

        // Memory cache - performans için
        private readonly Dictionary<string, MikroCategoryMapping?> _cache = new();
        private readonly SemaphoreSlim _cacheLock = new(1, 1);
        private DateTime _cacheExpiry = DateTime.MinValue;
        private readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(10);

        public CategoryMappingService(
            DbContext dbContext,
            ILogger<CategoryMappingService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<MikroCategoryMapping?> GetMappingAsync(
            string anagrupKod,
            string? altgrupKod = null,
            string? markaKod = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(anagrupKod))
            {
                _logger.LogDebug("[CategoryMappingService] AnagrupKod boş, null döndürülüyor");
                return null;
            }

            // Cache key oluştur
            var cacheKey = BuildCacheKey(anagrupKod, altgrupKod, markaKod);

            // Cache'i kontrol et
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                if (DateTime.UtcNow < _cacheExpiry && _cache.TryGetValue(cacheKey, out var cachedMapping))
                {
                    _logger.LogDebug(
                        "[CategoryMappingService] Cache hit: {Key} → CategoryId: {CategoryId}",
                        cacheKey, cachedMapping?.CategoryId);
                    return cachedMapping;
                }
            }
            finally
            {
                _cacheLock.Release();
            }

            // Veritabanından sorgula
            var mapping = await FindMappingAsync(anagrupKod, altgrupKod, markaKod, cancellationToken);

            // Cache'e ekle
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                _cache[cacheKey] = mapping;
                _cacheExpiry = DateTime.UtcNow.Add(_cacheLifetime);
            }
            finally
            {
                _cacheLock.Release();
            }

            return mapping;
        }

        /// <inheritdoc />
        public async Task<(string AnagrupKod, string? AltgrupKod)?> GetMikroKodlarAsync(
            int categoryId,
            CancellationToken cancellationToken = default)
        {
            if (categoryId <= 0)
                return null;

            try
            {
                var mapping = await _dbContext.Set<MikroCategoryMapping>()
                    .AsNoTracking()
                    .Where(m => m.CategoryId == categoryId && m.IsActive)
                    .OrderBy(m => m.Priority) // En düşük priority önce
                    .FirstOrDefaultAsync(cancellationToken);

                if (mapping == null)
                {
                    _logger.LogDebug(
                        "[CategoryMappingService] CategoryId {CategoryId} için Mikro kodu bulunamadı",
                        categoryId);
                    return null;
                }

                _logger.LogDebug(
                    "[CategoryMappingService] CategoryId {CategoryId} → Anagrup: {Anagrup}, Altgrup: {Altgrup}",
                    categoryId, mapping.MikroAnagrupKod, mapping.MikroAltgrupKod);

                return (mapping.MikroAnagrupKod, mapping.MikroAltgrupKod);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[CategoryMappingService] CategoryId {CategoryId} için Mikro kodu aranırken hata",
                    categoryId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<MikroCategoryMapping> AddMappingAsync(
            MikroCategoryMapping mapping,
            CancellationToken cancellationToken = default)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            if (string.IsNullOrWhiteSpace(mapping.MikroAnagrupKod))
                throw new ArgumentException("MikroAnagrupKod zorunlu", nameof(mapping));

            if (mapping.CategoryId <= 0)
                throw new ArgumentException("CategoryId geçerli olmalı", nameof(mapping));

            try
            {
                // Mükerrer kontrolü
                var existing = await _dbContext.Set<MikroCategoryMapping>()
                    .FirstOrDefaultAsync(m =>
                        m.MikroAnagrupKod == mapping.MikroAnagrupKod &&
                        m.MikroAltgrupKod == mapping.MikroAltgrupKod &&
                        m.MikroMarkaKod == mapping.MikroMarkaKod,
                        cancellationToken);

                if (existing != null)
                {
                    _logger.LogWarning(
                        "[CategoryMappingService] Eşleme zaten mevcut. " +
                        "Anagrup: {Anagrup}, Altgrup: {Altgrup}, Marka: {Marka}",
                        mapping.MikroAnagrupKod, mapping.MikroAltgrupKod, mapping.MikroMarkaKod);

                    // Mevcut kaydı güncelle
                    existing.CategoryId = mapping.CategoryId;
                    existing.BrandId = mapping.BrandId;
                    existing.Priority = mapping.Priority;
                    existing.IsActive = mapping.IsActive;
                    existing.UpdatedAt = DateTime.UtcNow;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    ClearCache();

                    return existing;
                }

                // Yeni kayıt
                mapping.CreatedAt = DateTime.UtcNow;
                mapping.IsActive = true;

                await _dbContext.Set<MikroCategoryMapping>().AddAsync(mapping, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "[CategoryMappingService] Yeni eşleme eklendi. " +
                    "Anagrup: {Anagrup} → CategoryId: {CategoryId}",
                    mapping.MikroAnagrupKod, mapping.CategoryId);

                ClearCache();
                return mapping;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CategoryMappingService] Eşleme eklenirken hata");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MikroCategoryMapping>> GetAllMappingsAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.Set<MikroCategoryMapping>()
                    .AsNoTracking()
                    .Include(m => m.Category)
                    .OrderBy(m => m.MikroAnagrupKod)
                    .ThenBy(m => m.MikroAltgrupKod)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CategoryMappingService] Eşlemeler listelenirken hata");
                throw;
            }
        }

        #region Private Methods

        /// <summary>
        /// Veritabanından eşleme arar (öncelik sırasına göre).
        /// </summary>
        private async Task<MikroCategoryMapping?> FindMappingAsync(
            string anagrupKod,
            string? altgrupKod,
            string? markaKod,
            CancellationToken cancellationToken)
        {
            var mappings = _dbContext.Set<MikroCategoryMapping>();

            // 1. En spesifik: Anagrup + Altgrup + Marka
            if (!string.IsNullOrEmpty(altgrupKod) && !string.IsNullOrEmpty(markaKod))
            {
                var exactMatch = await mappings
                    .AsNoTracking()
                    .Where(m => m.IsActive &&
                               m.MikroAnagrupKod == anagrupKod &&
                               m.MikroAltgrupKod == altgrupKod &&
                               m.MikroMarkaKod == markaKod)
                    .OrderBy(m => m.Priority)
                    .FirstOrDefaultAsync(cancellationToken);

                if (exactMatch != null)
                {
                    _logger.LogDebug(
                        "[CategoryMappingService] Tam eşleşme bulundu. " +
                        "Anagrup+Altgrup+Marka → CategoryId: {CategoryId}",
                        exactMatch.CategoryId);
                    return exactMatch;
                }
            }

            // 2. Anagrup + Altgrup
            if (!string.IsNullOrEmpty(altgrupKod))
            {
                var altgrupMatch = await mappings
                    .AsNoTracking()
                    .Where(m => m.IsActive &&
                               m.MikroAnagrupKod == anagrupKod &&
                               m.MikroAltgrupKod == altgrupKod &&
                               m.MikroMarkaKod == null)
                    .OrderBy(m => m.Priority)
                    .FirstOrDefaultAsync(cancellationToken);

                if (altgrupMatch != null)
                {
                    _logger.LogDebug(
                        "[CategoryMappingService] Anagrup+Altgrup eşleşmesi bulundu. " +
                        "CategoryId: {CategoryId}",
                        altgrupMatch.CategoryId);
                    return altgrupMatch;
                }
            }

            // 3. Sadece Anagrup
            var anagrupMatch = await mappings
                .AsNoTracking()
                .Where(m => m.IsActive &&
                           m.MikroAnagrupKod == anagrupKod &&
                           m.MikroAltgrupKod == null &&
                           m.MikroMarkaKod == null)
                .OrderBy(m => m.Priority)
                .FirstOrDefaultAsync(cancellationToken);

            if (anagrupMatch != null)
            {
                _logger.LogDebug(
                    "[CategoryMappingService] Anagrup eşleşmesi bulundu. CategoryId: {CategoryId}",
                    anagrupMatch.CategoryId);
                return anagrupMatch;
            }

            // 4. Varsayılan kategori (Priority = 999, AnagrupKod = "*")
            var defaultMapping = await mappings
                .AsNoTracking()
                .Where(m => m.IsActive && m.MikroAnagrupKod == "*")
                .OrderBy(m => m.Priority)
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultMapping != null)
            {
                _logger.LogDebug(
                    "[CategoryMappingService] Varsayılan kategori kullanılıyor. CategoryId: {CategoryId}",
                    defaultMapping.CategoryId);
            }
            else
            {
                _logger.LogWarning(
                    "[CategoryMappingService] Eşleşme bulunamadı ve varsayılan tanımlı değil. " +
                    "AnagrupKod: {Anagrup}, AltgrupKod: {Altgrup}",
                    anagrupKod, altgrupKod);
            }

            return defaultMapping;
        }

        /// <summary>
        /// Cache key oluşturur.
        /// </summary>
        private string BuildCacheKey(string anagrupKod, string? altgrupKod, string? markaKod)
        {
            return $"{anagrupKod}|{altgrupKod ?? ""}|{markaKod ?? ""}".ToUpperInvariant();
        }

        /// <summary>
        /// Cache'i temizler.
        /// </summary>
        private void ClearCache()
        {
            _cacheLock.Wait();
            try
            {
                _cache.Clear();
                _cacheExpiry = DateTime.MinValue;
                _logger.LogDebug("[CategoryMappingService] Cache temizlendi");
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        #endregion
    }
}
