using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.DTOs.XmlImport;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// XML Import Manager - Tedarikçi XML feed'lerinden ürün ve varyant aktarımı.
    /// </summary>
    public class XmlImportManager : IXmlImportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IXmlFeedSourceService _feedSourceService;
        private readonly IProductVariantService _variantService;
        private readonly ILogger<XmlImportManager> _logger;
        
        private static readonly ConcurrentDictionary<string, ImportProgressDto> _activeImports = new();
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();
        
        public XmlImportManager(
            IUnitOfWork unitOfWork,
            IHttpClientFactory httpClientFactory,
            IXmlFeedSourceService feedSourceService,
            IProductVariantService variantService,
            ILogger<XmlImportManager> logger)
        {
            _unitOfWork = unitOfWork;
            _httpClientFactory = httpClientFactory;
            _feedSourceService = feedSourceService;
            _variantService = variantService;
            _logger = logger;
        }
        
        #region Manual Import
        
        public async Task<XmlImportResultDto> ImportFromFeedAsync(int feedSourceId, XmlImportRequestDto? options = null)
        {
            var feedSource = await _unitOfWork.XmlFeedSources.GetByIdAsync(feedSourceId);
            if (feedSource == null)
            {
                return XmlImportResultDto.CreateError("Feed source bulunamadı");
            }
            
            var importId = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource();
            _cancellationTokens[importId] = cts;
            
            var progress = new ImportProgressDto
            {
                ImportId = importId,
                FeedSourceId = feedSourceId,
                FeedSourceName = feedSource.Name,
                Status = ImportStatus.Downloading,
                StartedAt = DateTime.UtcNow
            };
            _activeImports[importId] = progress;
            
            try
            {
                await _feedSourceService.MarkSyncStartedAsync(feedSourceId);
                
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMinutes(5);
                
                var content = await client.GetStringAsync(feedSource.Url, cts.Token);
                var mapping = await _feedSourceService.GetMappingConfigAsync(feedSourceId);
                
                if (mapping == null)
                {
                    mapping = await _feedSourceService.SuggestMappingAsync(feedSourceId);
                }
                
                var result = await ProcessXmlContentAsync(content, mapping, feedSourceId, progress, cts.Token);
                
                await _feedSourceService.UpdateSyncResultAsync(feedSourceId, result.Success, result.Message);
                await _feedSourceService.UpdateStatisticsAsync(feedSourceId, 
                    result.TotalRecords, result.CreatedCount, result.UpdatedCount, result.FailedCount);
                
                return result;
            }
            catch (OperationCanceledException)
            {
                progress.Status = ImportStatus.Cancelled;
                progress.CompletedAt = DateTime.UtcNow;
                return XmlImportResultDto.CreateError("Import işlemi iptal edildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XML import hatası: FeedSource {FeedSourceId}", feedSourceId);
                progress.Status = ImportStatus.Failed;
                progress.CompletedAt = DateTime.UtcNow;
                await _feedSourceService.UpdateSyncResultAsync(feedSourceId, false, ex.Message);
                return XmlImportResultDto.CreateError(ex.Message);
            }
            finally
            {
                _cancellationTokens.TryRemove(importId, out var _);
                ScheduleProgressCleanup(importId);
            }
        }
        
        private void ScheduleProgressCleanup(string importId)
        {
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(30));
                _activeImports.TryRemove(importId, out var _);
            });
        }
        
        public async Task<XmlImportResultDto> ImportFromUrlAsync(string url, XmlMappingConfigDto mapping)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMinutes(5);
                var content = await client.GetStringAsync(url);
                
                var progress = new ImportProgressDto
                {
                    ImportId = Guid.NewGuid().ToString(),
                    Status = ImportStatus.Processing,
                    StartedAt = DateTime.UtcNow
                };
                
                return await ProcessXmlContentAsync(content, mapping, null, progress, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "URL'den import hatası: {Url}", url);
                return XmlImportResultDto.CreateError(ex.Message);
            }
        }
        
        public async Task<XmlImportResultDto> ImportFromContentAsync(string xmlContent, XmlMappingConfigDto mapping, int? feedSourceId = null)
        {
            var progress = new ImportProgressDto
            {
                ImportId = Guid.NewGuid().ToString(),
                FeedSourceId = feedSourceId ?? 0,
                Status = ImportStatus.Processing,
                StartedAt = DateTime.UtcNow
            };
            
            return await ProcessXmlContentAsync(xmlContent, mapping, feedSourceId, progress, CancellationToken.None);
        }
        
        #endregion
        
        #region Scheduled Sync
        
        public async Task<IEnumerable<XmlImportResultDto>> SyncAllDueFeedsAsync()
        {
            var dueFeedSources = await _feedSourceService.GetDueForSyncAsync();
            var results = new List<XmlImportResultDto>();
            
            foreach (var feedSource in dueFeedSources)
            {
                _logger.LogInformation("Scheduled sync başlıyor: {FeedSourceName}", feedSource.Name);
                var result = await ImportFromFeedAsync(feedSource.Id);
                results.Add(result);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            
            return results;
        }
        
        public async Task<XmlImportResultDto> ForceSyncFeedAsync(int feedSourceId)
        {
            return await ImportFromFeedAsync(feedSourceId, new XmlImportRequestDto());
        }
        
        #endregion
        
        #region Import Progress & Status
        
        public Task<IEnumerable<ImportProgressDto>> GetActiveImportsAsync()
        {
            var active = _activeImports.Values
                .Where(p => p.Status == ImportStatus.Downloading || 
                           p.Status == ImportStatus.Parsing || 
                           p.Status == ImportStatus.Processing)
                .ToList();
            return Task.FromResult<IEnumerable<ImportProgressDto>>(active);
        }
        
        public Task<ImportProgressDto?> GetImportProgressAsync(string importId)
        {
            _activeImports.TryGetValue(importId, out var progress);
            return Task.FromResult(progress);
        }
        
        public Task<bool> CancelImportAsync(string importId)
        {
            if (_cancellationTokens.TryGetValue(importId, out var cts))
            {
                cts.Cancel();
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        
        #endregion
        
        #region Import History
        
        public Task<IEnumerable<XmlImportResultDto>> GetImportHistoryAsync(int? feedSourceId = null, int page = 1, int pageSize = 20)
        {
            var completed = _activeImports.Values
                .Where(p => p.Status == ImportStatus.Completed || p.Status == ImportStatus.Failed)
                .Where(p => !feedSourceId.HasValue || p.FeedSourceId == feedSourceId.Value)
                .OrderByDescending(p => p.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new XmlImportResultDto
                {
                    Success = p.Status == ImportStatus.Completed,
                    TotalRecords = p.TotalItems,
                    CreatedCount = p.SuccessCount,
                    StartedAt = p.StartedAt,
                    CompletedAt = p.CompletedAt
                })
                .ToList();
            
            return Task.FromResult<IEnumerable<XmlImportResultDto>>(completed);
        }
        
        public Task<XmlImportResultDto?> GetImportDetailAsync(string importId)
        {
            if (_activeImports.TryGetValue(importId, out var progress))
            {
                return Task.FromResult<XmlImportResultDto?>(new XmlImportResultDto
                {
                    Success = progress.Status == ImportStatus.Completed,
                    TotalRecords = progress.TotalItems,
                    ProcessedRecords = progress.ProcessedItems,
                    CreatedCount = progress.SuccessCount,
                    FailedCount = progress.ErrorCount,
                    StartedAt = progress.StartedAt,
                    CompletedAt = progress.CompletedAt
                });
            }
            return Task.FromResult<XmlImportResultDto?>(null);
        }
        
        #endregion
        
        #region Validation & Preview
        
        public Task<XmlValidationResultDto> ValidateXmlAsync(string xmlContent, XmlMappingConfigDto? mapping = null)
        {
            var result = new XmlValidationResultDto();
            var errors = new List<XmlValidationError>();
            
            try
            {
                var doc = XDocument.Parse(xmlContent);
                result.IsValid = true;
                
                if (mapping != null && doc.Root != null)
                {
                    var productElements = doc.Root.Elements().ToList();
                    result.TotalProducts = productElements.Count;
                    
                    int validCount = 0;
                    int lineNumber = 0;
                    
                    foreach (var element in productElements)
                    {
                        lineNumber++;
                        var hasError = false;
                        
                        var sku = element.Element(mapping.SkuMapping)?.Value;
                        if (string.IsNullOrEmpty(sku))
                        {
                            errors.Add(new XmlValidationError
                            {
                                Line = lineNumber,
                                Field = "SKU",
                                Message = "SKU alanı boş"
                            });
                            hasError = true;
                        }
                        
                        var priceStr = element.Element(mapping.PriceMapping)?.Value;
                        if (!decimal.TryParse(priceStr?.Replace(",", "."), 
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var price) || price <= 0)
                        {
                            errors.Add(new XmlValidationError
                            {
                                Line = lineNumber,
                                Field = "Price",
                                Message = "Geçersiz fiyat değeri",
                                Value = priceStr
                            });
                            hasError = true;
                        }
                        
                        if (!hasError) validCount++;
                    }
                    
                    result.ValidProducts = validCount;
                    result.InvalidProducts = result.TotalProducts - validCount;
                }
                
                result.Errors = errors;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                errors.Add(new XmlValidationError { Message = $"XML parse hatası: {ex.Message}" });
                result.Errors = errors;
            }
            
            return Task.FromResult(result);
        }
        
        public async Task<XmlImportPreviewDto> PreviewImportAsync(int feedSourceId, int sampleSize = 10)
        {
            var feedPreview = await _feedSourceService.PreviewFeedAsync(feedSourceId, sampleSize);
            
            if (!feedPreview.Success)
            {
                return new XmlImportPreviewDto
                {
                    Success = false,
                    Error = feedPreview.Error
                };
            }
            
            var sampleProducts = new List<ProductPreviewDto>();
            foreach (var item in feedPreview.SampleProducts)
            {
                sampleProducts.Add(new ProductPreviewDto
                {
                    Sku = item.SKU ?? string.Empty,
                    Name = item.Title ?? string.Empty,
                    Price = item.Price ?? 0,
                    Stock = item.Stock ?? 0,
                    Status = "New"
                });
            }
            
            return new XmlImportPreviewDto
            {
                Success = true,
                TotalProducts = feedPreview.TotalProducts,
                SampleProducts = sampleProducts
            };
        }
        
        #endregion
        
        #region Conflict Resolution
        
        public Task<IEnumerable<ImportConflictDto>> GetConflictsAsync(string importId)
        {
            return Task.FromResult<IEnumerable<ImportConflictDto>>(new List<ImportConflictDto>());
        }
        
        public Task<bool> ResolveConflictAsync(string conflictId, ConflictResolution resolution)
        {
            return Task.FromResult(false);
        }
        
        #endregion
        
        #region Cleanup
        
        public async Task<int> CleanupStaleProductsAsync(int feedSourceId, int hoursThreshold = 48)
        {
            return await _variantService.DeactivateStaleVariantsAsync(feedSourceId, hoursThreshold);
        }
        
        public async Task<int> MarkRemovedProductsAsync(int feedSourceId)
        {
            return await _variantService.DeactivateStaleVariantsAsync(feedSourceId, 24);
        }
        
        #endregion
        
        #region Private Helpers
        
        private async Task<XmlImportResultDto> ProcessXmlContentAsync(
            string xmlContent, 
            XmlMappingConfigDto mapping, 
            int? feedSourceId,
            ImportProgressDto progress,
            CancellationToken cancellationToken)
        {
            var result = new XmlImportResultDto { StartedAt = progress.StartedAt };
            
            try
            {
                progress.Status = ImportStatus.Parsing;
                var doc = XDocument.Parse(xmlContent);
                var productElements = doc.Root?.Elements().ToList() ?? new List<XElement>();
                
                progress.TotalItems = productElements.Count;
                progress.Status = ImportStatus.Processing;
                result.TotalRecords = productElements.Count;
                
                _logger.LogInformation("XML import başlıyor: {Count} ürün", productElements.Count);
                
                var batchSize = 100;
                var processedSkus = new HashSet<string>();
                
                for (int i = 0; i < productElements.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var element = productElements[i];
                    progress.ProcessedItems = i + 1;
                    
                    try
                    {
                        var sku = element.Element(mapping.SkuMapping)?.Value ?? string.Empty;
                        var name = element.Element(mapping.TitleMapping)?.Value ?? string.Empty;
                        var priceStr = element.Element(mapping.PriceMapping)?.Value;
                        var stockStr = element.Element(mapping.StockMapping)?.Value;
                        
                        progress.CurrentItem = sku;
                        
                        if (string.IsNullOrEmpty(sku))
                        {
                            progress.SkippedCount++;
                            result.SkippedCount++;
                            continue;
                        }
                        
                        if (processedSkus.Contains(sku))
                        {
                            progress.SkippedCount++;
                            result.SkippedCount++;
                            continue;
                        }
                        processedSkus.Add(sku);
                        
                        var price = ParseDecimal(priceStr);
                        var stock = ParseInt(stockStr);
                        
                        var existingVariant = await _variantService.GetBySkuAsync(sku);
                        
                        if (existingVariant == null)
                        {
                            var productId = await CreateProductAsync(name, price, stock);
                            
                            await _variantService.CreateAsync(productId, new ProductVariantCreateDto
                            {
                                SKU = sku,
                                Title = name,
                                Price = price,
                                Stock = stock
                            });
                            
                            progress.SuccessCount++;
                            result.CreatedCount++;
                        }
                        else
                        {
                            await _variantService.UpdateAsync(existingVariant.Id, new ProductVariantUpdateDto
                            {
                                Price = price,
                                Stock = stock
                            });
                            
                            progress.SuccessCount++;
                            result.UpdatedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        progress.ErrorCount++;
                        result.FailedCount++;
                        result.Errors.Add(new XmlImportErrorDto
                        {
                            RowNumber = i + 1,
                            ErrorMessage = ex.Message,
                            ErrorType = "Processing"
                        });
                        
                        _logger.LogWarning(ex, "Ürün import hatası: Satır {Line}", i + 1);
                    }
                    
                    if ((i + 1) % batchSize == 0)
                    {
                        await _unitOfWork.SaveChangesAsync();
                    }
                }
                
                await _unitOfWork.SaveChangesAsync();
                
                progress.Status = ImportStatus.Completed;
                progress.CompletedAt = DateTime.UtcNow;
                
                result.Success = true;
                result.ProcessedRecords = productElements.Count;
                result.CompletedAt = DateTime.UtcNow;
                result.Message = $"Import tamamlandı: {result.CreatedCount} yeni, {result.UpdatedCount} güncelleme";
                
                _logger.LogInformation(
                    "XML import tamamlandı: Toplam={Total}, Yeni={New}, Güncellenen={Updated}",
                    result.TotalRecords, result.CreatedCount, result.UpdatedCount);
            }
            catch (Exception ex)
            {
                progress.Status = ImportStatus.Failed;
                progress.CompletedAt = DateTime.UtcNow;
                
                result.Success = false;
                result.Message = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
                
                _logger.LogError(ex, "XML import başarısız");
            }
            
            return result;
        }
        
        private async Task<int> CreateProductAsync(string name, decimal price, int stock)
        {
            var product = new Product
            {
                Name = name,
                Description = string.Empty,
                Price = price,
                StockQuantity = stock,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            
            return product.Id;
        }
        
        private decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            value = value.Replace(",", ".").Trim();
            return decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
        }
        
        private int ParseInt(string? value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            return int.TryParse(value.Trim(), out var result) ? result : 0;
        }
        
        #endregion
    }
}
