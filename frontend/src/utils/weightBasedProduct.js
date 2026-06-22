const FIXED_WEIGHT_PATTERN = /\b\d+(?:[.,]\d+)?\s*(GR|KG|LT|ML|CL|L)\b/i;
const STANDALONE_KG_PATTERN = /\bKG\b/i;
export function isStrictVariableWeightProduct(product) {
  if (!product) return false;

  const productName = String(
    product.name || product.Name || product.productName || "",
  ).trim();
  if (!productName) return false;

  if (FIXED_WEIGHT_PATTERN.test(productName)) return false;
  if (/\bADET\b/i.test(productName)) return false;
  if (!STANDALONE_KG_PATTERN.test(productName)) return false;

  const unit = String(product.unit || "").trim().toUpperCase();
  if (unit && unit !== "KG") return false;

  const weightUnit = product.weightUnit;
  if (weightUnit != null && weightUnit !== "" && weightUnit !== "Kilogram" && weightUnit !== 2) {
    return false;
  }

  return true;
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
  };
}
