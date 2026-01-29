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
  getOrders: async (status = null) => {
    // NEDEN: Backend sadece /orders endpoint'ini expose ediyor.
    let endpoint = `${STORE_BASE}/orders`;

    if (status) {
      endpoint += `?status=${encodeURIComponent(status)}`;
    }

    return fetchWithAuth(endpoint);
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
  // New/Pending → Confirmed → Preparing → Ready
  // =========================================================================
  updateOrderStatus: async (orderId, newStatus, weightInGrams = null) => {
    // NEDEN: Backend'de genel status endpoint'i yok; durum geçişleri ayrı aksiyonlar.
    const normalized = (newStatus || "").toLowerCase();
    const hasWeight = weightInGrams !== null && weightInGrams !== undefined;

    // Yeni/Beklemede → Onaylandı
    if (normalized === "confirmed") {
      return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/confirm`, {
        method: "POST",
      });
    }

    if (normalized === "preparing") {
      return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/start-preparing`, {
        method: "POST",
      });
    }

    if (normalized === "ready") {
      return fetchWithAuth(`${STORE_BASE}/orders/${orderId}/mark-ready`, {
        method: "POST",
        body: JSON.stringify(hasWeight ? { weightInGrams } : {}),
      });
    }

    return {
      success: false,
      error: "Bu durum geçişi Store Attendant panelinde desteklenmiyor.",
    };
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
