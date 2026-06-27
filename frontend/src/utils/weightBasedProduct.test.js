/**
 * weightBasedProduct.test.js — Frontend kg tespiti için regresyon testleri.
 *
 * NEDEN: Bu testler, frontend `isStrictVariableWeightProduct` fonksiyonunun backend'deki
 * tek doğruluk kaynağı `WeightBasedProductResolver` ile AYNI kararları verdiğini garanti eder.
 * İki taraf ayrışırsa sepette görünen tutar ile 3DS tutarı yeniden uyuşmaz hale gelir.
 * Vakalar, backend `WeightBasedProductResolverTests` ile bilinçli olarak paraleldir.
 */
import {
  isStrictVariableWeightProduct,
  toWeightBasedProductCandidate,
} from "./weightBasedProduct";

describe("isStrictVariableWeightProduct", () => {
  test("null ürün false döner", () => {
    expect(isStrictVariableWeightProduct(null)).toBe(false);
  });

  test("açık isWeightBased=true, isimde KG yoksa bile kg kabul eder", () => {
    expect(
      isStrictVariableWeightProduct({ name: "DOMATES", isWeightBased: true }),
    ).toBe(true);
  });

  test("WeightUnit=Kilogram (string), isimde KG yoksa bile kg", () => {
    expect(
      isStrictVariableWeightProduct({ name: "DOMATES", weightUnit: "Kilogram" }),
    ).toBe(true);
  });

  test("WeightUnit sayısal (2=Kilogram) kg kabul eder", () => {
    expect(
      isStrictVariableWeightProduct({ name: "DOMATES", weightUnit: 2 }),
    ).toBe(true);
  });

  test("WeightUnit=Gram kg kabul eder", () => {
    expect(
      isStrictVariableWeightProduct({ name: "KURUYEMİŞ", weightUnit: "Gram" }),
    ).toBe(true);
  });

  test("paketli isim sinyali ('500 GR') yapısal birimi ezer", () => {
    expect(
      isStrictVariableWeightProduct({ name: "ZEYTİN 500 GR", weightUnit: "Gram" }),
    ).toBe(false);
  });

  test("açık bayrak paketli isim sinyalini ezer", () => {
    expect(
      isStrictVariableWeightProduct({
        name: "ZEYTİN 500 GR",
        isWeightBased: true,
        weightUnit: "Gram",
      }),
    ).toBe(true);
  });

  test("PricePerUnit>0 ve birim adet değilse kg", () => {
    expect(
      isStrictVariableWeightProduct({
        name: "PEYNİR",
        weightUnit: "Kilogram",
        pricePerUnit: 120,
      }),
    ).toBe(true);
  });

  test("heuristik: isimde KG + uygun kategori kg", () => {
    expect(
      isStrictVariableWeightProduct({
        name: "DOMATES KG",
        weightUnit: "Piece",
        categoryName: "MANAV",
      }),
    ).toBe(true);
  });

  test("adet ürün (sinyal yok) false", () => {
    expect(
      isStrictVariableWeightProduct({
        name: "KALEM",
        weightUnit: "Piece",
        categoryName: "KIRTASIYE",
      }),
    ).toBe(false);
  });

  test("serbest metin unit 'ADET' ise false", () => {
    expect(
      isStrictVariableWeightProduct({ name: "YUMURTA", unit: "ADET" }),
    ).toBe(false);
  });
});

describe("toWeightBasedProductCandidate", () => {
  test("yapısal sinyalleri (pricePerUnit/isWeightBased/weightUnit) korur", () => {
    const candidate = toWeightBasedProductCandidate(
      { quantity: 0.25 },
      {
        name: "DOMATES",
        weightUnit: "Kilogram",
        pricePerUnit: 25,
        isWeightBased: true,
        categoryName: "MANAV",
      },
    );

    expect(candidate.pricePerUnit).toBe(25);
    expect(candidate.isWeightBased).toBe(true);
    expect(candidate.weightUnit).toBe("Kilogram");
    // Aday üzerinden tespit yine kg olmalı.
    expect(isStrictVariableWeightProduct(candidate)).toBe(true);
  });
});
