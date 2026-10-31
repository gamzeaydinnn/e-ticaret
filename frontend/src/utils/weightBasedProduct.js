const FIXED_WEIGHT_PATTERN = /\b\d+(?:[.,]\d+)?\s*(GR|G|GRAM|KG|LT|ML|CL|L)\b/i;
const STANDALONE_KG_PATTERN = /\bKG\b/i;
const PACKAGED_PIECE_NAME_PATTERN =
  /\b(ADET|AD|PAKET|PK|DEMET|BAG|BAĞ|KUTU|KAVANOZ|TENEKE|POSET|POŞET|SISE|ŞİŞE|KOLI|KOLİ|TABAK|FILE|FİLE)\b/i;
const CANNED_OR_FROZEN_NAME_PATTERN =
  /\b(KONSERVE|KONSERVESI|KONSERVESİ|DONUK|DONDURULMUS|DONDURULMUŞ|DONDURMA|TURSU|TURŞU|GARNITUR|GARNİTÜR|KOZLENMIS|KÖZLENMİŞ|HASLANMIS|HAŞLANMIŞ|SUPERFRESH|FEAST)\b/i;

/**
 * Konserve, dondurulmuş/donuk gıda, dondurma, turşu ve paketli alt kategorilerde
 * kesinlikle kg (açık tartım) satışı yoktur; bu ürünler paket/adet ile satılır.
 */
const NON_WEIGHT_CATEGORY_HINTS = [
  "KONSERVE",
  "TURSU",
  "TURŞU",
  "DONUK",
  "DONDURULMUS",
  "DONDURULMUŞ",
  "DONDURMA",
  "PAKETLI",
  "PAKETLİ",
  "PAKET KURUYEMIS",
  "PAKET KURUYEMİŞ",
];

const NON_WEIGHT_CATEGORY_CODES = new Set([
  "2203", // Konserve Meyve & Sebze
  "2204", // Donuk Meyve & Sebze
  "703", // Konserve & Turşu
  "1102", // Paket Kuruyemiş
  "1300", // Dondurulmuş Gıda (Ana Grup)
  "1301", // Dondurulmuş Balık & Deniz Ürünleri
  "1302", // Dondurulmuş Piliç Ürünleri
  "1303", // Dondurulmuş Et Ürünleri
  "1304", // Dondurulmuş Sebze & Meyve
  "1305", // Dondurulmuş Pasta & Unlu Mamuller
  "1306", // Diğer Dondurulmuş Gıda
  "800", // Dondurma (Ana Grup)
  "801", // Kap Dondurma
  "802", // Tek Dondurma
]);

const VARIABLE_WEIGHT_CATEGORY_HINTS = [
  "MANAV",
  "MEYVE",
  "SEBZE",
  "YESILLIK",
  "YEŞİLLİK",
  "KASAP",
  "KIRMIZI ET",
  "BEYAZ ET",
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
  "ACIK KURUYEMIS",
  "AÇIK KURUYEMİŞ",
  "KURUYEMIS",
  "KURUYEMİŞ",
  "KURU YEMIS",
  "KURU YEMİŞ",
  "BAKLIYAT",
  "BAKLİYAT",
  "BAHARAT",
];

/** Açık tartımlı ürün adları (paketli "250 GR", konserve ve donuk hariç) — backend ile aynı */
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

// Birim açıkça "adet" veya "paket" mi? (WeightUnit.Piece = 0)
function isPieceWeightUnit(weightUnit, unit) {
  if (weightUnit === "Piece" || weightUnit === 0) return true;
  const upperUnit = String(unit || "").trim().toUpperCase();
  return (
    upperUnit === "ADET" ||
    upperUnit === "AD" ||
    upperUnit === "PAKET" ||
    upperUnit === "PK" ||
    upperUnit === "KUTU" ||
    upperUnit === "DEMET"
  );
}

/**
 * Ürünün konserve, dondurulmuş/donuk gıda veya paketli/adetli satış kategorisinde/isminde
 * olup olmadığını kontrol eder. True dönerse ürün ASLA kg ile satılmaz.
 */
export function isExcludedFromWeightSale(product) {
  if (!product) return false;

  const productName = String(
    product.name || product.Name || product.productName || "",
  ).trim();

  if (productName) {
    if (FIXED_WEIGHT_PATTERN.test(productName)) return true;
    if (PACKAGED_PIECE_NAME_PATTERN.test(productName)) return true;
    if (CANNED_OR_FROZEN_NAME_PATTERN.test(productName)) return true;
  }

  const rawCodes = [
    product.categoryCode,
    product.CategoryCode,
    product.mikroAltGrupKod,
    product.MikroAltGrupKod,
    product.anagrupCode,
    product.AnagrupCode,
    product.mikroAnaGrupKod,
    product.MikroAnaGrupKod,
    product.category?.mikroAltGrupKod,
    product.category?.mikroAnaGrupKod,
    product.Category?.MikroAltGrupKod,
    product.Category?.MikroAnaGrupKod,
  ];

  for (const code of rawCodes) {
    const trimmed = String(code || "").trim();
    if (trimmed && NON_WEIGHT_CATEGORY_CODES.has(trimmed)) {
      return true;
    }
  }

  const categoryText = normalizeForHintMatch(
    [
      product.categoryName,
      product.CategoryName,
      product.category?.name,
      product.Category?.Name,
      typeof product.category === "string" ? product.category : "",
      product.categorySlug,
      product.CategorySlug,
      product.category?.slug,
      product.Category?.Slug,
      product.subCategoryName,
    ]
      .filter(Boolean)
      .join(" "),
  );

  if (
    categoryText &&
    NON_WEIGHT_CATEGORY_HINTS.some((hint) =>
      categoryText.includes(normalizeForHintMatch(hint)),
    )
  ) {
    return true;
  }

  return false;
}

/**
 * Bir ürünün değişken ağırlıklı (kg) olup olmadığını belirler.
 *
 * Backend `WeightBasedProductResolver` ile aynı öncelik sırası.
 */
export function isStrictVariableWeightProduct(product) {
  if (!product) return false;

  // 1) Konserve, dondurulmuş/donuk gıda ve paketli/adetli ürünler ASLA kg ile satılmaz.
  //    Veritabanında eskiden kalma isWeightBased=true olsa bile bu negatif sinyal ezer.
  if (isExcludedFromWeightSale(product)) {
    return false;
  }

  if (product.isWeightBased === true || product.IsWeightBased === true) {
    return true;
  }

  const productName = String(
    product.name || product.Name || product.productName || "",
  ).trim();

  const weightUnit = product.weightUnit ?? product.WeightUnit;
  const unit = String(product.unit || product.Unit || "")
    .trim()
    .toUpperCase();

  // Kütle birimi (kg/gram) tanımlıysa kg.
  if (isMassWeightUnit(weightUnit, unit)) return true;

  // Birim fiyat (TL/kg) tanımlı ve birim AÇIKÇA adet/paket değilse kg.
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
      (typeof product.category === "string" ? product.category : "") ||
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
    categorySlug:
      product?.categorySlug ||
      product?.category?.slug ||
      item?.categorySlug ||
      item?.product?.categorySlug ||
      item?.product?.category?.slug ||
      "",
    categoryCode:
      product?.categoryCode ||
      product?.mikroAltGrupKod ||
      item?.categoryCode ||
      item?.product?.categoryCode ||
      item?.product?.mikroAltGrupKod ||
      "",
    anagrupCode:
      product?.anagrupCode ||
      product?.mikroAnaGrupKod ||
      item?.anagrupCode ||
      item?.product?.anagrupCode ||
      item?.product?.mikroAnaGrupKod ||
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
