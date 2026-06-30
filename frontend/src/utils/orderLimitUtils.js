/**

 * Backend'den gelen orderLimits veya ürün alanlarından miktar kuralı üretir.

 * Storefront'ta tek doğruluk kaynağı olarak kullanılır.

 */

import getProductCategoryRules from "../config/productCategoryRules";

import { getOrderLimitSettings } from "../services/orderLimitSettingsService";

import { isStrictVariableWeightProduct } from "./weightBasedProduct";



export const DEFAULT_PIECE_RULE = {

  unit: "adet",

  min_quantity: 1,

  max_quantity: 5,

  step: 1,

};



export const DEFAULT_KG_RULE = {

  unit: "kg",

  min_quantity: 0.25,

  max_quantity: 10,

  step: 0.25,

};



const toNumber = (value, fallback) => {

  const parsed = parseFloat(String(value ?? "").replace(",", "."));

  return Number.isFinite(parsed) ? parsed : fallback;

};



const mapOrderLimitsDto = (limits) => ({

  unit:

    limits.unit ||

    limits.Unit ||

    (limits.isWeightBased || limits.IsWeightBased ? "kg" : "adet"),

  min_quantity: toNumber(

    limits.minQuantity ?? limits.MinQuantity,

    limits.isWeightBased || limits.IsWeightBased ? DEFAULT_KG_RULE.min_quantity : 1,

  ),

  max_quantity: toNumber(

    limits.maxQuantity ?? limits.MaxQuantity,

    limits.isWeightBased || limits.IsWeightBased

      ? DEFAULT_KG_RULE.max_quantity

      : DEFAULT_PIECE_RULE.max_quantity,

  ),

  step: toNumber(

    limits.step ?? limits.Step,

    limits.isWeightBased || limits.IsWeightBased

      ? DEFAULT_KG_RULE.step

      : DEFAULT_PIECE_RULE.step,

  ),

});



const buildWeightRule = (product, globalSettings = null) => {

  const defaults = globalSettings || DEFAULT_KG_RULE;

  const maxKg =

    product.maxOrderWeight > 0

      ? product.maxOrderWeight / 1000

      : toNumber(defaults.defaultMaxWeightKg, DEFAULT_KG_RULE.max_quantity);

  const minKg =

    product.minOrderWeight > 0

      ? product.minOrderWeight / 1000

      : toNumber(defaults.defaultMinWeightKg, DEFAULT_KG_RULE.min_quantity);

  const step = toNumber(defaults.defaultWeightStepKg, DEFAULT_KG_RULE.step);



  return {

    unit: "kg",

    min_quantity: minKg,

    max_quantity: maxKg,

    step,

  };

};



export const resolveProductOrderRule = (

  product,

  selectedVariant = null,

  globalSettings = null,

) => {

  if (!product) return { ...DEFAULT_PIECE_RULE };



  const limits = product.orderLimits || product.OrderLimits;

  const isWeightProduct = isStrictVariableWeightProduct(product);



  // Backend orderLimits — kg üründe yanlışlıkla adet (max 5) gelirse kg kuralına düş
  if (limits) {
    const mapped = mapOrderLimitsDto(limits);
    const backendSaysKg =
      mapped.unit === "kg" || limits.isWeightBased || limits.IsWeightBased;

    if (isWeightProduct) {
      if (backendSaysKg) {
        return { ...mapped, unit: "kg" };
      }
      return buildWeightRule(product, globalSettings);
    }

    return mapped;
  }



  if (isWeightProduct) {

    return buildWeightRule(product, globalSettings);

  }



  const variantMax =

    selectedVariant?.maxOrderQuantity ?? selectedVariant?.MaxOrderQuantity;

  const productMax = product.maxOrderQuantity ?? product.MaxOrderQuantity;

  const maxQty =

    variantMax > 0

      ? variantMax

      : productMax > 0

        ? productMax

        : toNumber(

            globalSettings?.defaultMaxQuantityPiece,

            DEFAULT_PIECE_RULE.max_quantity,

          );



  const minQty =

    (product.minOrderQuantity ?? product.MinOrderQuantity) > 0

      ? product.minOrderQuantity ?? product.MinOrderQuantity

      : toNumber(

          globalSettings?.defaultMinQuantityPiece,

          DEFAULT_PIECE_RULE.min_quantity,

        );



  const step =

    (product.quantityStep ?? product.QuantityStep) > 0

      ? product.quantityStep ?? product.QuantityStep

      : toNumber(

          globalSettings?.defaultQuantityStepPiece,

          DEFAULT_PIECE_RULE.step,

        );



  return {

    unit: "adet",

    min_quantity: minQty,

    max_quantity: maxQty,

    step,

  };

};



export const clampQuantityToRule = (quantity, rule) => {

  const step = rule?.step ?? 1;

  const min = rule?.min_quantity ?? step;

  const max = rule?.max_quantity ?? 99;

  const parsed = parseFloat(String(quantity).replace(",", "."));

  if (!Number.isFinite(parsed)) return min;

  return Math.max(min, Math.min(max, parsed));

};



const hasExplicitBackendLimits = (product) => {

  if (!product) return false;



  if (isStrictVariableWeightProduct(product)) {

    return (

      Boolean(product.orderLimits || product.OrderLimits) ||

      Number(product.maxOrderWeight ?? product.MaxOrderWeight) > 0 ||

      Number(product.minOrderWeight ?? product.MinOrderWeight) > 0 ||

      product.isWeightBased === true ||

      product.IsWeightBased === true

    );

  }



  return (

    Boolean(product.orderLimits || product.OrderLimits) ||

    Number(product.maxOrderQuantity ?? product.MaxOrderQuantity) > 0 ||

    Number(product.minOrderQuantity ?? product.MinOrderQuantity) > 0 ||

    Number(product.quantityStep ?? product.QuantityStep) > 0

  );

};



const parseCategoryRule = (match) => {

  if (!match) return null;

  return {

    unit: match.unit || "adet",

    min_quantity: toNumber(match.min_quantity, 1),

    max_quantity: toNumber(match.max_quantity, 5),

    step: toNumber(match.step, match.unit === "kg" ? 0.25 : 1),

    category: match.category,

  };

};



const findCategoryRuleMatch = (product, rules) => {

  const pname = (product?.name || "").toLowerCase();

  const pcat = (product?.categoryName || product?.category || "").toLowerCase();



  let match = (rules || []).find((r) => {

    const examples = (r.examples || []).map((e) => String(e).toLowerCase());

    return (

      (r.category || "").toLowerCase().includes(pname) ||

      examples.some((ex) => pname.includes(ex) || ex.includes(pname))

    );

  });



  if (

    !match &&

    (pcat.includes("meyve") ||

      pcat.includes("sebze") ||

      pcat.includes("et") ||

      pcat.includes("tavuk") ||

      pcat.includes("balık") ||

      pcat.includes("balik") ||

      pcat.includes("manav"))

  ) {

    match = (rules || []).find((r) => (r.unit || "").toLowerCase() === "kg");

  }



  const unitLimitCats = [

    "süt",

    "süt ürünleri",

    "süt urunleri",

    "temel gıda",

    "temel gida",

    "temizlik",

    "içecek",

    "icecek",

    "atıştırmalık",

    "atistirmalik",

  ];



  if (!match && unitLimitCats.some((tok) => pcat.includes(tok))) {

    match = {

      category: "Kategori adedi sınırı",

      unit: "adet",

      min_quantity: 1,

      max_quantity: 5,

      step: 1,

    };

  }



  return parseCategoryRule(match);

};



export const resolveProductOrderRuleWithFallback = async (

  product,

  selectedVariant = null,

) => {

  const globalSettings = await getOrderLimitSettings();



  if (hasExplicitBackendLimits(product)) {

    return resolveProductOrderRule(product, selectedVariant, globalSettings);

  }



  if (isStrictVariableWeightProduct(product)) {

    return resolveProductOrderRule(product, selectedVariant, globalSettings);

  }



  try {

    const rules = await getProductCategoryRules();

    const categoryRule = findCategoryRuleMatch(product, rules);

    if (categoryRule) {

      return categoryRule;

    }

  } catch (error) {

    console.warn("[OrderLimit] Kategori kuralları yüklenemedi:", error);

  }



  return resolveProductOrderRule(product, selectedVariant, globalSettings);

};


