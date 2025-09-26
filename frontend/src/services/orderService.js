import api from "../api/client";

const base = "/api/Orders";

// Temel sipariş işlemleri
export const createOrder = (payload) =>
  api.post(base, payload).then((r) => r.data);

export const getOrders = () => api.get(base).then((r) => r.data);

export const getOrderById = (id) =>
  api.get(`${base}/${id}`).then((r) => r.data);

// Gelişmiş sipariş işlemleri
export const getUserOrders = async () => {
  try {
    const response = await api.get(`${base}/user`);
    return response.data;
  } catch (error) {
    console.error("Siparişler getirilemedi:", error);

    // Demo data - gerçek API çalışmıyorsa
    return [
      {
        id: 1,
        orderNumber: "ORD-2025-001",
        trackingCode: "TK123456789",
        status: "shipped",
        orderDate: "2025-09-25T10:00:00Z",
        totalAmount: 299.99,
        deliveryAddress: "Atatürk Cad. No:123 Kadıköy/İstanbul",
        shippingCompany: "Yurtiçi Kargo",
        estimatedDeliveryDate: "2025-09-28T18:00:00Z",
        items: [
          {
            id: 1,
            productName: "Wireless Bluetooth Kulaklık",
            quantity: 1,
            unitPrice: 149.99,
          },
          {
            id: 2,
            productName: "Telefon Kılıfı",
            quantity: 2,
            unitPrice: 75.0,
          },
        ],
      },
      {
        id: 2,
        orderNumber: "ORD-2025-002",
        trackingCode: "TK987654321",
        status: "preparing",
        orderDate: "2025-09-26T14:30:00Z",
        totalAmount: 599.99,
        deliveryAddress: "Cumhuriyet Mah. Barış Sok. No:45 Beşiktaş/İstanbul",
        shippingCompany: "MNG Kargo",
        estimatedDeliveryDate: "2025-09-30T18:00:00Z",
        items: [
          {
            id: 3,
            productName: "Gaming Mouse",
            quantity: 1,
            unitPrice: 299.99,
          },
          {
            id: 4,
            productName: "Mouse Pad",
            quantity: 1,
            unitPrice: 50.0,
          },
        ],
      },
    ];
  }
};

// Takip kodu ile sipariş ara
export const getOrderByTrackingCode = async (trackingCode) => {
  try {
    const response = await api.get(`${base}/track/${trackingCode}`);
    return response.data;
  } catch (error) {
    console.error("Sipariş takip edilemedi:", error);

    // Demo data
    const demoOrders = await getUserOrders();
    return demoOrders.find((order) => order.trackingCode === trackingCode);
  }
};

// Ödeme işlemi
export const processPayment = async (paymentData) => {
  try {
    const response = await api.post("/api/Payments/process", paymentData);
    return response.data;
  } catch (error) {
    console.error("Ödeme işlemi başarısız:", error);

    // Demo ödeme simülasyonu
    return new Promise((resolve, reject) => {
      setTimeout(() => {
        // Rastgele başarı/başarısızlük simülasyonu
        const isSuccess = Math.random() > 0.1; // %90 başarı oranı

        if (isSuccess) {
          resolve({
            success: true,
            orderNumber: "ORD-" + Date.now(),
            paymentId: "PAY-" + Date.now(),
            message: "Ödeme başarıyla tamamlandı",
          });
        } else {
          reject(
            new Error(
              "Ödeme işlemi reddedildi. Lütfen kart bilgilerinizi kontrol edin."
            )
          );
        }
      }, 2000); // 2 saniye gecikme simülasyonu
    });
  }
};

// Sipariş durumunu güncelle
export const updateOrderStatus = async (orderId, status) => {
  try {
    const response = await api.patch(`${base}/${orderId}/status`, { status });
    return response.data;
  } catch (error) {
    console.error("Sipariş durumu güncellenemedi:", error);
    throw error;
  }
};

// Sipariş iptal et
export const cancelOrder = async (orderId, reason) => {
  try {
    const response = await api.patch(`${base}/${orderId}/cancel`, { reason });
    return response.data;
  } catch (error) {
    console.error("Sipariş iptal edilemedi:", error);
    throw error;
  }
};

// Fatura indir
export const downloadInvoice = async (orderId) => {
  try {
    const response = await api.get(`${base}/${orderId}/invoice`, {
      responseType: "blob",
    });

    // Blob'u dosya olarak indir
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", `fatura-${orderId}.pdf`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);

    return true;
  } catch (error) {
    console.error("Fatura indirilemedi:", error);
    throw error;
  }
};

// Kargo takip bilgisi getir
export const getShippingInfo = async (trackingCode) => {
  try {
    const response = await api.get(`/api/Shipping/track/${trackingCode}`);
    return response.data;
  } catch (error) {
    console.error("Kargo takip bilgisi alınamadı:", error);

    // Demo kargo bilgisi
    return {
      trackingCode,
      currentStatus: "in_transit",
      estimatedDelivery: new Date(
        Date.now() + 2 * 24 * 60 * 60 * 1000
      ).toISOString(),
      trackingHistory: [
        {
          date: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
          status: "shipped",
          location: "İstanbul Depo",
          description: "Paketiniz kargo firmasına teslim edildi",
        },
        {
          date: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000).toISOString(),
          status: "in_transit",
          location: "Ankara Transfer Merkezi",
          description: "Paketiniz transfer merkezinden geçti",
        },
      ],
    };
  }
};
