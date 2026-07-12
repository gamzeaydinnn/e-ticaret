using ECommerce.Core.Helpers;
using Xunit;

namespace ECommerce.Tests.Helpers
{
    public class ProductSearchMatcherTests
    {
        [Theory]
        [InlineData("Erikli Su 0.5L", "su")]
        [InlineData("Hayat Su 5 Lt", "su")]
        [InlineData("SU", "su")]
        [InlineData("Pınar Su", "su")]
        [InlineData("Erikli Doğal Kaynak Suyu 1.5L", "su")]
        [InlineData("Hayat İçme Suyu 5L", "su")]
        [InlineData("Beypazarı Maden Suyu", "su")]
        public void Matches_WaterProducts_ReturnsTrue(string name, string query)
        {
            Assert.True(ProductSearchMatcher.Matches(name, null, "İçecek", null, query));
            Assert.True(ProductSearchMatcher.Score(name, null, "İçecek", null, query) > 0);
        }

        [Theory]
        [InlineData("Elma Golden", "el")]
        [InlineData("Elma", "elm")]
        [InlineData("Granny Smith Elma", "elma")]
        [InlineData("Peynir Beyaz", "pey")]
        [InlineData("Domates Kokteyl", "dom")]
        public void Matches_PrefixQuery_FindsIntendedProduct(string name, string query)
        {
            Assert.True(ProductSearchMatcher.Matches(name, null, null, null, query));
        }

        [Fact]
        public void Score_PrefixEl_RanksElmaHigh()
        {
            var elma = ProductSearchMatcher.Score("Elma Golden 1kg", null, "Meyve", null, "el");
            var elektronik = ProductSearchMatcher.Score("Elektronik Tartı", null, null, null, "el");

            Assert.True(elma > 0);
            Assert.True(elma >= elektronik);
        }

        [Fact]
        public void Score_ExactWaterRanksAbovePrefixNoise()
        {
            var water = ProductSearchMatcher.Score("Erikli Su 1.5L", null, "İçecek", null, "su");
            var sausage = ProductSearchMatcher.Score("Sucuk Kangal", null, null, null, "su");

            Assert.True(water > 0);
            Assert.True(sausage > 0); // önek ile gelebilir
            Assert.True(water > sausage);
        }

        [Theory]
        [InlineData("Uludağ Gazoz Soda 330ml", "soda")]
        [InlineData("Soda", "soda")]
        public void Matches_SodaQuery_ReturnsTrue(string name, string query)
        {
            Assert.True(ProductSearchMatcher.Matches(name, null, null, null, query));
        }

        [Fact]
        public void Matches_SodaQuery_DoesNotMatchMadenSuyu()
        {
            Assert.False(ProductSearchMatcher.Matches("Beypazarı Maden Suyu", null, null, null, "soda"));
        }

        [Fact]
        public void Score_DrinkingWaterRanksAboveSunscreen()
        {
            var water = ProductSearchMatcher.Score(
                "Erikli Doğal Kaynak Suyu 1.5L", null, "İçecek", null, "su");
            var sunscreen = ProductSearchMatcher.Score(
                "NIVEA SU KORUMA FERAHLIK 30 F 200 ML", null, "Temizlik", null, "su");

            Assert.True(water > 0);
            Assert.True(water > sunscreen);
        }

        [Fact]
        public void Matches_TurkishChars_Normalized()
        {
            Assert.True(ProductSearchMatcher.Matches("Şeker", null, null, null, "seker"));
            Assert.True(ProductSearchMatcher.Matches("Çilek Reçeli", null, null, null, "recel"));
        }

        [Fact]
        public void Matches_MultiWord_RequiresAllTokens()
        {
            Assert.True(ProductSearchMatcher.Matches("Erikli Su 5L", null, null, null, "erikli su"));
            Assert.False(ProductSearchMatcher.Matches("Hayat Meyve", null, null, null, "erikli su"));
        }

        [Fact]
        public void Score_ExactNameRanksHigherThanSubstring()
        {
            var exact = ProductSearchMatcher.Score("Soda", null, null, null, "soda");
            var contained = ProductSearchMatcher.Score("Uludağ Gazoz Soda 330ml", null, null, null, "soda");

            Assert.True(exact > contained);
        }
    }
}
