using ECommerce.Business.Helpers;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Xunit;

namespace ECommerce.Tests.Weight
{
    /// <summary>
    /// <see cref="WeightBasedProductResolver"/> için regresyon testleri.
    ///
    /// NEDEN: Bu çözümleyici, "kg ürün mü?" kararını dört katmanda tekilleştirir. Aşağıdaki testler
    /// özellikle ESKİ <see cref="WeightBasedProductRules.IsVariableWeightKgProduct"/> kuralının yanlış
    /// sonuç ürettiği (ve sepet ≠ 3DS tutar farkına yol açan) senaryoları sabitler:
    /// - IsWeightBased=true ama isimde "KG" yok → eski kural FALSE derdi; doğrusu TRUE.
    /// - WeightUnit=Kilogram ama isimde "KG" yok → eski kural FALSE derdi; doğrusu TRUE.
    /// </summary>
    public class WeightBasedProductResolverTests
    {
        private static Product MakeProduct(
            string name = "",
            bool isWeightBased = false,
            WeightUnit weightUnit = WeightUnit.Piece,
            decimal pricePerUnit = 0m,
            decimal price = 0m,
            decimal? specialPrice = null,
            string? categoryName = null)
        {
            return new Product
            {
                Name = name,
                IsWeightBased = isWeightBased,
                WeightUnit = weightUnit,
                PricePerUnit = pricePerUnit,
                Price = price,
                SpecialPrice = specialPrice,
                Category = categoryName is null ? null! : new Category { Name = categoryName }
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ResolveIsWeightBased — tespit kararı
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public void Null_urun_false_doner()
        {
            Assert.False(WeightBasedProductResolver.ResolveIsWeightBased(null));
        }

        [Fact]
        public void AcikBayrak_IsWeightBasedTrue_isimdeKgYoksaBileKgKabulEder()
        {
            // Kök neden senaryosu: eski heuristik isimde "KG" arıyordu ve FALSE dönüyordu.
            var product = MakeProduct(name: "DOMATES", isWeightBased: true, weightUnit: WeightUnit.Piece);
            Assert.True(WeightBasedProductResolver.ResolveIsWeightBased(product));
        }

        [Fact]
        public void WeightUnitKilogram_isimdeKgYoksaBileKgKabulEder()
        {
            // İkinci kök neden senaryosu: WeightUnit=Kilogram ama isim "DOMATES".
            var product = MakeProduct(name: "DOMATES", weightUnit: WeightUnit.Kilogram);
            Assert.True(WeightBasedProductResolver.ResolveIsWeightBased(product));
        }

        [Fact]
        public void WeightUnitGram_kgKabulEder()
        {
            var product = MakeProduct(name: "KARIŞIK KURUYEMİŞ", weightUnit: WeightUnit.Gram);
            Assert.True(WeightBasedProductResolver.ResolveIsWeightBased(product));
        }

        [Fact]
        public void PaketliIsimSinyali_yapisalTahminiEzer()
        {
            // WeightUnit=Gram olsa bile "500 GR" paketli sinyali ürünü adet yapar.
            var product = MakeProduct(name: "ZEYTİN 500 GR", weightUnit: WeightUnit.Gram);
            Assert.False(WeightBasedProductResolver.ResolveIsWeightBased(product));
        }

        [Fact]
        public void AcikBayrak_paketliIsimSinyaliniEzer()
        {
            // Admin AÇIKÇA IsWeightBased=true demişse "500 GR" ismine rağmen kg kabul edilir.
            var product = MakeProduct(name: "ZEYTİN 500 GR", isWeightBased: true, weightUnit: WeightUnit.Gram);
            Assert.True(WeightBasedProductResolver.ResolveIsWeightBased(product));
        }

        [Fact]
        public void PricePerUnitTanimli_birimAdetDegilse_kgKabulEder()
        {
            var product = MakeProduct(name: "PEYNİR", weightUnit: WeightUnit.Kilogram, pricePerUnit: 120m);
            Assert.True(WeightBasedProductResolver.ResolveIsWeightBased(product));
        }

        [Fact]
        public void Heuristik_isimKgArtiKategori_kgKabulEder()
        {
            // Yapısal veri yok (bayrak false, Piece, PricePerUnit 0) → isim+kategori heuristiği.
            var product = MakeProduct(name: "DOMATES KG", weightUnit: WeightUnit.Piece, categoryName: "MANAV");
            Assert.True(WeightBasedProductResolver.ResolveIsWeightBased(product));
        }

        [Fact]
        public void AdetUrun_hicbirSinyalYok_false()
        {
            var product = MakeProduct(name: "KALEM", weightUnit: WeightUnit.Piece, categoryName: "KIRTASIYE");
            Assert.False(WeightBasedProductResolver.ResolveIsWeightBased(product));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ResolveUnitPrice — birim fiyat seçimi
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public void UnitPrice_kgUrun_PricePerUnitOnceliklidir()
        {
            var product = MakeProduct(pricePerUnit: 120m, price: 50m, specialPrice: 40m);
            Assert.Equal(120m, WeightBasedProductResolver.ResolveUnitPrice(product, isWeightBased: true));
        }

        [Fact]
        public void UnitPrice_kgUrun_PricePerUnitYoksaIndirimliFiyat()
        {
            var product = MakeProduct(pricePerUnit: 0m, price: 50m, specialPrice: 40m);
            Assert.Equal(40m, WeightBasedProductResolver.ResolveUnitPrice(product, isWeightBased: true));
        }

        [Fact]
        public void UnitPrice_kgUrun_PricePerUnitVeIndirimYoksaNormalFiyat()
        {
            var product = MakeProduct(pricePerUnit: 0m, price: 50m, specialPrice: null);
            Assert.Equal(50m, WeightBasedProductResolver.ResolveUnitPrice(product, isWeightBased: true));
        }

        [Fact]
        public void UnitPrice_adetUrun_indirimliFiyatOnceliklidir()
        {
            var product = MakeProduct(price: 50m, specialPrice: 30m);
            Assert.Equal(30m, WeightBasedProductResolver.ResolveUnitPrice(product, isWeightBased: false));
        }

        [Fact]
        public void UnitPrice_nullUrun_sifirDoner()
        {
            Assert.Equal(0m, WeightBasedProductResolver.ResolveUnitPrice(null, isWeightBased: true));
        }

        [Fact]
        public void Resolve_tespitVeFiyatiTutarliDondurur()
        {
            var product = MakeProduct(name: "DOMATES", weightUnit: WeightUnit.Kilogram, pricePerUnit: 25m, price: 99m);
            var (isWeightBased, unitPrice) = WeightBasedProductResolver.Resolve(product);

            Assert.True(isWeightBased);
            Assert.Equal(25m, unitPrice);
        }
    }
}
