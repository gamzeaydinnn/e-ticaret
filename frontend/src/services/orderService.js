import api from "./api";

const base = "/api/Orders";

const normalizeOrder = (order = {}) => {
  const itemsSource = Array.isArray(order.orderItems)
    ? order.orderItems
    : Array.isArray(order.items)
    ? order.items
    : [];

  const normalizedItems = itemsSource.map((item, index) => ({
    id: item.id ?? item.productId ?? index,
    productId: item.productId ?? item.id ?? index,
    name: item.productName || item.name || "Ürün",
    quantity: item.quantity ?? 0,
    unitPrice: item.unitPrice ?? item.price ?? 0,
    price: item.unitPrice ?? item.price ?? 0,
  }));

  const fallbackOrderNo = order.id ? `ORD-${order.id}` : "ORD-0000";

  return {
    id: order.id ?? order.orderId ?? null,
    orderNumber: order.orderNumber || fallbackOrderNo,
    trackingCode: order.trackingCode || order.orderNumber || fallbackOrderNo,
    status: (order.status || "").toLowerCase(),
    orderDate: order.orderDate || new Date().toISOString(),
    totalAmount: order.totalAmount ?? order.totalPrice ?? 0,
    totalPrice: order.totalPrice ?? order.totalAmount ?? 0,
    isGuestOrder: Boolean(order.isGuestOrder),
    customerName: order.customerName || "",
    customerPhone: order.customerPhone || "",
    deliveryAddress:
      order.deliveryAddress || order.shippingAddress || "Adres belirtilmedi",
    shippingCompany: order.shippingCompany || order.shippingMethod || "",
    estimatedDeliveryDate: order.estimatedDeliveryDate || null,
    shippingMethod: order.shippingMethod || "",
    items: normalizedItems,
    orderItems: normalizedItems,
    raw: order,
  };
};

const normalizeList = (payload) =>
  Array.isArray(payload) ? payload.map(normalizeOrder) : [];

export const OrderService = {
  create: (payload) => api.post(base, payload).then(normalizeOrder),
  list: (userId) =>
    api
      .get(base, { params: userId ? { userId } : undefined })
      .then(normalizeList),
  getById: (id) => api.get(`${base}/${id}`).then(normalizeOrder),
  updateStatus: (id, status) => api.patch(`${base}/${id}/status`, { status }),
  cancel: (id) => api.post(`${base}/${id}/cancel`),
  checkout: (payload) => api.post(`${base}/checkout`, payload),
  downloadInvoice: async (id) => {
    const blob = await api.get(`${base}/${id}/invoice`, {
      responseType: "blob",
      transformResponse: (value) => value,
    });

    const url = window.URL.createObjectURL(new Blob([blob]));
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", `invoice-${id}.pdf`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },
};
