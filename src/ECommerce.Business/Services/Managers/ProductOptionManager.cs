using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.ProductOption;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Product Option Manager - Renk, Beden gibi seçeneklerin yönetimi.
    /// </summary>
    public class ProductOptionManager : IProductOptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductOptionManager> _logger;
        
        public ProductOptionManager(
            IUnitOfWork unitOfWork,
            ILogger<ProductOptionManager> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        
        #region Option Management
        
        public async Task<IEnumerable<ProductOptionDto>> GetAllOptionsAsync()
        {
            var options = await _unitOfWork.ProductOptions.GetAllWithValuesAsync();
            return options.Select(MapToDto);
        }
        
        public async Task<ProductOptionDto?> GetOptionByIdAsync(int optionId)
        {
            var option = await _unitOfWork.ProductOptions.GetByIdWithValuesAsync(optionId);
            return option != null ? MapToDto(option) : null;
        }
        
        public async Task<ProductOptionDto> GetOrCreateOptionAsync(string name)
        {
            var option = await _unitOfWork.ProductOptions.GetOrCreateOptionAsync(name);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(option);
        }
        
        public async Task<ProductOptionDto> UpdateOptionAsync(int optionId, ProductOptionCreateDto dto)
        {
            var option = await _unitOfWork.ProductOptions.GetByIdWithValuesAsync(optionId);
            if (option == null)
            {
                throw new KeyNotFoundException($"Option bulunamadı: {optionId}");
            }
            
            option.Name = dto.Name;
            option.DisplayOrder = dto.DisplayOrder;
            
            await _unitOfWork.ProductOptions.UpdateAsync(option);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Option güncellendi: {Name}", dto.Name);
            
            return MapToDto(option);
        }
        
        public async Task<bool> DeleteOptionAsync(int optionId)
        {
            var option = await _unitOfWork.ProductOptions.GetByIdWithValuesAsync(optionId);
            if (option == null) return false;
            
            await _unitOfWork.ProductOptions.DeleteAsync(option);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Option silindi: {Name}", option.Name);
            return true;
        }
        
        #endregion
        
        #region Option Value Management
        
        public async Task<IEnumerable<ProductOptionValueDto>> GetValuesByOptionIdAsync(int optionId)
        {
            var option = await _unitOfWork.ProductOptions.GetByIdWithValuesAsync(optionId);
            if (option?.OptionValues == null)
            {
                return Enumerable.Empty<ProductOptionValueDto>();
            }
            
            return option.OptionValues.Select(v => new ProductOptionValueDto
            {
                Id = v.Id,
                OptionId = option.Id,
                Value = v.Value,
                DisplayOrder = v.DisplayOrder,
                IsActive = true
            });
        }
        
        public async Task<ProductOptionValueDto> GetOrCreateValueAsync(int optionId, string value)
        {
            var optionValue = await _unitOfWork.ProductOptions.GetOrCreateValueAsync(optionId, value);
            await _unitOfWork.SaveChangesAsync();
            
            return new ProductOptionValueDto
            {
                Id = optionValue.Id,
                OptionId = optionId,
                Value = optionValue.Value,
                DisplayOrder = optionValue.DisplayOrder,
                IsActive = true
            };
        }
        
        public async Task<IEnumerable<ProductOptionValueDto>> GetOrCreateValuesAsync(int optionId, IEnumerable<string> values)
        {
            var result = new List<ProductOptionValueDto>();
            
            foreach (var value in values)
            {
                var dto = await GetOrCreateValueAsync(optionId, value);
                result.Add(dto);
            }
            
            return result;
        }
        
        public async Task<ProductOptionValueDto> UpdateValueAsync(int valueId, string newValue)
        {
            // Basitleştirilmiş implementasyon
            var options = await _unitOfWork.ProductOptions.GetAllWithValuesAsync();
            
            foreach (var option in options)
            {
                var value = option.OptionValues?.FirstOrDefault(v => v.Id == valueId);
                if (value != null)
                {
                    value.Value = newValue;
                    await _unitOfWork.ProductOptions.UpdateAsync(option);
                    await _unitOfWork.SaveChangesAsync();
                    
                    return new ProductOptionValueDto
                    {
                        Id = value.Id,
                        OptionId = option.Id,
                        Value = value.Value,
                        DisplayOrder = value.DisplayOrder,
                        IsActive = true
                    };
                }
            }
            
            throw new KeyNotFoundException($"Option value bulunamadı: {valueId}");
        }
        
        public async Task<bool> DeleteValueAsync(int valueId)
        {
            var options = await _unitOfWork.ProductOptions.GetAllWithValuesAsync();
            
            foreach (var option in options)
            {
                var value = option.OptionValues?.FirstOrDefault(v => v.Id == valueId);
                if (value != null)
                {
                    option.OptionValues!.Remove(value);
                    await _unitOfWork.ProductOptions.UpdateAsync(option);
                    await _unitOfWork.SaveChangesAsync();
                    
                    _logger.LogInformation("Option value silindi: {Value}", value.Value);
                    return true;
                }
            }
            
            return false;
        }
        
        #endregion
        
        #region Product-Specific Operations
        
        public Task<IEnumerable<ProductOptionDto>> GetOptionsForProductAsync(int productId)
        {
            // TODO: Implement product-specific options
            _logger.LogWarning("GetOptionsForProductAsync henüz tam implemente değil");
            return Task.FromResult(Enumerable.Empty<ProductOptionDto>());
        }
        
        public Task<IEnumerable<ProductOptionDto>> GetOptionsForCategoryAsync(int categoryId)
        {
            // TODO: Implement category-specific options
            _logger.LogWarning("GetOptionsForCategoryAsync henüz tam implemente değil");
            return Task.FromResult(Enumerable.Empty<ProductOptionDto>());
        }
        
        public Task<IEnumerable<ProductOptionDto>> GetMostUsedOptionsAsync(int limit = 10)
        {
            // TODO: Implement most used options query
            _logger.LogWarning("GetMostUsedOptionsAsync henüz tam implemente değil");
            return Task.FromResult(Enumerable.Empty<ProductOptionDto>());
        }
        
        #endregion
        
        #region Batch Operations
        
        public async Task<IDictionary<string, IDictionary<string, int>>> BatchGetOrCreateAsync(
            IDictionary<string, IEnumerable<string>> optionValueMap)
        {
            var result = new Dictionary<string, IDictionary<string, int>>();
            
            foreach (var kvp in optionValueMap)
            {
                var optionName = kvp.Key;
                var values = kvp.Value;
                
                var option = await _unitOfWork.ProductOptions.GetOrCreateOptionAsync(optionName);
                var valueDict = new Dictionary<string, int>();
                
                foreach (var value in values)
                {
                    var optionValue = await _unitOfWork.ProductOptions.GetOrCreateValueAsync(option.Id, value);
                    valueDict[value] = optionValue.Id;
                }
                
                result[optionName] = valueDict;
            }
            
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Batch option/value oluşturuldu: {Count} option", result.Count);
            
            return result;
        }
        
        #endregion
        
        #region Private Helpers
        
        private ProductOptionDto MapToDto(ProductOption option)
        {
            return new ProductOptionDto
            {
                Id = option.Id,
                Name = option.Name,
                DisplayOrder = option.DisplayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Values = option.OptionValues?.Select(v => new ProductOptionValueDto
                {
                    Id = v.Id,
                    OptionId = option.Id,
                    Value = v.Value,
                    DisplayOrder = v.DisplayOrder,
                    IsActive = true
                }).ToList() ?? new List<ProductOptionValueDto>()
            };
        }
        
        #endregion
    }
}
