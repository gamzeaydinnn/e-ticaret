using ECommerce.Business.Helpers;
using ECommerce.Core.DTOs.Order;
using ECommerce.Entities.Enums;
using Xunit;

namespace ECommerce.Tests.Weight
{
    /// <summary>
    /// Kg/ağırlık bazlı ürünlerin fiyat ve tespit mantığına dair regresyon testleri.
    ///
    /// NEDEN: Sepet ile 3DS arasındaki tutar tutarsızlığının ana kaynağı, "kg ürün mü?"
    /// tespitinin ve kg satır toplamının tek/doğru biçimde hesaplanmamasıydı. Bu testler:
    /// 1. Tespit kuralının (WeightBasedProductRules) kenar durumlarda doğru karar verdiğini,
    /// 2. Kg ürünlerde satır toplamının "Quantity × UnitPrice" yerine EstimatedPrice/ActualPrice
    ///    üzerinden hesaplandığını (Faz 1 düzeltmesi) güvence altına alır.
    /// </summary>
    public class WeightBasedPricingTests
    {
        // ─────────────────────────────────────────────────────────────────────────
        // TESPİT KURALI (IsVariableWeightKgProduct) — kenar durumlar
        // ─────────────────────────────────────────────────────────────────────────
        [Theory]
        // İsimde düz "KG" + WeightUnit.Kilogram → değişken ağırlıklı ürün.
        [InlineData("DOMATES KG", WeightUnit.Kilogram, "MANAV", true)]
        // İsimde düz "KG" + Piece ama kategori ipucu (MANAV) → değişken ağırlıklı.
        [InlineData("DOMATES KG", WeightUnit.Piece, "MANAV", true)]
        // Sabit gramajlı paketli ürün ("1 LT") → ASLA değişken ağırlıklı değil.
        [InlineData("SÜT 1 LT", WeightUnit.Piece, "İÇECEK", false)]
        // İsimde "ADET" geçen ürün → adet bazlı, değişken ağırlıklı değil.
        [InlineData("YUMURTA 30 ADET", WeightUnit.Piece, "MANAV", false)]
        // Ne "KG" ne de uygun kategori → değişken ağırlıklı değil.
        [InlineData("KALEM", WeightUnit.Piece, "KIRTASIYE", false)]
        public void IsVariableWeightKgProduct_KenarDurumlariDogruSiniflandirir(
            string name, WeightUnit unit, string category, bool expected)
        {
            var result = WeightBasedProductRules.IsVariableWeightKgProduct(name, unit, category);
            Assert.Equal(expected, result);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // SENKRON POPÜLASYONU (DeriveIsWeightBasedForSync)
        // ─────────────────────────────────────────────────────────────────────────
        [Theory]
        // Birim KG ama isimde "KG" yok → eski popülasyon FALSE derdi; doğrusu TRUE (asıl düzeltme).
        [InlineData("DOMATES", WeightUnit.Kilogram, "MANAV", true)]
        // Birim Gram → tartılı.
        [InlineData("KURUYEMİŞ", WeightUnit.Gram, "BAKLİYAT", true)]
        // Paketli sinyal birim KG olsa bile adet yapar.
        [InlineData("ZEYTİN 500 GR", WeightUnit.Kilogram, "ŞARKÜTERİ", false)]
        // Yapısal birim yok ama isim+kategori heuristiği → tartılı.
        [InlineData("DOMATES KG", WeightUnit.Piece, "MANAV", true)]
        // Hiçbir sinyal yok → adet.
        [InlineData("KALEM", WeightUnit.Piece, "KIRTASIYE", false)]
        public void DeriveIsWeightBasedForSync_KaynakVerisindenDogruTuretir(
            string name, WeightUnit unit, string category, bool expected)
        {
            var result = WeightBasedProductRules.DeriveIsWeightBasedForSync(name, unit, category);
            Assert.Equal(expected, result);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // KG SATIR TOPLAMI (OrderItemDto.TotalPrice) — Faz 1 düzeltmesi
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public void TotalPrice_KgUrun_EstimatedPriceUzerindenHesaplanir()
        {
            // 250 gr (0.25 kg) × 120 TL/kg = 30 TL. Quantity=1 olmasına rağmen
            // satır toplamı "1 × 120" değil, 30 TL olmalı.
            var dto = new OrderItemDto
            {
                IsWeightBased = true,
                Quantity = 1,
                UnitPrice = 120m,
                EstimatedWeight = 250m, // gram
                EstimatedPrice = 30m
            };

            Assert.Equal(30m, dto.TotalPrice);
        }

        [Fact]
        public void TotalPrice_KgUrun_ActualPriceVarsaOnceliklidir()
        {
            // Tartı sonrası gerçek fiyat (ActualPrice) tahmini fiyatın önüne geçer.
            var dto = new OrderItemDto
            {
                IsWeightBased = true,
                Quantity = 1,
                UnitPrice = 120m,
                EstimatedWeight = 250m,
                EstimatedPrice = 30m,
                ActualPrice = 36m
            };

            Assert.Equal(36m, dto.TotalPrice);
        }

        [Fact]
        public void TotalPrice_AdetUrun_KlasikCarpimKullanir()
        {
            // Adet bazlı ürün: 3 × 15 = 45 TL (kg mantığına düşmemeli).
            var dto = new OrderItemDto
            {
                IsWeightBased = false,
                Quantity = 3,
                UnitPrice = 15m
            };

            Assert.Equal(45m, dto.TotalPrice);
        }

        [Fact]
        public void DisplayDetail_KgUrun_KgFormatindaUretir()
        {
            var dto = new OrderItemDto
            {
                IsWeightBased = true,
                Quantity = 1,
                UnitPrice = 120m,
                EstimatedWeight = 250m,
                EstimatedPrice = 30m
            };

            // Kg ürünlerde detay satırı "... kg x .../kg = ..." biçiminde olmalı.
            Assert.Contains("kg", dto.DisplayDetail);
        }
    }
}
