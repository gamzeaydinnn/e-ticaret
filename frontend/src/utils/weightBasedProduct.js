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
  "KURUYEMIS",
  "KURUYEMİŞ",
  "KURU YEMIS",
  "KURU YEMİŞ",
  "BAKLIYAT",
  "BAKLİYAT",
  "BAHARAT",
];

/** Açık tartımlı ürün adları (paketli "250 GR" hariç) — backend ile aynı */
const VARIABLE_WEIGHT_NAME_HINTS = [
  "BADEM",
  "FINDIK",
  "FINDİK",
  "FISTIK",
  "FISTİK",
  "FISTIG",
  "FISTIĞ",
  "CEVIZ",
  "CEVİZ",
  "KAJU",
  "ANTEP",
  "LEBLEBI",
  "LEBLEBİ",
  "KABAK CEKIRDEK",
  "KABAK ÇEKİRDEK",
  "AY CEKIRDEK",
  "AY ÇEKİRDEK",
  "CEKIRDEK",
  "ÇEKİRDEK",
  "KURU UZUM",
  "KURU ÜZÜM",
  "KURU INCIR",
  "KURU İNCİR",
  "KURU KAYISI",
  "HURMA",
  "KIRMIZI PUL",
  "TOZ BIBER",
  "TOZ BİBER",
  "KIMYON",
  "KİMYON",
  "SUMAC",
  "SUMAK",
  "NANE",
  "REZEN",
  "NOHUT",
  "MERCIMEK",
  "MERCİMEK",
  "FASULYE",
  "BÖRÜLCE",
  "BULGUR",
  "PIRINC",
  "PİRİNÇ",
  "IRMIK",
  "İRMİK",
];

function normalizeForHintMatch(value) {
  return String(value || "")
    .trim()
    .toLocaleUpperCase("tr-TR")
    .replace(/İ/g, "I")
    .replace(/Ş/g, "S")
    .replace(/Ğ/g, "G")
    .replace(/Ü/g, "U")
    .replace(/Ö/g, "O")
    .replace(/Ç/g, "C");
}

// Kütle birimi mi? Backend WeightUnit enum'una karşılık gelir (Gram=1, Kilogram=2).
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
 * Backend `WeightBasedProductResolver` ile aynı öncelik sırası.
 */
export function isStrictVariableWeightProduct(product) {
  if (!product) return false;

  if (product.isWeightBased === true || product.IsWeightBased === true) {
    return true;
  }

  const productName = String(
    product.name || product.Name || product.productName || "",
  ).trim();

  // Paketli ürün negatif sinyali (isimde "500 GR" / "ADET")
  if (productName) {
    if (FIXED_WEIGHT_PATTERN.test(productName)) return false;
    if (/\bADET\b/i.test(productName)) return false;
  }

  const weightUnit = product.weightUnit;
  const unit = String(product.unit || "").trim().toUpperCase();

  // Kütle birimi (kg/gram) tanımlıysa kg.
  if (isMassWeightUnit(weightUnit, unit)) return true;

  // Birim fiyat (TL/kg) tanımlı ve birim AÇIKÇA adet değilse kg.
  const pricePerUnit = Number(product.pricePerUnit ?? product.PricePerUnit ?? 0);
  const hasKnownWeightUnit = weightUnit != null && weightUnit !== "";
  if (
    pricePerUnit > 0 &&
    hasKnownWeightUnit &&
    !isPieceWeightUnit(weightUnit, unit)
  ) {
    return true;
  }

  const categoryName = normalizeForHintMatch(
    product.categoryName ||
      product.CategoryName ||
      product.category?.name ||
      product.Category?.Name ||
      "",
  );
  const normalizedName = normalizeForHintMatch(productName);

  // Taze / kuruyemiş / bakliyat kategorileri
  if (
    VARIABLE_WEIGHT_CATEGORY_HINTS.some((hint) =>
      categoryName.includes(normalizeForHintMatch(hint)),
    )
  ) {
    return true;
  }

  // Badem, fındık vb. — Mikro birimi ADET olsa bile açık tartım
  // (serbest metin unit=ADET heuristiği engellemez; backend ile aynı)
  if (
    VARIABLE_WEIGHT_NAME_HINTS.some((hint) =>
      normalizedName.includes(normalizeForHintMatch(hint)),
    )
  ) {
    return true;
  }

  // Son çare: isimde "KG" + kategori ipucu
  if (!productName) return false;
  if (!STANDALONE_KG_PATTERN.test(productName)) return false;

  return VARIABLE_WEIGHT_CATEGORY_HINTS.some((hint) =>
    categoryName.includes(normalizeForHintMatch(hint)),
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
