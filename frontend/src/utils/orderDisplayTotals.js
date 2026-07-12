/**
 * Müşteri sipariş ekranlarında tutar gösterimi — kalem toplamları ile
 * sipariş genel toplamının aynı kaynaktan türemesi için.
 */

export function getOrderItemsList(order) {
  if (Array.isArray(order?.items) && order.items.length > 0) {
    return order.items;
  }
  if (Array.isArray(order?.orderItems) && order.orderItems.length > 0) {
    return order.orderItems;
  }
  if (Array.isArray(order?.raw?.orderItems) && order.raw.orderItems.length > 0) {
    return order.raw.orderItems;
  }
  if (Array.isArray(order?.raw?.items) && order.raw.items.length > 0) {
    return order.raw.items;
  }
  return [];
}

/** Tek kalem satır tutarı (tartı sonrası ActualPrice / API LineTotal öncelikli) */
export function getOrderItemLineTotal(item) {
  if (!item) return 0;

  const lineTotal = Number(item.lineTotal ?? item.LineTotal ?? 0);
  if (lineTotal > 0) return lineTotal;

  const actual = Number(item.actualPrice ?? item.ActualPrice ?? 0);
  if (actual > 0) return actual;

  const estimated = Number(item.estimatedPrice ?? item.EstimatedPrice ?? 0);
  if (estimated > 0) return estimated;

  const qty = Number(item.quantity ?? 0);
  const unit = Number(item.unitPrice ?? item.price ?? item.UnitPrice ?? 0);
  return Math.round(qty * unit * 100) / 100;
}

export function isWeightBasedOrderItem(item) {
  return Boolean(
    item?.isWeightBased ??
      item?.IsWeightBased ??
      item?.weightBased ??
      (item?.actualWeight ?? item?.ActualWeight) > 0,
  );
}

/** Kalem birim fiyat etiketi (kg/adet tartılı ürünler için) */
export function getOrderItemUnitLabel(item) {
  const qty = Number(item?.quantity ?? 0);
  const unit = Number(item?.unitPrice ?? item?.price ?? 0);
  const isWeight = isWeightBasedOrderItem(item);

  if (isWeight) {
    const grams = Number(item?.actualWeight ?? item?.ActualWeight ?? 0);
    const estGrams = Number(item?.estimatedWeight ?? item?.EstimatedWeight ?? 0);
    const weightGrams = grams > 0 ? grams : estGrams > 0 ? estGrams : qty * 1000;
    const kg = (weightGrams / 1000).toFixed(2);
    return `${kg} kg × ₺${unit.toFixed(2)}/kg`;
  }

  return `${qty} adet × ₺${unit.toFixed(2)}`;
}

export function getOrderDisplayTotals(order) {
  const items = getOrderItemsList(order);
  const itemsSubtotal = items.reduce(
    (sum, item) => sum + getOrderItemLineTotal(item),
    0,
  );

  const shippingCost = Number(
    order?.shippingCost ?? order?.raw?.shippingCost ?? 0,
  );
  const discountAmount = Number(order?.discountAmount ?? 0);
  const couponDiscount = Number(
    order?.couponDiscountAmount ?? order?.raw?.couponDiscountAmount ?? 0,
  );
  const campaignDiscount = Number(
    order?.campaignDiscountAmount ?? order?.raw?.campaignDiscountAmount ?? 0,
  );
  const totalDiscount = discountAmount + couponDiscount + campaignDiscount;

  const weightAdjustment = Number(
    order?.totalPriceDifference ?? order?.raw?.totalPriceDifference ?? 0,
  );

  const computedTotal = Math.max(
    0,
    Math.round((itemsSubtotal + shippingCost - totalDiscount) * 100) / 100,
  );

  const backendTotal = Number(
    order?.finalAmount ??
      order?.raw?.finalAmount ??
      order?.finalPrice ??
      order?.raw?.finalPrice ??
      order?.totalAmount ??
      order?.totalPrice ??
      0,
  );

  // Backend toplamı ile kalem+kargo uyumluysa onu kullan; değilse hesaplananı göster
  const total =
    backendTotal > 0 &&
    Math.abs(backendTotal - computedTotal) <= 0.02
      ? backendTotal
      : computedTotal > 0
        ? computedTotal
        : backendTotal;

  return {
    itemsSubtotal: Math.round(itemsSubtotal * 100) / 100,
    shippingCost,
    totalDiscount: Math.round(totalDiscount * 100) / 100,
    weightAdjustment,
    total: Math.round(total * 100) / 100,
    hasShipping: shippingCost > 0,
    hasDiscount: totalDiscount > 0,
    hasWeightAdjustment: Math.abs(weightAdjustment) > 0.009,
  };
}

export function formatTry(amount) {
  return `₺${Number(amount || 0).toFixed(2)}`;
}
