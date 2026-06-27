/**
 * weightPricing.test.js — Kg/ağırlık bazlı fiyat yardımcıları için regresyon testleri.
 *
 * NEDEN: Sepette "250 gr ekleyip +'ya basınca 0.50 yerine 1.25 oluyor" ve küsüratlı
 * ürünlerde yanlış hesap sorunlarının kökü, miktar adımlama/normalizasyon ve birim fiyat
 * seçimi mantığıydı. Bu testler bu saf fonksiyonların doğruluğunu garanti altına alır.
 */
import {
  normalizeWeightStepQuantity,
  getEffectiveUnitPrice,
  isResolvedWeightBasedProduct,
} from "./weightPricing";

describe("normalizeWeightStepQuantity", () => {
  test("0.25 kg değeri korunur (minimum)", () => {
    expect(normalizeWeightStepQuantity(0.25)).toBeCloseTo(0.25, 5);
  });

  test("0.25 + 0.25 = 0.50 (artış adımı doğru)", () => {
    // Sepette '+' tıklamasının ürettiği yeni miktar: 0.25 + 0.25.
    expect(normalizeWeightStepQuantity(0.25 + 0.25)).toBeCloseTo(0.5, 5);
  });

  test("küsüratlı 1.25 kg değeri 0.25 adımına oturur ve korunur", () => {
    expect(normalizeWeightStepQuantity(1.25)).toBeCloseTo(1.25, 5);
  });

  test("0.25 altı değerler minimuma yuvarlanır", () => {
    expect(normalizeWeightStepQuantity(0.1)).toBeCloseTo(0.25, 5);
  });

  test("0, negatif veya geçersiz değerlerde minimum döner", () => {
    expect(normalizeWeightStepQuantity(0)).toBeCloseTo(0.25, 5);
    expect(normalizeWeightStepQuantity(-3)).toBeCloseTo(0.25, 5);
    expect(normalizeWeightStepQuantity("abc")).toBeCloseTo(0.25, 5);
  });

  test("0.25'in katı olmayan değerler en yakın adıma yuvarlanır", () => {
    // 0.60 → en yakın 0.25 katı = 0.50
    expect(normalizeWeightStepQuantity(0.6)).toBeCloseTo(0.5, 5);
  });
});

describe("getEffectiveUnitPrice", () => {
  test("kg ürünlerde pricePerUnit (TL/kg) önceliklidir", () => {
    const product = { isWeightBased: true, pricePerUnit: 120, price: 50 };
    expect(getEffectiveUnitPrice(null, product)).toBe(120);
  });

  test("adet ürünlerde özel fiyat/normal fiyat kullanılır", () => {
    const product = { price: 50 };
    expect(getEffectiveUnitPrice(null, product)).toBe(50);
  });

  test("kalem üzerindeki pricePerUnit, üründekinden önce gelir", () => {
    const item = { isWeightBased: true, pricePerUnit: 99 };
    const product = { isWeightBased: true, pricePerUnit: 120 };
    expect(getEffectiveUnitPrice(item, product)).toBe(99);
  });
});

describe("isResolvedWeightBasedProduct", () => {
  test("açık isWeightBased bayrağı kg ürünü tanımlar", () => {
    expect(isResolvedWeightBasedProduct({ isWeightBased: true }, {})).toBe(true);
  });

  test("weightUnit 'Kilogram' kg ürünü tanımlar", () => {
    expect(isResolvedWeightBasedProduct({ weightUnit: "Kilogram" }, {})).toBe(true);
  });

  test("adet ürün (bayrak yok, KG yok) false döner", () => {
    const product = { name: "KALEM", weightUnit: "Piece", categoryName: "KIRTASIYE" };
    expect(isResolvedWeightBasedProduct({}, product)).toBe(false);
  });
});
