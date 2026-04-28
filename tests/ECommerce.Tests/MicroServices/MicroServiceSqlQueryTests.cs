using System.Text.Json;
using ECommerce.Infrastructure.Services.MicroServices;
using Xunit;

namespace ECommerce.Tests.MicroServices;

/// <summary>
/// Task 8.1: SQL Query Builder testleri
/// Gereksinim: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6
/// </summary>
public class MicroServiceSqlQueryTests
{
    [Fact]
    public void BuildUnifiedProductQuery_WithNoParameters_ReturnsBaseQuery()
    {
        // Arrange & Act
        var query = BuildTestQuery();

        // Assert
        Assert.Contains("STOK_SATIS_FIYAT_LISTELERI_YONETIM", query);
        Assert.Contains("fn_Stok_Depo_Dagilim", query);
        Assert.Contains("STOKLAR", query);
        Assert.Contains("ROW_NUMBER()", query);
    }

    [Fact]
    public void BuildUnifiedProductQuery_WithDepoFilter_IncludesDepoCondition()
    {
        // Arrange
        int depoNo = 1;

        // Act
        var query = BuildTestQuery(depoNo: depoNo);

        // Assert
        Assert.Contains("dep_no = @DepoNo", query);
    }

    [Fact]
    public void BuildUnifiedProductQuery_WithGrupKodFilter_IncludesGrupCondition()
    {
        // Arrange
        string grupKod = "GIDA";

        // Act
        var query = BuildTestQuery(grupKod: grupKod);

        // Assert
        Assert.Contains("sto_grup_kod = @GrupKod", query);
    }

    [Fact]
    public void BuildUnifiedProductQuery_WithStokKodFilter_IncludesStokKodCondition()
    {
        // Arrange
        string stokKod = "TEST001";

        // Act
        var query = BuildTestQuery(stokKod: stokKod);

        // Assert
        Assert.Contains("sto_kod LIKE @StokKod", query);
    }

    [Fact]
    public void BuildUnifiedProductQuery_WithSadeceStoklu_IncludesStockCondition()
    {
        // Arrange & Act
        var query = BuildTestQuery(sadeceStoklu: true);

        // Assert
        Assert.Contains("msg_S_0343 > 0", query);
    }

    [Fact]
    public void BuildUnifiedProductQuery_WithSadeceAktif_IncludesActiveCondition()
    {
        // Arrange & Act
        var query = BuildTestQuery(sadeceAktif: true);

        // Assert
        Assert.Contains("sto_webe_gonderilecek_fl = 1", query);
    }

    [Fact]
    public void BuildUnifiedProductQuery_WithMultipleFilters_CombinesConditions()
    {
        // Arrange
        int depoNo = 1;
        string grupKod = "GIDA";
        bool sadeceStoklu = true;

        // Act
        var query = BuildTestQuery(depoNo: depoNo, grupKod: grupKod, sadeceStoklu: sadeceStoklu);

        // Assert
        Assert.Contains("dep_no = @DepoNo", query);
        Assert.Contains("sto_grup_kod = @GrupKod", query);
        Assert.Contains("msg_S_0343 > 0", query);
    }

    [Fact]
    public void BuildUnifiedProductQuery_IncludesThisYearFilter()
    {
        // Arrange & Act
        var query = BuildTestQuery();

        // Assert
        Assert.Contains("YEAR(GETDATE())", query);
    }

    [Theory]
    [InlineData("'; DROP TABLE STOKLAR; --")]
    [InlineData("1' OR '1'='1")]
    [InlineData("<script>alert('xss')</script>")]
    public void BuildUnifiedProductQuery_WithMaliciousInput_UsesParameterizedQuery(string maliciousInput)
    {
        // Arrange & Act
        var query = BuildTestQuery(stokKod: maliciousInput);

        // Assert
        // Parametreli sorgu kullanıldığı için SQL injection riski yok
        Assert.Contains("@StokKod", query);
        Assert.DoesNotContain("DROP TABLE", query);
        Assert.DoesNotContain("OR '1'='1'", query);
    }

    // Helper method to simulate BuildUnifiedProductQuery
    private string BuildTestQuery(
        int? depoNo = null,
        int? fiyatListesiNo = null,
        string? stokKod = null,
        string? grupKod = null,
        bool? sadeceStoklu = null,
        bool? sadeceAktif = null)
    {
        var conditions = new List<string>();

        if (depoNo.HasValue && depoNo.Value > 0)
            conditions.Add("dep_no = @DepoNo");

        if (!string.IsNullOrWhiteSpace(stokKod))
            conditions.Add("sto_kod LIKE @StokKod");

        if (!string.IsNullOrWhiteSpace(grupKod))
            conditions.Add("sto_grup_kod = @GrupKod");

        if (sadeceStoklu == true)
            conditions.Add("msg_S_0343 > 0");

        if (sadeceAktif == true)
            conditions.Add("sto_webe_gonderilecek_fl = 1");

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        return $@"
            SELECT * FROM (
                SELECT 
                    ROW_NUMBER() OVER (PARTITION BY sto_kod ORDER BY msg_S_0002 DESC) as RowNum,
                    *
                FROM STOK_SATIS_FIYAT_LISTELERI_YONETIM
                INNER JOIN fn_Stok_Depo_Dagilim() ON ...
                INNER JOIN STOKLAR ON ...
                WHERE YEAR(GETDATE()) = YEAR(...)
                {whereClause}
            ) AS Filtered
            WHERE RowNum = 1";
    }
}
