using System.Text.Json;
using ECommerce.Core.DTOs.Micro;
using Xunit;

namespace ECommerce.Tests.MicroServices;

/// <summary>
/// Task 8.3: DTO Mapping testleri
/// Gereksinim: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7
/// </summary>
public class MikroUrunDtoTests
{
    [Fact]
    public void MikroUrunDto_WithAllProperties_MapsCorrectly()
    {
        // Arrange & Act
        var dto = new MikroUrunDto
        {
            StokKod = "TEST001",
            UrunAdi = "Test Ürün",
            Fiyat = 100.50m,
            StokMiktar = 10,
            DepoAdi = "Ana Depo",
            DepoNo = 1,
            IsWebActive = true,
            Birim = "Adet",
            GrupKod = "GIDA"
        };

        // Assert
        Assert.Equal("TEST001", dto.StokKod);
        Assert.Equal("Test Ürün", dto.UrunAdi);
        Assert.Equal(100.50m, dto.Fiyat);
        Assert.Equal(10, dto.StokMiktar);
        Assert.Equal("Ana Depo", dto.DepoAdi);
        Assert.Equal(1, dto.DepoNo);
        Assert.True(dto.IsWebActive);
        Assert.Equal("Adet", dto.Birim);
        Assert.Equal("GIDA", dto.GrupKod);
    }

    [Fact]
    public void MikroUrunDto_IsStokta_WhenStockGreaterThanZero_ReturnsTrue()
    {
        // Arrange
        var dto = new MikroUrunDto
        {
            StokMiktar = 5
        };

        // Act & Assert
        Assert.True(dto.IsStokta);
    }

    [Fact]
    public void MikroUrunDto_IsStokta_WhenStockZero_ReturnsFalse()
    {
        // Arrange
        var dto = new MikroUrunDto
        {
            StokMiktar = 0
        };

        // Act & Assert
        Assert.False(dto.IsStokta);
    }

    [Fact]
    public void MikroUrunDto_StokDurumu_WhenInStock_ReturnsStokta()
    {
        // Arrange
        var dto = new MikroUrunDto
        {
            StokMiktar = 10
        };

        // Act
        var durum = dto.StokDurumu;

        // Assert
        Assert.Equal("Stokta", durum);
    }

    [Fact]
    public void MikroUrunDto_StokDurumu_WhenOutOfStock_ReturnsStoktaYok()
    {
        // Arrange
        var dto = new MikroUrunDto
        {
            StokMiktar = 0
        };

        // Act
        var durum = dto.StokDurumu;

        // Assert
        Assert.Equal("Stokta Yok", durum);
    }

    [Fact]
    public void MikroUrunDto_FiyatFormatli_FormatsCorrectly()
    {
        // Arrange
        var dto = new MikroUrunDto
        {
            Fiyat = 1234.56m
        };

        // Act
        var formatted = dto.FiyatFormatli;

        // Assert
        Assert.Contains("1", formatted);
        Assert.Contains("234", formatted);
        Assert.Contains("56", formatted);
        Assert.Contains("₺", formatted);
    }

    [Fact]
    public void MikroUrunDto_FiyatFormatli_WithZeroPrice_FormatsCorrectly()
    {
        // Arrange
        var dto = new MikroUrunDto
        {
            Fiyat = 0m
        };

        // Act
        var formatted = dto.FiyatFormatli;

        // Assert
        Assert.Contains("0", formatted);
        Assert.Contains("₺", formatted);
    }

    [Fact]
    public void MikroUrunDto_JsonSerialization_WithJsonPropertyNames_SerializesCorrectly()
    {
        // Arrange
        var dto = new MikroUrunDto
        {
            StokKod = "TEST001",
            UrunAdi = "Test Ürün",
            Fiyat = 100.50m,
            StokMiktar = 10
        };

        // Act
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert
        Assert.Contains("stokKod", json);
        Assert.Contains("TEST001", json);
        Assert.Contains("urunAdi", json);
        Assert.Contains("Test Ürün", json);
    }

    [Fact]
    public void MikroUrunDto_JsonDeserialization_WithJsonPropertyNames_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{
            ""stokKod"": ""TEST001"",
            ""urunAdi"": ""Test Ürün"",
            ""fiyat"": 100.50,
            ""stokMiktar"": 10,
            ""isWebActive"": true
        }";

        // Act
        var dto = JsonSerializer.Deserialize<MikroUrunDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("TEST001", dto.StokKod);
        Assert.Equal("Test Ürün", dto.UrunAdi);
        Assert.Equal(100.50m, dto.Fiyat);
        Assert.Equal(10, dto.StokMiktar);
        Assert.True(dto.IsWebActive);
    }

    [Fact]
    public void MikroUrunDto_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var dto = new MikroUrunDto();

        // Assert
        Assert.Equal(string.Empty, dto.StokKod);
        Assert.Equal(string.Empty, dto.UrunAdi);
        Assert.Equal(0m, dto.Fiyat);
        Assert.Equal(0, dto.StokMiktar);
        Assert.Equal(string.Empty, dto.DepoAdi);
        Assert.Equal(0, dto.DepoNo);
        Assert.False(dto.IsWebActive);
        Assert.Equal(string.Empty, dto.Birim);
        Assert.Equal(string.Empty, dto.GrupKod);
    }

    [Theory]
    [InlineData(0, false, "Stokta Yok")]
    [InlineData(1, true, "Stokta")]
    [InlineData(10, true, "Stokta")]
    [InlineData(-1, false, "Stokta Yok")]
    public void MikroUrunDto_StockProperties_WorkCorrectly(int stokMiktar, bool expectedIsStokta, string expectedDurum)
    {
        // Arrange
        var dto = new MikroUrunDto
        {
            StokMiktar = stokMiktar
        };

        // Act & Assert
        Assert.Equal(expectedIsStokta, dto.IsStokta);
        Assert.Equal(expectedDurum, dto.StokDurumu);
    }

    [Fact]
    public void MikroUrunDto_WithTurkishCharacters_HandlesCorrectly()
    {
        // Arrange
        var dto = new MikroUrunDto
        {
            StokKod = "ÜRN001",
            UrunAdi = "Çikolata Şeker İçeceği",
            DepoAdi = "İstanbul Deposu",
            GrupKod = "GIDA"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserialized = JsonSerializer.Deserialize<MikroUrunDto>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("ÜRN001", deserialized.StokKod);
        Assert.Equal("Çikolata Şeker İçeceği", deserialized.UrunAdi);
        Assert.Equal("İstanbul Deposu", deserialized.DepoAdi);
    }
}
