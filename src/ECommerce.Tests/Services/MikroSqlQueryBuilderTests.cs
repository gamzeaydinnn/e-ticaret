using Xunit;
using ECommerce.Infrastructure.Services.MicroServices;

namespace ECommerce.Tests.Services
{
    public class MikroSqlQueryBuilderTests
    {
        [Fact]
        public void BuildUnifiedProductQuery_ShouldInclude_Liste11_Filter()
        {
            // Act
            var result = MikroSqlQueryBuilder.BuildUnifiedProductQuery();

            // Assert
            Assert.Contains("Y.msg_S_1265 = 11", result.Query);
        }

        [Fact]
        public void BuildUnifiedProductQuery_WithoutDepo_ShouldUse_DefaultDepoFunc()
        {
            // Act
            var result = MikroSqlQueryBuilder.BuildUnifiedProductQuery(depoNo: null);

            // Assert
            Assert.Contains("fn_Stok_Depo_Dagilim('', 0)", result.Query);
        }

        [Fact]
        public void BuildUnifiedProductQuery_WithDepo_ShouldUse_SpecificDepoFunc()
        {
            // Act
            var result = MikroSqlQueryBuilder.BuildUnifiedProductQuery(depoNo: 5);

            // Assert
            Assert.Contains("fn_Stok_Depo_Dagilim('', 5)", result.Query);
        }

        [Fact]
        public void BuildUnifiedProductQuery_WithGrupKod_ShouldInclude_GrupKodFilter()
        {
            // Act
            var result = MikroSqlQueryBuilder.BuildUnifiedProductQuery(grupKod: "TESTGRUP");

            // Assert
            Assert.Contains("S.sto_grup_kod = 'TESTGRUP'", result.Query);
        }

        [Fact]
        public void BuildUnifiedProductQuery_WithStokKod_ShouldEscapeInput_AndIncludeFilter()
        {
            // Act
            var result = MikroSqlQueryBuilder.BuildUnifiedProductQuery(stokKod: "TEST'KOD");

            // Assert
            // ' karakteri '' olarak escape edilmeli
            Assert.Contains("Y.msg_S_0001 LIKE '%TEST''KOD%'", result.Query);
        }

        [Fact]
        public void BuildSqlStockQuery_ShouldInclude_Liste11_Filter()
        {
            // Act
            var result = MikroSqlQueryBuilder.BuildSqlStockQuery(null);

            // Assert
            Assert.Contains("Y.msg_S_1265 = 11", result.Query);
            Assert.Contains("D.msg_S_0343", result.Query);
        }

        [Fact]
        public void BuildSqlPriceQuery_ShouldInclude_Liste11_Filter()
        {
            // Act
            var result = MikroSqlQueryBuilder.BuildSqlPriceQuery(null);

            // Assert
            Assert.Contains("Y.msg_S_1265 = 11", result.Query);
            Assert.Contains("Y.msg_S_0002", result.Query);
        }
    }
}
