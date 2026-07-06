using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.Helpers;
using ECommerce.Data.Context;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Services.MicroServices;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Infrastructure
{
    /// <summary>
    /// Admin katalog istatistikleri — Mikro ERP + yerel DB birleşik ürün sayımları.
    /// Kategori yönetimi ekranındaki ürün adetleri bu servisten gelmelidir.
    /// </summary>
    public sealed class AdminCatalogStatsService : IAdminCatalogStatsService
    {
        private readonly IMikroDbService _mikroDbService;
        private readonly ECommerceDbContext _dbContext;
        private readonly IProductAdminOverrideSettingsService _productAdminOverrideSettingsService;

        public AdminCatalogStatsService(
            IMikroDbService mikroDbService,
            ECommerceDbContext dbContext,
            IProductAdminOverrideSettingsService productAdminOverrideSettingsService)
        {
            _mikroDbService = mikroDbService;
            _dbContext = dbContext;
            _productAdminOverrideSettingsService = productAdminOverrideSettingsService;
        }

        public async Task<IReadOnlyDictionary<int, int>> GetActiveProductCountsByCategoryAsync(
            CancellationToken cancellationToken = default)
        {
            var snapshots = await GetProductSnapshotsAsync(cancellationToken);

            return snapshots
                .Where(product => product.IsActive && product.CategoryId.HasValue && product.CategoryId.Value > 0)
                .GroupBy(product => product.CategoryId!.Value)
                .ToDictionary(group => group.Key, group => group.Count());
        }

        public Task<IReadOnlyList<AdminCatalogProductSnapshot>> GetProductSnapshotsAsync(
            CancellationToken cancellationToken = default)
        {
            return BuildProductSnapshotsAsync(cancellationToken);
        }

        private async Task<IReadOnlyList<AdminCatalogProductSnapshot>> BuildProductSnapshotsAsync(
            CancellationToken cancellationToken)
        {
            if (_mikroDbService.IsConfigured)
            {
                var unified = MikroWebCatalogFilter.OnlyWebActive(
                    await _mikroDbService.GetUnifiedProductsAsync(null, null, cancellationToken));
                if (unified.Count == 0)
                {
                    var fallbackLocalProducts = await _dbContext.Products
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                    return fallbackLocalProducts
                        .Select(MapLocalProductSnapshot)
                        .ToList();
                }

                var localAll = await _dbContext.Products
                    .Include(product => product.Category)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var skuToLocal = localAll
                    .Where(product => !string.IsNullOrWhiteSpace(product.SKU))
                    .GroupBy(product => product.SKU!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

                var categoryMappings = await LoadActiveCategoryMappingsAsync(cancellationToken);
                var activeCategories = await _dbContext.Categories
                    .AsNoTracking()
                    .Where(category => category.IsActive)
                    .ToListAsync(cancellationToken);

                var idToSlug = activeCategories
                    .Where(category => category.Id > 0)
                    .GroupBy(category => category.Id)
                    .ToDictionary(
                        group => group.Key,
                        group => AdminCatalogCategoryResolver.NormalizeCategorySlug(
                            group.First().Slug ?? group.First().Name),
                        EqualityComparer<int>.Default);

                var slugToName = activeCategories
                    .SelectMany(category => new[]
                    {
                        new KeyValuePair<string, string>(
                            AdminCatalogCategoryResolver.NormalizeCategorySlug(category.Slug),
                            category.Name ?? string.Empty),
                        new KeyValuePair<string, string>(
                            AdminCatalogCategoryResolver.NormalizeCategorySlug(category.Name),
                            category.Name ?? string.Empty)
                    })
                    .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                    .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.OrdinalIgnoreCase);

                var overrideDefaults = await _productAdminOverrideSettingsService
                    .GetSettingsAsync(cancellationToken);

                var mergedProducts = unified
                    .Select(mikroProduct =>
                    {
                        var normalizedSku = mikroProduct.StokKod?.Trim() ?? string.Empty;
                        var hasLocal = skuToLocal.TryGetValue(normalizedSku, out var local);
                        var resolvedCategoryInfo = AdminCatalogCategoryResolver.ResolveCategoryInfo(
                            mikroProduct.AnagrupKod,
                            mikroProduct.GrupKod,
                            mikroProduct.StokAd,
                            categoryMappings,
                            idToSlug,
                            slugToName);

                        var resolvedCategoryId = hasLocal &&
                                               ProductAdminOverridePolicy.ShouldUseAdminCategory(local, overrideDefaults)
                            ? (int?)local!.CategoryId
                            : resolvedCategoryInfo.CategoryId;

                        return new AdminCatalogProductSnapshot
                        {
                            Id = hasLocal ? local!.Id : 0,
                            Sku = mikroProduct.StokKod ?? string.Empty,
                            Name = ProductAdminOverridePolicy.ResolveName(
                                mikroProduct.StokAd,
                                hasLocal ? local : null,
                                overrideDefaults),
                            StockQuantity = (int)Math.Max(0, mikroProduct.StokMiktar),
                            IsActive = MikroWebCatalogFilter.ResolveIsActive(mikroProduct, hasLocal ? local : null),
                            CategoryId = resolvedCategoryId,
                        };
                    })
                    .GroupBy(
                        product => !string.IsNullOrWhiteSpace(product.Sku)
                            ? product.Sku.Trim()
                            : $"local:{product.Id}",
                        StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First());

                var localOnlyProducts = localAll
                    .Where(product => string.IsNullOrWhiteSpace(product.SKU))
                    .Select(MapLocalProductSnapshot);

                return mergedProducts.Concat(localOnlyProducts).ToList();
            }

            var localProducts = await _dbContext.Products
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return localProducts.Select(MapLocalProductSnapshot).ToList();
        }

        private static AdminCatalogProductSnapshot MapLocalProductSnapshot(Product product)
        {
            return new AdminCatalogProductSnapshot
            {
                Id = product.Id,
                Sku = product.SKU ?? string.Empty,
                Name = product.Name ?? string.Empty,
                StockQuantity = product.StockQuantity,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId > 0 ? product.CategoryId : null,
            };
        }

        private async Task<Dictionary<string, List<MikroCategoryMapping>>> LoadActiveCategoryMappingsAsync(
            CancellationToken cancellationToken)
        {
            var mappings = await _dbContext.MikroCategoryMappings
                .AsNoTracking()
                .Where(mapping => mapping.IsActive)
                .OrderByDescending(mapping => mapping.Priority)
                .ThenBy(mapping => mapping.Id)
                .ToListAsync(cancellationToken);

            return mappings
                .GroupBy(mapping => mapping.MikroAnagrupKod, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList(),
                    StringComparer.OrdinalIgnoreCase);
        }
    }
}
