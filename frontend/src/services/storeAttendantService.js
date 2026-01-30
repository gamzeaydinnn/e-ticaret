// ==========================================================================
// storeAttendantService.js - Market Görevlisi API Service
// ==========================================================================
// Store Attendant paneli için tüm API çağrılarını yönetir.
// Sipariş listesi, durum güncelleme, tartı girişi işlemleri.
// ==========================================================================

// API base URL
const API_ROOT = (process.env.REACT_APP_API_URL || "").replace(/\/$/, "");
const API_BASE = `${API_ROOT}/api`;
// NEDEN: Backend route'ları /api/StoreAttendantOrder/... ile başlar.
const STORE_BASE = "/StoreAttendantOrder";

// ============================================================================
// TOKEN YARDIMCI
// ============================================================================
const getToken = () => {
  return (
    localStorage.getItem("storeAttendantToken") ||
    sessionStorage.getItem("storeAttendantToken") ||
    localStorage.getItem("token")
  ); // Fallback to general token
};

// ============================================================================
// FETCH WRAPPER
// ============================================================================
const fetchWithAuth = async (endpoint, options = {}) => {
  const token = getToken();

  const config = {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  };

  try {
    const response = await fetch(`${API_BASE}${endpoint}`, config);

    // 401 - Unauthorized
    if (response.status === 401) {
      console.warn("[StoreAttendantService] Unauthorized - session expired");
      // Token'ı temizle
      localStorage.removeItem("storeAttendantToken");
      sessionStorage.removeItem("storeAttendantToken");
      // Login sayfasına yönlendir
      window.location.href = "/store/login";
      return { success: false, error: "Oturum süresi doldu" };
    }

    // 403 - Forbidden
    if (response.status === 403) {
      return { success: false, error: "Bu işlem için yetkiniz yok" };
    }

    // JSON parse
    const data = await response.json();

    if (response.ok) {
      return { success: true, data };
    } else {
      return {
        success: false,
        error: data.message || data.error || "Bir hata oluştu",
      };
    }
  } catch (error) {
    console.error("[StoreAttendantService] API hatası:", error);
    return {
      success: false,
      error: "Bağlantı hatası. Lütfen tekrar deneyin.",
    };
  }
};

// ============================================================================
// API FONKSİYONLARI
// ============================================================================
const storeAttendantService = {
  // =========================================================================
  // SİPARİŞLERİ GETİR
  // Status: Confirmed, Preparing, Ready
  // =========================================================================
  getOrders: async (status = null, page = 1, pageSize = 20) => {
    // NEDEN: Backend sadece /orders endpoint'ini expose ediyor.
    const params = new URLSearchParams();
    params.set("page", String(page));
    params.set("pageSize", String(pageSize));
    if (status) {
      params.set("status", status);
    }

    return fetchWithAuth(`${STORE_BASE}/orders?${params.toString()}`);
  },

  // =========================================================================
  // TEK SİPARİŞ DETAYI
  // =========================================================================
  getOrderById: async () => {
    // NEDEN: StoreAttendantOrder için tekil GET endpoint'i backend'de yok.
    return {
      success: false,
      error: "Tekil sipariş detayı bu panel için desteklenmiyor.",
    };
  },

  // =========================================================================
  // ÖZET BİLGİLERİ
  // Confirmed, Preparing, Ready sayıları
  // =========================================================================
  getSummary: async () => {
    return fetchWithAuth(`${STORE_BASE}/summary`);
  },

  // =========================================================================
  // SİPARİŞ DURUMU GÜNCELLE
  // Tüm statü geçişleri için merkezi fonksiyon (Admin ile aynı yetkiler)
  // =========================================================================
  updateOrderStatus: async (orderId, newStatus, weightInGrams = null) => {
    // NEDEN: Her durum geçişi için ayrı endpoint kullanılıyor (state machine)
    const normalized = (newStatus || "").toLowerCase();
    const hasWeight = weightInGrams !== null && weightInGrams !== undefined;

    // Yeni/Beklemede → Onaylandı
    if (normalized === "confirmed") {
      return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/confirm`, {
        method: "POST",
      });
    }

    // Onaylandı → Hazırlanıyor
    if (normalized === "preparing") {
      return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/start-preparing`, {
        method: "POST",
      });
    }

    // Hazırlanıyor → Hazır
    if (normalized === "ready") {
      return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/mark-ready`, {
        method: "POST",
        body: JSON.stringify(hasWeight ? { weightInGrams } : {}),
      });
    }

    // Atandı/Hazır → Dağıtımda (YENİ: Admin ile aynı yetki)
    if (normalized === "out_for_delivery" || normalized === "outfordelivery") {
      return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/out-for-delivery`, {
        method: "POST",
      });
    }

    // Dağıtımda → Teslim Edildi (YENİ: Admin ile aynı yetki)
    if (normalized === "delivered") {
      return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/deliver`, {
        method: "POST",
      });
    }

    // Herhangi → İptal Edildi (YENİ: Admin ile aynı yetki)
    if (normalized === "cancelled" || normalized === "canceled") {
      return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/cancel`, {
        method: "POST",
      });
    }

    // İade işlemi (YENİ: Admin ile aynı yetki)
    if (normalized === "refunded") {
      return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/refund`, {
        method: "POST",
      });
    }

    // Genel durum güncelleme (fallback - PUT endpoint)
    return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/status`, {
      method: "PUT",
      body: JSON.stringify({ status: newStatus }),
    });
  },

  // =========================================================================
  // KURYE ATA (YENİ: Admin ile aynı yetki)
  // Ready/Assigned durumundaki siparişlere kurye atama
  // =========================================================================
  assignCourier: async (orderId, courierId) => {
    return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/assign-courier`, {
      method: "POST",
      body: JSON.stringify({ courierId }),
    });
  },

  // =========================================================================
  // SİPARİŞ DETAYI (YENİ: Admin ile aynı yetki)
  // =========================================================================
  getOrderDetail: async (orderId) => {
    return fetchWithAuth(`${STORE_BASE}/orders/${orderId}`);
  },

  // =========================================================================
  // SİPARİŞ ONAYLA (New/Pending → Confirmed)
  // =========================================================================
  confirmOrder: async (orderId) => {
    return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/confirm`, {
      method: "POST",
    });
  },

  // =========================================================================
  // HAZIRLAMA BAŞLAT (Confirmed → Preparing)
  // =========================================================================
  startPreparing: async (orderId) => {
    return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/start-preparing`, {
      method: "POST",
    });
  },

  // =========================================================================
  // HAZIR İŞARETLE (Preparing → Ready)
  // =========================================================================
  markAsReady: async (orderId, weightInGrams = null) => {
    const hasWeight = weightInGrams !== null && weightInGrams !== undefined;
    return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/mark-ready`, {
      method: "POST",
      body: JSON.stringify(hasWeight ? { weightInGrams } : {}),
    });
  },

  // =========================================================================
  // TARTI GİRİŞİ
  // =========================================================================
  submitWeight: async (orderId, weight) => {
    // NEDEN: Backend'de ayrı weight endpoint'i yok; ağırlık "mark-ready" ile gönderilir.
    return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/mark-ready`, {
      method: "POST",
      body: JSON.stringify({ weightInGrams: Number(weight) }),
    });
  },

  // =========================================================================
  // TARTI VE HAZIR İŞARETLE (Birleşik işlem)
  // =========================================================================
  submitWeightAndMarkReady: async (orderId, weight) => {
    // NEDEN: Tek çağrı ile ağırlık + hazır işaretleme sağlanır.
    return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/mark-ready`, {
      method: "POST",
      body: JSON.stringify({ weightInGrams: Number(weight) }),
    });
  },
};

export default storeAttendantService;
