import {
  isStrictVariableWeightProduct,
  toWeightBasedProductCandidate,
} from "./weightBasedProduct";

const toFiniteNumber = (value) => {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
};

const pickPositiveNumber = (...values) => {
  for (const value of values) {
    const parsed = toFiniteNumber(value);
    if (parsed !== null && parsed > 0) {
      return parsed;
    }
  }

  return 0;
};

export const isResolvedWeightBasedProduct = (item, product) => {
  const mergedProduct = product || item?.product || {};
  const explicitWeightBased =
    item?.isWeightBased === true ||
    item?.IsWeightBased === true ||
    mergedProduct?.isWeightBased === true ||
    mergedProduct?.IsWeightBased === true;

  if (explicitWeightBased) {
    return true;
  }

  const weightUnit =
    item?.weightUnit ??
    item?.WeightUnit ??
    mergedProduct?.weightUnit ??
    mergedProduct?.WeightUnit ??
    null;

  if (weightUnit === "Kilogram" || weightUnit === 2) {
    return true;
  }

  return isStrictVariableWeightProduct(
    toWeightBasedProductCandidate(item, mergedProduct),
  );
};

export const getEffectiveUnitPrice = (item, product) => {
  const mergedProduct = product || item?.product || {};
  const isWeightBased = isResolvedWeightBasedProduct(item, mergedProduct);
  const pricePerUnit = pickPositiveNumber(
    item?.pricePerUnit,
    item?.PricePerUnit,
    mergedProduct?.pricePerUnit,
    mergedProduct?.PricePerUnit,
  );

  if (isWeightBased && pricePerUnit > 0) {
    return pricePerUnit;
  }

  return pickPositiveNumber(
    item?.unitPrice,
    item?.UnitPrice,
    mergedProduct?.specialPrice,
    mergedProduct?.SpecialPrice,
    mergedProduct?.discountedPrice,
    mergedProduct?.DiscountedPrice,
    mergedProduct?.price,
    mergedProduct?.Price,
  );
};

export const normalizeWeightStepQuantity = (quantity, minimum = 0.25) => {
  const parsed = Number(quantity);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    return minimum;
  }

  return Math.max(
    minimum,
    Math.round(parsed / 0.25) * 0.25,
  );
};
