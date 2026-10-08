using Xunit;
using ECommerce.Infrastructure.Services.MicroServices;
using System.Text.Json;
using ECommerce.Core.DTOs.Micro;

namespace ECommerce.Tests.Services
{
    public class MikroParserTests
    {
        [Theory]
        [InlineData("123,45", 123.45)]
        [InlineData("123.45", 123.45)]
        [InlineData("1.234,56", 1234.56)]
        [InlineData("1,234.56", 1234.56)]
        [InlineData("0", 0)]
        [InlineData("", 0)]
        [InlineData(null, 0)]
        [InlineData("abc", 0)]
        public void ParseDecimalFlexible_ShouldHandleVariousFormats(string? input, decimal expected)
        {
            // Act
            var result = MicroService.ParseDecimalFlexible(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("1", true)]
        [InlineData("true", true)]
        [InlineData("evet", true)]
        [InlineData("yes", true)]
        [InlineData("0", false)]
        [InlineData("false", false)]
        [InlineData("hayir", false)]
        [InlineData("no", false)]
        [InlineData("", null)]
        [InlineData(null, null)]
        [InlineData("invalid", null)]
        public void ParseBoolFlexible_ShouldHandleVariousFormats(string? input, bool? expected)
        {
            // Act
            var result = MicroService.ParseBoolFlexible(input);

            // Assert
            Assert.Equal(expected, result);
        }
        
        [Fact]
        public void MikroStokKaydetRequestDto_FiyatNo_ShouldDefaultTo_11()
        {
            // Act
            var dto = new MikroFiyatDegisikligiRequestDto();
            var stokFiyat = new MikroStokFiyatDto();

            // Assert
            Assert.Equal(11, dto.FiyatNo);
            Assert.Equal(11, stokFiyat.SfiyatNo);
        }
    }
}
