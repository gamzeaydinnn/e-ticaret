using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.XmlImport;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// XML Feed Source yönetim servisi.
    /// </summary>
    public class XmlFeedSourceManager : IXmlFeedSourceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<XmlFeedSourceManager> _logger;
        
        public XmlFeedSourceManager(
            IUnitOfWork unitOfWork, 
            IHttpClientFactory httpClientFactory,
            ILogger<XmlFeedSourceManager> logger)
        {
            _unitOfWork = unitOfWork;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        
        #region CRUD Operations
        
        public async Task<IEnumerable<XmlFeedSourceDto>> GetAllAsync()
        {
            var entities = await _unitOfWork.XmlFeedSources.GetAllAsync();
            return entities.Select(MapToDto);
        }
        
        public async Task<IEnumerable<XmlFeedSourceDto>> GetActiveAsync()
        {
            var entities = await _unitOfWork.XmlFeedSources.GetActiveSourcesAsync();
            return entities.Select(MapToDto);
        }
        
        public async Task<XmlFeedSourceDto?> GetByIdAsync(int feedSourceId)
        {
            var entity = await _unitOfWork.XmlFeedSources.GetByIdAsync(feedSourceId);
            return entity != null ? MapToDto(entity) : null;
        }
        
        public async Task<XmlFeedSourceDto> CreateAsync(XmlFeedSourceCreateDto dto)
        {
            var entity = new XmlFeedSource
            {
                Name = dto.Name,
                Url = dto.Url,
                SupplierName = dto.SupplierName,
                IsActive = true,
                AutoSyncEnabled = dto.AutoSyncEnabled,
                SyncIntervalMinutes = dto.SyncIntervalMinutes
            };
            
            if (dto.MappingConfig != null)
            {
                entity.MappingConfig = JsonSerializer.Serialize(dto.MappingConfig);
            }
            
            await _unitOfWork.XmlFeedSources.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Feed source oluşturuldu: {Name}", dto.Name);
            return MapToDto(entity);
        }
        
        public async Task<XmlFeedSourceDto> UpdateAsync(int feedSourceId, XmlFeedSourceCreateDto dto)
        {
            var entity = await _unitOfWork.XmlFeedSources.GetByIdAsync(feedSourceId);
            if (entity == null)
                throw new KeyNotFoundException($"Feed source bulunamadı: {feedSourceId}");
            
            entity.Name = dto.Name;
            entity.Url = dto.Url;
            entity.SupplierName = dto.SupplierName;
            entity.AutoSyncEnabled = dto.AutoSyncEnabled;
            entity.SyncIntervalMinutes = dto.SyncIntervalMinutes;
            
            if (dto.MappingConfig != null)
            {
                entity.MappingConfig = JsonSerializer.Serialize(dto.MappingConfig);
            }
            
            await _unitOfWork.XmlFeedSources.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Feed source güncellendi: ID={Id}", feedSourceId);
            return MapToDto(entity);
        }
        
        public async Task<bool> DeleteAsync(int feedSourceId)
        {
            var entity = await _unitOfWork.XmlFeedSources.GetByIdAsync(feedSourceId);
            if (entity == null) return false;
            
            await _unitOfWork.XmlFeedSources.DeleteAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Feed source silindi: ID={Id}", feedSourceId);
            return true;
        }
        
        #endregion
        
        #region Activation & Status
        
        public async Task<bool> SetActiveStatusAsync(int feedSourceId, bool isActive)
        {
            var entity = await _unitOfWork.XmlFeedSources.GetByIdAsync(feedSourceId);
            if (entity == null) return false;
            
            entity.IsActive = isActive;
            await _unitOfWork.XmlFeedSources.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        
        public async Task<FeedSourceStatusDto?> GetStatusAsync(int feedSourceId)
        {
            var entity = await _unitOfWork.XmlFeedSources.GetByIdAsync(feedSourceId);
            if (entity == null) return null;
            
            return new FeedSourceStatusDto
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                LastSyncAt = entity.LastSyncAt,
                NextSyncAt = entity.NextSyncAt,
                LastSyncSuccess = entity.LastSyncSuccess,
                LastErrorMessage = entity.LastSyncError,
                TotalProductsInFeed = 0,
                ActiveVariantsFromFeed = 0
            };
        }
        
        #endregion
        
        #region Sync Operations
        
        public async Task<IEnumerable<XmlFeedSourceDto>> GetDueForSyncAsync()
        {
            var entities = await _unitOfWork.XmlFeedSources.GetSourcesDueForSyncAsync();
            return entities.Select(MapToDto);
        }
        
        public async Task MarkSyncStartedAsync(int feedSourceId)
        {
            var entity = await _unitOfWork.XmlFeedSources.GetByIdAsync(feedSourceId);
            if (entity != null)
            {
                entity.TotalSyncCount++;
                await _unitOfWork.XmlFeedSources.UpdateAsync(entity);
                await _unitOfWork.SaveChangesAsync();
            }
        }
        
        public async Task UpdateSyncResultAsync(int feedSourceId, bool success, string? errorMessage = null)
        {
            var entity = await _unitOfWork.XmlFeedSources.GetByIdAsync(feedSourceId);
            if (entity != null)
            {
                entity.LastSyncAt = DateTime.UtcNow;
                entity.LastSyncSuccess = success;
                entity.LastSyncError = errorMessage;
                entity.NextSyncAt = DateTime.UtcNow.AddMinutes(entity.SyncIntervalMinutes ?? 60);
                await _unitOfWork.XmlFeedSources.UpdateAsync(entity);
                await _unitOfWork.SaveChangesAsync();
            }
        }
        
        public async Task UpdateStatisticsAsync(int feedSourceId, int totalProducts, int newProducts, int updatedProducts, int skippedProducts)
        {
            var entity = await _unitOfWork.XmlFeedSources.GetByIdAsync(feedSourceId);
            if (entity != null)
            {
                entity.LastSyncCreatedCount = newProducts;
                entity.LastSyncUpdatedCount = updatedProducts;
                entity.LastSyncFailedCount = skippedProducts;
                await _unitOfWork.XmlFeedSources.UpdateAsync(entity);
                await _unitOfWork.SaveChangesAsync();
            }
        }
        
        #endregion
        
        #region Mapping Configuration
        
        public async Task<XmlMappingConfigDto?> GetMappingConfigAsync(int feedSourceId)
        {
            var entity = await _unitOfWork.XmlFeedSources.GetByIdAsync(feedSourceId);
            if (entity == null || string.IsNullOrEmpty(entity.MappingConfig))
                return null;
            
            return JsonSerializer.Deserialize<XmlMappingConfigDto>(entity.MappingConfig);
        }
        
        public async Task<bool> UpdateMappingConfigAsync(int feedSourceId, XmlMappingConfigDto config)
        {
            var entity = await _unitOfWork.XmlFeedSources.GetByIdAsync(feedSourceId);
            if (entity == null) return false;
            
            entity.MappingConfig = JsonSerializer.Serialize(config);
            await _unitOfWork.XmlFeedSources.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        
        public async Task<XmlMappingConfigDto> SuggestMappingAsync(int feedSourceId)
        {
            var entity = await _unitOfWork.XmlFeedSources.GetByIdAsync(feedSourceId);
            if (entity == null)
                return GetDefaultMapping();
            
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var content = await client.GetStringAsync(entity.Url);
                
                var doc = XDocument.Parse(content);
                var root = doc.Root;
                
                if (root == null)
                    return GetDefaultMapping();
                
                var firstProduct = root.Elements().FirstOrDefault();
                if (firstProduct == null)
                    return GetDefaultMapping();
                
                var mapping = new XmlMappingConfigDto
                {
                    RootElement = root.Name.LocalName,
                    ItemElement = firstProduct.Name.LocalName
                };
                
                var childNames = firstProduct.Elements().Select(e => e.Name.LocalName.ToLowerInvariant()).ToList();
                
                if (childNames.Any(n => n.Contains("sku")))
                    mapping.SkuMapping = firstProduct.Elements().First(e => e.Name.LocalName.ToLowerInvariant().Contains("sku")).Name.LocalName;
                else if (childNames.Any(n => n.Contains("code")))
                    mapping.SkuMapping = firstProduct.Elements().First(e => e.Name.LocalName.ToLowerInvariant().Contains("code")).Name.LocalName;
                
                if (childNames.Any(n => n.Contains("name") || n.Contains("title")))
                    mapping.TitleMapping = firstProduct.Elements().First(e => 
                        e.Name.LocalName.ToLowerInvariant().Contains("name") || 
                        e.Name.LocalName.ToLowerInvariant().Contains("title")).Name.LocalName;
                
                if (childNames.Any(n => n.Contains("price")))
                    mapping.PriceMapping = firstProduct.Elements().First(e => e.Name.LocalName.ToLowerInvariant().Contains("price")).Name.LocalName;
                
                if (childNames.Any(n => n.Contains("stock") || n.Contains("quantity")))
                    mapping.StockMapping = firstProduct.Elements().First(e => 
                        e.Name.LocalName.ToLowerInvariant().Contains("stock") || 
                        e.Name.LocalName.ToLowerInvariant().Contains("quantity")).Name.LocalName;
                
                return mapping;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mapping önerisi oluşturulamadı: FeedSource {Id}", feedSourceId);
                return GetDefaultMapping();
            }
        }
        
        private XmlMappingConfigDto GetDefaultMapping()
        {
            return new XmlMappingConfigDto
            {
                ItemElement = "Product",
                SkuMapping = "SKU",
                TitleMapping = "ProductName",
                PriceMapping = "Price",
                StockMapping = "Stock"
            };
        }
        
        #endregion
        
        #region Validation
        
        public async Task<FeedValidationResult> ValidateFeedUrlAsync(string feedUrl)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                var response = await client.GetAsync(feedUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new FeedValidationResult
                    {
                        IsValid = false,
                        IsReachable = true,
                        IsValidXml = false,
                        Error = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                    };
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var doc = XDocument.Parse(content);
                var root = doc.Root;
                var productCount = root?.Elements().Count() ?? 0;
                var fields = root?.Elements().FirstOrDefault()?.Elements().Select(e => e.Name.LocalName).ToList();
                
                return new FeedValidationResult
                {
                    IsValid = true,
                    IsReachable = true,
                    IsValidXml = true,
                    EstimatedProductCount = productCount,
                    DetectedFields = fields
                };
            }
            catch (HttpRequestException ex)
            {
                return new FeedValidationResult
                {
                    IsValid = false,
                    IsReachable = false,
                    IsValidXml = false,
                    Error = $"Bağlantı hatası: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new FeedValidationResult
                {
                    IsValid = false,
                    IsReachable = true,
                    IsValidXml = false,
                    Error = $"XML parse hatası: {ex.Message}"
                };
            }
        }
        
        public async Task<FeedPreviewResult> PreviewFeedAsync(int feedSourceId, int sampleSize = 5)
        {
            var entity = await _unitOfWork.XmlFeedSources.GetByIdAsync(feedSourceId);
            if (entity == null)
            {
                return new FeedPreviewResult { Success = false, Error = "Feed source bulunamadı" };
            }
            
            try
            {
                var client = _httpClientFactory.CreateClient();
                var content = await client.GetStringAsync(entity.Url);
                var doc = XDocument.Parse(content);
                var root = doc.Root;
                
                var productElements = root?.Elements().Take(sampleSize).ToList() ?? new List<XElement>();
                var mapping = await GetMappingConfigAsync(feedSourceId) ?? GetDefaultMapping();
                
                var samples = new List<XmlProductItemDto>();
                foreach (var element in productElements)
                {
                    samples.Add(new XmlProductItemDto
                    {
                        SKU = element.Element(mapping.SkuMapping)?.Value,
                        Title = element.Element(mapping.TitleMapping)?.Value,
                        Price = ParseDecimal(element.Element(mapping.PriceMapping)?.Value),
                        Stock = ParseInt(element.Element(mapping.StockMapping)?.Value)
                    });
                }
                
                return new FeedPreviewResult
                {
                    Success = true,
                    TotalProducts = root?.Elements().Count() ?? 0,
                    SampleProducts = samples
                };
            }
            catch (Exception ex)
            {
                return new FeedPreviewResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private XmlFeedSourceDto MapToDto(XmlFeedSource entity)
        {
            return new XmlFeedSourceDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Url = entity.Url,
                SupplierName = entity.SupplierName,
                IsActive = entity.IsActive,
                AutoSyncEnabled = entity.AutoSyncEnabled,
                SyncIntervalMinutes = entity.SyncIntervalMinutes,
                LastSyncAt = entity.LastSyncAt,
                LastSyncSuccess = entity.LastSyncSuccess,
                LastSyncError = entity.LastSyncError,
                NextSyncAt = entity.NextSyncAt,
                LastSyncCreatedCount = entity.LastSyncCreatedCount,
                LastSyncUpdatedCount = entity.LastSyncUpdatedCount,
                LastSyncFailedCount = entity.LastSyncFailedCount,
                TotalSyncCount = entity.TotalSyncCount
            };
        }
        
        private decimal? ParseDecimal(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            value = value.Replace(",", ".").Trim();
            return decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;
        }
        
        private int? ParseInt(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return int.TryParse(value.Trim(), out var result) ? result : null;
        }
        
        #endregion
    }
}
