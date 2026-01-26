// ==========================================================================
// storeAttendantService.js - Market Görevlisi API Service
// ==========================================================================
// Store Attendant paneli için tüm API çağrılarını yönetir.
// Sipariş listesi, durum güncelleme, tartı girişi işlemleri.
// ==========================================================================

// API base URL
const API_BASE = process.env.REACT_APP_API_URL || "http://localhost:5002/api";

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
    let endpoint = "/StoreAttendantOrder";

    if (status) {
      endpoint += `?status=${status}`;
    }

    return fetchWithAuth(endpoint);
  },

  // =========================================================================
  // TEK SİPARİŞ DETAYI
  // =========================================================================
  getOrderById: async (orderId) => {
    return fetchWithAuth(`/StoreAttendantOrder/${orderId}`);
  },

  // =========================================================================
  // ÖZET BİLGİLERİ
  // Confirmed, Preparing, Ready sayıları
  // =========================================================================
  getSummary: async () => {
    return fetchWithAuth("/StoreAttendantOrder/summary");
  },

  // =========================================================================
  // SİPARİŞ DURUMU GÜNCELLE
  // Confirmed → Preparing → Ready
  // =========================================================================
  updateOrderStatus: async (orderId, newStatus) => {
    return fetchWithAuth(`/StoreAttendantOrder/${orderId}/status`, {
      method: "PUT",
      body: JSON.stringify({ status: newStatus }),
    });
  },

  // =========================================================================
  // HAZIRLAMA BAŞLAT (Confirmed → Preparing)
  // =========================================================================
  startPreparing: async (orderId) => {
    return fetchWithAuth(`/StoreAttendantOrder/${orderId}/start-preparing`, {
      method: "PUT",
    });
  },

  // =========================================================================
  // HAZIR İŞARETLE (Preparing → Ready)
  // =========================================================================
  markAsReady: async (orderId) => {
    return fetchWithAuth(`/StoreAttendantOrder/${orderId}/mark-ready`, {
      method: "PUT",
    });
  },

  // =========================================================================
  // TARTI GİRİŞİ
  // =========================================================================
  submitWeight: async (orderId, weight) => {
    return fetchWithAuth(`/StoreAttendantOrder/${orderId}/weight`, {
      method: "PUT",
      body: JSON.stringify({ weight }),
    });
  },

  // =========================================================================
  // TARTI VE HAZIR İŞARETLE (Birleşik işlem)
  // =========================================================================
  submitWeightAndMarkReady: async (orderId, weight) => {
    return fetchWithAuth(`/StoreAttendantOrder/${orderId}/weight-and-ready`, {
      method: "PUT",
      body: JSON.stringify({ weight }),
    });
  },
};

export default storeAttendantService;
