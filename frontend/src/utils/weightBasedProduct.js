const FIXED_WEIGHT_PATTERN = /\b\d+(?:[.,]\d+)?\s*(GR|KG|LT|ML|CL|L)\b/i;
const STANDALONE_KG_PATTERN = /\bKG\b/i;
const VARIABLE_WEIGHT_CATEGORY_HINTS = [
  "MANAV",
  "MEYVE",
  "SEBZE",
  "YESILLIK",
  "YEŞİLLİK",
  "OT",
  "KASAP",
  "ET",
  "TAVUK",
  "BALIK",
  "DENIZ",
  "DENİZ",
  "PEYNIR",
  "PEYNİR",
  "SARKUTERI",
  "ŞARKÜTERİ",
  "ZEYTIN",
  "ZEYTİN",
];
// Kütle birimi mi? Backend WeightUnit enum'una karşılık gelir (Gram=1, Kilogram=2).
// Frontend'e bu değer string ("Kilogram"/"Gram") veya sayısal (1/2) gelebilir; ayrıca
// bazı ürünlerde yalnız serbest metin `unit` ("KG") bulunur.
function isMassWeightUnit(weightUnit, unit) {
  if (
    weightUnit === "Kilogram" ||
    weightUnit === 2 ||
    weightUnit === "Gram" ||
    weightUnit === 1
  ) {
    return true;
  }
  return String(unit || "").trim().toUpperCase() === "KG";
}

// Birim açıkça "adet" mi? (WeightUnit.Piece = 0)
function isPieceWeightUnit(weightUnit, unit) {
  if (weightUnit === "Piece" || weightUnit === 0) return true;
  return String(unit || "").trim().toUpperCase() === "ADET";
}

/**
 * Bir ürünün değişken ağırlıklı (kg) olup olmadığını belirler.
 *
 * ÖNEMLİ: Bu fonksiyon backend'deki tek doğruluk kaynağı `WeightBasedProductResolver`
 * ile BİREBİR aynı öncelik sırasını uygular. Böylece sepet (frontend) ile sipariş/3DS
 * (backend) aynı ürünü aynı şekilde sınıflandırır; tutar uyuşmazlığı önlenir.
 *
 * Öncelik: 1) açık `isWeightBased` bayrağı → kg, 2) paketli isim sinyali ("500 GR"/"ADET") → adet,
 * 3) kütle birimi (kg/gram) → kg, 4) PricePerUnit>0 ve birim adet değilse → kg,
 * 5) son çare heuristik: isimde "KG" + uygun kategori → kg, 6) aksi halde adet.
 */
export function isStrictVariableWeightProduct(product) {
  if (!product) return false;

  // 1) Backend'in açık bayrağı en yüksek güven kaynağıdır (tek doğruluk kaynağı).
  if (product.isWeightBased === true || product.IsWeightBased === true) {
    return true;
  }

  const productName = String(
    product.name || product.Name || product.productName || "",
  ).trim();

  // 2) Paketli ürün negatif sinyali yapısal tahminleri ezer.
  if (productName) {
    if (FIXED_WEIGHT_PATTERN.test(productName)) return false;
    if (/\bADET\b/i.test(productName)) return false;
  }

  const weightUnit = product.weightUnit;
  const unit = String(product.unit || "").trim().toUpperCase();

  // Serbest metin birim açıkça "ADET" ise değişken ağırlıklı değildir.
  if (unit === "ADET") return false;

  // 3) Kütle birimi (kg/gram) tanımlıysa kg.
  if (isMassWeightUnit(weightUnit, unit)) return true;

  // 4) Birim fiyat (TL/kg) tanımlı ve birim AÇIKÇA adet değilse kg.
  //    weightUnit bilinmiyorsa yanlış pozitiften kaçınmak için bu adımı atlarız.
  const pricePerUnit = Number(product.pricePerUnit ?? product.PricePerUnit ?? 0);
  const hasKnownWeightUnit =
    weightUnit != null && weightUnit !== "";
  if (
    pricePerUnit > 0 &&
    hasKnownWeightUnit &&
    !isPieceWeightUnit(weightUnit, unit)
  ) {
    return true;
  }

  // 5) Yapısal veri yetersiz: isim+kategori heuristiğine düş (son çare).
  if (!productName) return false;
  if (!STANDALONE_KG_PATTERN.test(productName)) return false;

  const categoryName = String(
    product.categoryName || product.category?.name || "",
  ).trim().toUpperCase();

  return VARIABLE_WEIGHT_CATEGORY_HINTS.some((hint) =>
    categoryName.includes(hint),
  );
}

export function toWeightBasedProductCandidate(item, product) {
  return {
    ...(product || {}),
    ...(item?.product || {}),
    name:
      product?.name ||
      product?.Name ||
      item?.productName ||
      item?.product?.name ||
      "",
    categoryName:
      product?.categoryName ||
      product?.category?.name ||
      item?.categoryName ||
      item?.product?.categoryName ||
      item?.product?.category?.name ||
      "",
    unit: product?.unit || item?.product?.unit || "",
    weightUnit:
      item?.weightUnit ?? product?.weightUnit ?? item?.product?.weightUnit ?? null,
    // Yapısal sinyaller resolver ile aynı kararı verebilmek için açıkça taşınır.
    pricePerUnit:
      item?.pricePerUnit ??
      product?.pricePerUnit ??
      product?.PricePerUnit ??
      item?.product?.pricePerUnit ??
      0,
    isWeightBased:
      item?.isWeightBased ??
      product?.isWeightBased ??
      product?.IsWeightBased ??
      item?.product?.isWeightBased ??
      undefined,
  };
}
