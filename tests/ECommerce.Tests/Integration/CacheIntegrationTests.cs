using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Task 9.2: Cache integration testi
/// Gereksinim: 8.3
/// </summary>
public class CacheIntegrationTests : IDisposable
{
    private readonly IMemoryCache _cache;

    public CacheIntegrationTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public void Cache_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var key = "test_key";
        var value = "test_value";

        // Act
        _cache.Set(key, value);
        var retrieved = _cache.Get<string>(key);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public void Cache_WithExpiration_ExpiresCorrectly()
    {
        // Arrange
        var key = "expiring_key";
        var value = "expiring_value";
        var expiration = TimeSpan.FromMilliseconds(100);

        // Act
        _cache.Set(key, value, expiration);
        var immediateRetrieve = _cache.Get<string>(key);

        Thread.Sleep(150); // Wait for expiration

        var afterExpiration = _cache.Get<string>(key);

        // Assert
        Assert.Equal(value, immediateRetrieve);
        Assert.Null(afterExpiration);
    }

    [Fact]
    public void Cache_Remove_RemovesEntry()
    {
        // Arrange
        var key = "removable_key";
        var value = "removable_value";

        // Act
        _cache.Set(key, value);
        var beforeRemove = _cache.Get<string>(key);

        _cache.Remove(key);
        var afterRemove = _cache.Get<string>(key);

        // Assert
        Assert.Equal(value, beforeRemove);
        Assert.Null(afterRemove);
    }

    [Fact]
    public void Cache_TryGetValue_ReturnsTrueWhenExists()
    {
        // Arrange
        var key = "existing_key";
        var value = "existing_value";
        _cache.Set(key, value);

        // Act
        var exists = _cache.TryGetValue(key, out string? retrieved);

        // Assert
        Assert.True(exists);
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public void Cache_TryGetValue_ReturnsFalseWhenNotExists()
    {
        // Arrange
        var key = "non_existing_key";

        // Act
        var exists = _cache.TryGetValue(key, out string? retrieved);

        // Assert
        Assert.False(exists);
        Assert.Null(retrieved);
    }

    [Fact]
    public void Cache_GetOrCreate_CreatesWhenNotExists()
    {
        // Arrange
        var key = "create_key";
        var expectedValue = "created_value";

        // Act
        var value = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return expectedValue;
        });

        // Assert
        Assert.Equal(expectedValue, value);
    }

    [Fact]
    public void Cache_GetOrCreate_ReturnsExistingWhenExists()
    {
        // Arrange
        var key = "existing_create_key";
        var existingValue = "existing";
        var newValue = "new";

        _cache.Set(key, existingValue);

        // Act
        var value = _cache.GetOrCreate(key, entry =>
        {
            return newValue; // This should not be called
        });

        // Assert
        Assert.Equal(existingValue, value);
    }

    [Fact]
    public void Cache_MultipleKeys_WorkIndependently()
    {
        // Arrange
        var key1 = "key1";
        var key2 = "key2";
        var value1 = "value1";
        var value2 = "value2";

        // Act
        _cache.Set(key1, value1);
        _cache.Set(key2, value2);

        var retrieved1 = _cache.Get<string>(key1);
        var retrieved2 = _cache.Get<string>(key2);

        _cache.Remove(key1);

        var afterRemove1 = _cache.Get<string>(key1);
        var afterRemove2 = _cache.Get<string>(key2);

        // Assert
        Assert.Equal(value1, retrieved1);
        Assert.Equal(value2, retrieved2);
        Assert.Null(afterRemove1);
        Assert.Equal(value2, afterRemove2);
    }

    [Fact]
    public void Cache_WithComplexObject_WorksCorrectly()
    {
        // Arrange
        var key = "complex_key";
        var value = new TestProduct
        {
            StokKod = "TEST001",
            UrunAdi = "Test Product",
            Fiyat = 100.50m,
            StokMiktar = 10
        };

        // Act
        _cache.Set(key, value);
        var retrieved = _cache.Get<TestProduct>(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(value.StokKod, retrieved.StokKod);
        Assert.Equal(value.UrunAdi, retrieved.UrunAdi);
        Assert.Equal(value.Fiyat, retrieved.Fiyat);
        Assert.Equal(value.StokMiktar, retrieved.StokMiktar);
    }

    [Fact]
    public void Cache_WithSlidingExpiration_RenewsOnAccess()
    {
        // Arrange
        var key = "sliding_key";
        var value = "sliding_value";
        var slidingExpiration = TimeSpan.FromMilliseconds(200);

        // Act
        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            SlidingExpiration = slidingExpiration
        });

        // Access multiple times within sliding window
        Thread.Sleep(100);
        var access1 = _cache.Get<string>(key);

        Thread.Sleep(100);
        var access2 = _cache.Get<string>(key);

        Thread.Sleep(100);
        var access3 = _cache.Get<string>(key);

        // Wait for expiration without access
        Thread.Sleep(250);
        var afterExpiration = _cache.Get<string>(key);

        // Assert
        Assert.Equal(value, access1);
        Assert.Equal(value, access2);
        Assert.Equal(value, access3);
        Assert.Null(afterExpiration);
    }

    [Fact]
    public void Cache_KeyGeneration_ForMikroProducts_IsConsistent()
    {
        // Arrange
        var depoNo = 1;
        var fiyatListesiNo = 2;
        var grupKod = "GIDA";
        var sadeceStoklu = true;

        // Act
        var key1 = GenerateCacheKey(depoNo, fiyatListesiNo, grupKod, sadeceStoklu);
        var key2 = GenerateCacheKey(depoNo, fiyatListesiNo, grupKod, sadeceStoklu);

        // Assert
        Assert.Equal(key1, key2);
        Assert.Contains("mikro_products", key1);
        Assert.Contains("1", key1);
        Assert.Contains("2", key1);
        Assert.Contains("GIDA", key1);
    }

    [Fact]
    public void Cache_KeyGeneration_WithDifferentParams_GeneratesDifferentKeys()
    {
        // Arrange & Act
        var key1 = GenerateCacheKey(1, 1, "GIDA", true);
        var key2 = GenerateCacheKey(2, 1, "GIDA", true);
        var key3 = GenerateCacheKey(1, 2, "GIDA", true);
        var key4 = GenerateCacheKey(1, 1, "ICECEK", true);
        var key5 = GenerateCacheKey(1, 1, "GIDA", false);

        // Assert
        Assert.NotEqual(key1, key2);
        Assert.NotEqual(key1, key3);
        Assert.NotEqual(key1, key4);
        Assert.NotEqual(key1, key5);
    }

    // Helper methods
    private string GenerateCacheKey(int? depoNo, int? fiyatListesiNo, string? grupKod, bool? sadeceStoklu)
    {
        return $"mikro_products_{depoNo ?? 0}_{fiyatListesiNo ?? 0}_{grupKod ?? "all"}_{sadeceStoklu ?? false}";
    }

    public void Dispose()
    {
        _cache?.Dispose();
    }

    // Test helper class
    private class TestProduct
    {
        public string StokKod { get; set; } = string.Empty;
        public string UrunAdi { get; set; } = string.Empty;
        public decimal Fiyat { get; set; }
        public int StokMiktar { get; set; }
    }
}
