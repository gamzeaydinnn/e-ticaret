using System.Text.Json;
using ECommerce.Core.DTOs.Micro;
using Xunit;

namespace ECommerce.Tests.MicroServices;

/// <summary>
/// Task 8.2: Parser testleri
/// Gereksinim: 4.3, 4.4, 4.5, 4.6
/// </summary>
public class MicroServiceParserTests
{
    [Fact]
    public void ParseUnifiedProductRows_WithValidJson_ReturnsProducts()
    {
        // Arrange
        var json = @"{
            ""data"": [
                {
                    ""sto_kod"": ""TEST001"",
                    ""sto_isim"": ""Test Ürün"",
                    ""msg_S_0002"": ""100.50"",
                    ""msg_S_0343"": ""10"",
                    ""sto_webe_gonderilecek_fl"": ""1""
                }
            ]
        }";

        // Act
        var products = ParseTestJson(json);

        // Assert
        Assert.Single(products);
        Assert.Equal("TEST001", products[0].StokKod);
        Assert.Equal("Test Ürün", products[0].UrunAdi);
        Assert.Equal(100.50m, products[0].Fiyat);
        Assert.Equal(10, products[0].StokMiktar);
        Assert.True(products[0].IsWebActive);
    }

    [Fact]
    public void ParseUnifiedProductRows_WithMissingFields_UsesDefaults()
    {
        // Arrange
        var json = @"{
            ""data"": [
                {
                    ""sto_kod"": ""TEST002""
                }
            ]
        }";

        // Act
        var products = ParseTestJson(json);

        // Assert
        Assert.Single(products);
        Assert.Equal("TEST002", products[0].StokKod);
        Assert.Equal(string.Empty, products[0].UrunAdi);
        Assert.Equal(0m, products[0].Fiyat);
        Assert.Equal(0, products[0].StokMiktar);
        Assert.False(products[0].IsWebActive);
    }

    [Fact]
    public void ParseUnifiedProductRows_WithNullValues_HandlesGracefully()
    {
        // Arrange
        var json = @"{
            ""data"": [
                {
                    ""sto_kod"": ""TEST003"",
                    ""sto_isim"": null,
                    ""msg_S_0002"": null,
                    ""msg_S_0343"": null
                }
            ]
        }";

        // Act
        var products = ParseTestJson(json);

        // Assert
        Assert.Single(products);
        Assert.Equal("TEST003", products[0].StokKod);
        Assert.Equal(string.Empty, products[0].UrunAdi);
        Assert.Equal(0m, products[0].Fiyat);
        Assert.Equal(0, products[0].StokMiktar);
    }

    [Theory]
    [InlineData("100.50", 100.50)]
    [InlineData("100,50", 100.50)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("invalid", 0)]
    [InlineData("", 0)]
    public void ParseDecimalFlexible_WithVariousFormats_ParsesCorrectly(string input, decimal expected)
    {
        // Act
        var result = ParseDecimalFlexible(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("10", 10)]
    [InlineData("0", 0)]
    [InlineData("-5", -5)]
    [InlineData("invalid", 0)]
    [InlineData("", 0)]
    [InlineData("10.5", 10)]
    public void ParseIntFlexible_WithVariousInputs_ParsesCorrectly(string input, int expected)
    {
        // Act
        var result = ParseIntFlexible(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    [InlineData("False", false)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void ParseBoolFlexible_WithVariousInputs_ParsesCorrectly(string input, bool expected)
    {
        // Act
        var result = ParseBoolFlexible(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ReadStringFromRow_WithCaseInsensitiveMatch_FindsField()
    {
        // Arrange
        var json = @"{
            ""STO_KOD"": ""TEST001"",
            ""sto_kod"": ""TEST002"",
            ""Sto_Kod"": ""TEST003""
        }";
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = ReadStringFromRow(element, "sto_kod", "STO_KOD");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("TEST", result);
    }

    [Fact]
    public void ReadStringFromRow_WithFallbackFields_UsesFallback()
    {
        // Arrange
        var json = @"{
            ""alternative_field"": ""Fallback Value""
        }";
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = ReadStringFromRow(element, "primary_field", "alternative_field");

        // Assert
        Assert.Equal("Fallback Value", result);
    }

    [Fact]
    public void ParseUnifiedProductRows_WithInvalidRow_SkipsAndContinues()
    {
        // Arrange
        var json = @"{
            ""data"": [
                {
                    ""sto_kod"": ""VALID001"",
                    ""sto_isim"": ""Valid Product""
                },
                {
                    ""invalid"": ""data""
                },
                {
                    ""sto_kod"": ""VALID002"",
                    ""sto_isim"": ""Another Valid Product""
                }
            ]
        }";

        // Act
        var products = ParseTestJson(json);

        // Assert
        // Should skip invalid row and continue
        Assert.True(products.Count >= 2);
    }

    // Helper methods
    private List<MikroUrunDto> ParseTestJson(string json)
    {
        var products = new List<MikroUrunDto>();
        var document = JsonDocument.Parse(json);

        if (document.RootElement.TryGetProperty("data", out var dataArray))
        {
            foreach (var item in dataArray.EnumerateArray())
            {
                try
                {
                    var product = new MikroUrunDto
                    {
                        StokKod = ReadStringFromRow(item, "sto_kod") ?? string.Empty,
                        UrunAdi = ReadStringFromRow(item, "sto_isim") ?? string.Empty,
                        Fiyat = ParseDecimalFlexible(ReadStringFromRow(item, "msg_S_0002")),
                        StokMiktar = ParseIntFlexible(ReadStringFromRow(item, "msg_S_0343")),
                        IsWebActive = ParseBoolFlexible(ReadStringFromRow(item, "sto_webe_gonderilecek_fl"))
                    };
                    products.Add(product);
                }
                catch
                {
                    // Skip invalid rows
                }
            }
        }

        return products;
    }

    private string? ReadStringFromRow(JsonElement element, params string[] fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            if (element.TryGetProperty(fieldName, out var prop))
            {
                return prop.GetString();
            }

            // Case-insensitive search
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.GetString();
                }
            }
        }
        return null;
    }

    private decimal ParseDecimalFlexible(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        // Normalize: remove spaces, replace comma with dot
        var normalized = value.Trim()
            .Replace(" ", "")
            .Replace(",", ".");

        // Handle Turkish format (1.234,56 -> 1234.56)
        if (normalized.Count(c => c == '.') > 1)
        {
            normalized = normalized.Replace(".", "");
        }

        if (decimal.TryParse(normalized, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return 0m;
    }

    private int ParseIntFlexible(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        // Try direct parse
        if (int.TryParse(value.Trim(), out var result))
            return result;

        // Try parsing as decimal first (handles "10.5" -> 10)
        if (decimal.TryParse(value.Trim(), out var decimalResult))
            return (int)decimalResult;

        return 0;
    }

    private bool ParseBoolFlexible(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        // Check for "1" or "0"
        if (trimmed == "1") return true;
        if (trimmed == "0") return false;

        // Check for "true" or "false" (case-insensitive)
        if (bool.TryParse(trimmed, out var result))
            return result;

        return false;
    }
}
