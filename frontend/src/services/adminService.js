import {
  debugLog,
  isBackendAvailable,
  shouldUseMockData,
} from "../config/apiConfig";
import api from "./api";
import categoryServiceReal from "./categoryServiceReal";
import productServiceMock from "./productServiceMock";

// Kategoriler: Gerçek Backend API
// Ürünler: JSON Server (geçici - Mikro API gelene kadar)

let mockCoupons = [
  {
    id: 1,
    code: "WELCOME10",
    isPercentage: true,
    value: 10,
    expirationDate: "2024-12-31T00:00:00Z",
    minOrderAmount: 200,
    usageLimit: 500,
    isActive: true,
  },
  {
    id: 2,
    code: "KARGO50",
    isPercentage: false,
    value: 50,
    expirationDate: "2024-11-15T00:00:00Z",
    minOrderAmount: 300,
    usageLimit: 100,
    isActive: true,
  },
  {
    id: 3,
    code: "VIP20",
    isPercentage: true,
    value: 20,
    expirationDate: "2024-10-30T00:00:00Z",
    minOrderAmount: 500,
    usageLimit: 50,
    isActive: false,
  },
];

let mockCampaigns = [
  {
    id: 1,
    name: "Sepette %10 İndirim",
    description: "200 TL üzeri alışverişte geçerli.",
    startDate: "2024-10-01T00:00:00Z",
    endDate: "2024-10-31T23:59:59Z",
    isActive: true,
    conditionJson: '{"minSubtotal":200}',
    rewardType: "Percent",
    rewardValue: 10,
  },
  {
    id: 2,
    name: "150 TL İade",
    description: "1000 TL üzeri alışverişlerde 150 TL indirim",
    startDate: "2024-11-01T00:00:00Z",
    endDate: "2024-11-30T23:59:59Z",
    isActive: false,
    conditionJson: '{"minSubtotal":1000}',
    rewardType: "Amount",
    rewardValue: 150,
  },
  {
    id: 3,
    name: "Ücretsiz Kargo Haftası",
    description: "Kasım ayı ilk haftasında tüm siparişlere ücretsiz kargo",
    startDate: "2024-11-04T00:00:00Z",
    endDate: "2024-11-10T23:59:59Z",
    isActive: true,
    conditionJson: null,
    rewardType: "FreeShipping",
    rewardValue: 0,
  },
];

const clone = (data) => JSON.parse(JSON.stringify(data));

const ensureBackend = () => {
  if (!isBackendAvailable()) {
    throw new Error(
      "Backend API devre dışı. Lütfen sunucu bağlantısını kontrol edin.",
    );
  }
};

export const AdminService = {
  // Dashboard
  getDashboardStats: async () => {
    ensureBackend();
    try {
      return await api.get("/api/admin/dashboard/overview", { timeout: 10000 });
    } catch (error) {
      if (error?.status === 404 || error?.response?.status === 404) {
        return api.get("/api/admin/dashboard/stats", { timeout: 10000 });
      }
      throw error;
    }
  },
  // Users
  getUsers: async () => {
    ensureBackend();
    return api.get("/api/admin/users");
  },
  createUser: async (payload) => {
    ensureBackend();
    return api.post("/api/admin/users", payload);
  },
  updateUserRole: async (id, role) => {
    ensureBackend();
    return api.put(`/api/admin/users/${id}/role`, { role });
  },
  // ============================================================================
  // Kullanıcı Silme - Backend AdminUsersController.DeleteUser endpoint'i ile eşleşir
  // Yetki: Users.Delete permission gerektirir (SuperAdmin/Admin)
  // ============================================================================
  deleteUser: async (id) => {
    ensureBackend();
    return api.delete(`/api/admin/users/${id}`);
  },

  // ============================================================================
  // Madde 8: Kullanıcı Şifre Güncelleme
  // Backend: AdminUsersController.UpdateUserPassword endpoint'i
  // Yetki: Users.Update permission gerektirir
  // ============================================================================
  updateUserPassword: async (id, newPassword) => {
    ensureBackend();
    return api.put(`/api/admin/users/${id}/password`, { newPassword });
  },

  // ============================================================================
  // Admin Profil Yönetimi - Kendi Bilgilerini Güncelleme
  // Backend: AuthController.GetCurrentUser ve AccountController
  // ============================================================================

  /**
   * Giriş yapmış kullanıcının kendi profil bilgilerini getirir
   * Backend: AccountController.GetProfile endpoint'i (PhoneNumber, Address, City dahil)
   */
  getCurrentUser: async () => {
    ensureBackend();
    return api.get("/api/account/profile");
  },

  /**
   * Giriş yapmış kullanıcının profil bilgilerini günceller (ad, soyad, email, telefon, adres, şehir)
   */
  updateProfile: async (profileData) => {
    ensureBackend();
    return api.put("/api/account/profile", profileData);
  },

  /**
   * Giriş yapmış kullanıcının şifresini değiştirir
   * Backend: AccountController.ChangePassword endpoint'i
   */
  changePassword: async (passwordData) => {
    ensureBackend();
    return api.post("/api/account/change-password", {
      currentPassword: passwordData.oldPassword,
      newPassword: passwordData.newPassword,
    });
  },

  // ============================================================================
  // Roller API - Backend AdminRolesController endpoint'leri
  // Rol listesi ve atanabilir roller için
  // ============================================================================
  getRoles: async () => {
    ensureBackend();
    return api.get("/api/admin/roles");
  },

  // ============================================================================
  // Kullanıcı Durum Güncelleme (Aktif/Pasif)
  // Backend: AdminUsersController.UpdateUser endpoint'i ile
  // ============================================================================
  updateUserStatus: async (id, isActive) => {
    ensureBackend();
    return api.put(`/api/admin/users/${id}`, { isActive });
  },

  // ============================================================================
  // Kullanıcı Bilgileri Güncelleme (Ad, Soyad, Email, Telefon, Adres, Şehir)
  // Backend: AdminUsersController.UpdateUser endpoint'i (PUT /api/admin/users/{id})
  // ============================================================================
  updateUser: async (id, userData) => {
    ensureBackend();
    return api.put(`/api/admin/users/${id}`, userData);
  },

  // Logs
  getAuditLogs: async (params = {}) => {
    ensureBackend();
    return api.get("/api/admin/logs/audit", { params });
  },
  getErrorLogs: async (params = {}) => {
    ensureBackend();
    return api.get("/api/admin/logs/errors", { params });
  },
  getSystemLogs: async (params = {}) => {
    ensureBackend();
    return api.get("/api/admin/logs/system", { params });
  },
  getInventoryLogs: async (params = {}) => {
    ensureBackend();
    return api.get("/api/admin/logs/inventory", { params });
  },

  // Categories - GERÇEK BACKEND API
  getCategories: async () => {
    debugLog("Admin Categories - Gerçek Backend API kullanılıyor");
    return categoryServiceReal.getAllAdmin();
  },
  createCategory: async (formData) => {
    return categoryServiceReal.create(formData);
  },
  updateCategory: async (id, formData) => {
    return categoryServiceReal.update(id, formData);
  },
  deleteCategory: async (id) => {
    return categoryServiceReal.delete(id);
  },

  // Products - JSON SERVER (Geçici - Mikro API gelene kadar)
  getProducts: async (page = 1, size = 10) => {
    debugLog("Admin Products - JSON Server kullanılıyor (geçici)");
    return productServiceMock.getAll();
  },
  createProduct: async (payload) => {
    return productServiceMock.create(payload);
  },
  updateProduct: async (id, payload) => {
    return productServiceMock.update(id, payload);
  },
  deleteProduct: async (id) => {
    return productServiceMock.delete(id);
  },
  updateStock: (id, stock) =>
    api.patch(`/api/admin/products/${id}/stock`, stock),

  // Orders
  getOrders: async (page = 1, size = 20) => {
    ensureBackend();
    return api.get(`/api/admin/orders?page=${page}&size=${size}`);
  },
  getOrder: (id) => api.get(`/api/admin/orders/${id}`),
  updateOrderStatus: (id, status) =>
    api.put(`/api/admin/orders/${id}/status`, { status }),
  getRecentOrders: () => api.get("/api/admin/orders/recent"),
  // NEDEN: Admin panelinde sipariş silme aksiyonu gerekir.
  deleteOrder: (id) => api.delete(`/api/admin/orders/${id}`),

  // ============================================================
  // SİPARİŞ İPTAL + PARA İADESİ
  // Admin/Market görevlisi siparişi iptal eder ve otomatik POSNET reverse/return tetiklenir
  // ============================================================
  cancelOrderWithRefund: async (
    orderId,
    reason = "Admin tarafından iptal edildi",
  ) => {
    ensureBackend();
    return api.post(`/api/admin/orders/${orderId}/cancel-with-refund`, {
      reason,
    });
  },

  cancelOrder: async (orderId) => {
    ensureBackend();
    return api.post(`/api/admin/orders/${orderId}/cancel`);
  },

  // ============================================================
  // KURYE ATAMA - Backend'e POST isteği gönderir
  // ============================================================
  /**
   * Siparişe kurye atar.
   * @param {number} orderId - Sipariş ID
   * @param {number} courierId - Kurye ID
   * @returns {Promise<Object>} - Güncellenmiş sipariş bilgisi
   */
  assignCourier: async (orderId, courierId) => {
    ensureBackend();
    try {
      const response = await api.post(
        `/api/admin/orders/${orderId}/assign-courier`,
        {
          courierId: courierId,
        },
      );
      return response;
    } catch (error) {
      console.error("Kurye atama hatası:", error);
      throw error;
    }
  },

  // Reports
  getLowStockProducts: async () => {
    ensureBackend();
    const res = await api.get("/api/admin/reports/stock/low");
    return res;
  },
  getInventoryMovements: async ({ from, to } = {}) => {
    ensureBackend();
    const params = [];
    if (from) params.push(`from=${encodeURIComponent(from)}`);
    if (to) params.push(`to=${encodeURIComponent(to)}`);
    const qs = params.length ? `?${params.join("&")}` : "";
    const res = await api.get(`/api/admin/reports/inventory/movements${qs}`);
    const normalizedMovements = (res?.movements || []).map((movement) => ({
      ...movement,
      changeQuantity:
        movement.changeQuantity ?? movement.quantity ?? movement.Quantity ?? 0,
      changeType:
        movement.changeType ?? movement.action ?? movement.Action ?? "-",
    }));
    return {
      ...res,
      movements: normalizedMovements,
    };
  },
  getSalesReport: async (period = "daily") => {
    ensureBackend();
    const res = await api.get(
      `/api/admin/reports/sales?period=${encodeURIComponent(period)}`,
    );
    return res;
  },
  getErpSyncStatus: async ({ from, to } = {}) => {
    ensureBackend();
    const params = [];
    if (from) params.push(`from=${encodeURIComponent(from)}`);
    if (to) params.push(`to=${encodeURIComponent(to)}`);
    const qs = params.length ? `?${params.join("&")}` : "";
    return api.get(`/api/admin/reports/erp/sync-status${qs}`);
  },

  // Coupons
  getCoupons: async () => {
    if (shouldUseMockData()) {
      debugLog("Admin Coupons - Mock data kullanılıyor");
      return clone(mockCoupons);
    }
    ensureBackend();
    return api.get("/api/admin/coupons");
  },
  getCoupon: async (id) => {
    if (shouldUseMockData()) {
      const coupon = mockCoupons.find((c) => c.id === Number(id));
      if (!coupon) throw new Error("Kupon bulunamadı");
      return { ...coupon };
    }
    ensureBackend();
    return api.get(`/api/admin/coupons/${id}`);
  },
  createCoupon: async (coupon) => {
    if (shouldUseMockData()) {
      const nextId =
        mockCoupons.length > 0
          ? Math.max(...mockCoupons.map((c) => c.id)) + 1
          : 1;
      const prepared = {
        ...coupon,
        id: nextId,
        usageLimit:
          coupon.usageLimit == null || Number.isNaN(coupon.usageLimit)
            ? 1
            : Number(coupon.usageLimit),
      };
      mockCoupons.push(prepared);
      return { ...prepared };
    }
    ensureBackend();
    return api.post("/api/admin/coupons", coupon);
  },
  updateCoupon: async (id, coupon) => {
    if (shouldUseMockData()) {
      const idx = mockCoupons.findIndex((c) => c.id === Number(id));
      if (idx === -1) throw new Error("Kupon bulunamadı");
      mockCoupons[idx] = {
        ...mockCoupons[idx],
        ...coupon,
        id: mockCoupons[idx].id,
      };
      return { ...mockCoupons[idx] };
    }
    ensureBackend();
    return api.put(`/api/admin/coupons/${id}`, coupon);
  },
  deleteCoupon: async (id) => {
    if (shouldUseMockData()) {
      const idx = mockCoupons.findIndex((c) => c.id === Number(id));
      if (idx === -1) throw new Error("Kupon bulunamadı");
      mockCoupons.splice(idx, 1);
      return { success: true };
    }
    ensureBackend();
    return api.delete(`/api/admin/coupons/${id}`);
  },

  // Campaigns
  getCampaigns: async () => {
    if (shouldUseMockData()) {
      return clone(mockCampaigns);
    }
    ensureBackend();
    return api.get("/api/admin/campaigns");
  },
  getCampaignById: async (id) => {
    if (shouldUseMockData()) {
      const found = mockCampaigns.find((c) => c.id === Number(id));
      if (!found) throw new Error("Kampanya bulunamadı");
      return { ...found };
    }
    ensureBackend();
    return api.get(`/api/admin/campaigns/${id}`);
  },
  createCampaign: async (payload) => {
    if (shouldUseMockData()) {
      const nextId =
        mockCampaigns.length > 0
          ? Math.max(...mockCampaigns.map((c) => c.id)) + 1
          : 1;
      const prepared = {
        ...payload,
        id: nextId,
        startDate:
          payload.startDate instanceof Date
            ? payload.startDate.toISOString()
            : payload.startDate,
        endDate:
          payload.endDate instanceof Date
            ? payload.endDate.toISOString()
            : payload.endDate,
      };
      mockCampaigns.push(prepared);
      return { ...prepared };
    }
    ensureBackend();
    return api.post("/api/admin/campaigns", payload);
  },
  updateCampaign: async (id, payload) => {
    if (shouldUseMockData()) {
      const idx = mockCampaigns.findIndex((c) => c.id === Number(id));
      if (idx === -1) throw new Error("Kampanya bulunamadı");
      mockCampaigns[idx] = {
        ...mockCampaigns[idx],
        ...payload,
        startDate:
          payload.startDate instanceof Date
            ? payload.startDate.toISOString()
            : payload.startDate,
        endDate:
          payload.endDate instanceof Date
            ? payload.endDate.toISOString()
            : payload.endDate,
        id: mockCampaigns[idx].id,
      };
      return { ...mockCampaigns[idx] };
    }
    ensureBackend();
    return api.put(`/api/admin/campaigns/${id}`, payload);
  },
  deleteCampaign: async (id) => {
    if (shouldUseMockData()) {
      const idx = mockCampaigns.findIndex((c) => c.id === Number(id));
      if (idx === -1) throw new Error("Kampanya bulunamadı");
      mockCampaigns.splice(idx, 1);
      return { success: true };
    }
    ensureBackend();
    return api.delete(`/api/admin/campaigns/${id}`);
  },

  // ========== NEW CAMPAIGN SYSTEM METHODS ==========

  // Get campaign type enum values
  getCampaignTypes: async () => {
    if (shouldUseMockData()) {
      return [
        { value: 0, label: "Yüzde İndirim" },
        { value: 1, label: "Sabit Tutar İndirim" },
        { value: 2, label: "X Al Y Öde" },
        { value: 3, label: "Ücretsiz Kargo" },
      ];
    }
    ensureBackend();
    return api.get("/api/admin/campaigns/types");
  },

  // Get campaign target type enum values
  getCampaignTargetTypes: async () => {
    if (shouldUseMockData()) {
      return [
        { value: 0, label: "Tüm Ürünler" },
        { value: 1, label: "Kategori" },
        { value: 2, label: "Ürün" },
      ];
    }
    ensureBackend();
    return api.get("/api/admin/campaigns/target-types");
  },

  // Get all products for campaign target selection
  getCampaignProducts: async () => {
    if (shouldUseMockData()) {
      return [
        { id: 1, name: "Test Ürün 1", price: 100 },
        { id: 2, name: "Test Ürün 2", price: 200 },
      ];
    }
    ensureBackend();
    return api.get("/api/admin/campaigns/products");
  },

  // Get all categories for campaign target selection
  getCampaignCategories: async () => {
    if (shouldUseMockData()) {
      return [
        { id: 1, name: "Elektronik", parentId: null },
        { id: 2, name: "Giyim", parentId: null },
      ];
    }
    ensureBackend();
    return api.get("/api/admin/campaigns/categories");
  },

  // Toggle campaign active status
  toggleCampaignActive: async (id) => {
    if (shouldUseMockData()) {
      const idx = mockCampaigns.findIndex((c) => c.id === Number(id));
      if (idx === -1) throw new Error("Kampanya bulunamadı");
      mockCampaigns[idx].isActive = !mockCampaigns[idx].isActive;
      return { ...mockCampaigns[idx] };
    }
    ensureBackend();
    return api.patch(`/api/admin/campaigns/${id}/toggle`);
  },

  // ========== KAMPANYA ÖNİZLEME FONKSİYONLARI ==========

  /**
   * Mevcut bir kampanyanın önizlemesini getirir
   * @param {number} id - Kampanya ID
   * @returns {Promise<Object>} - Önizleme sonucu (etkilenen ürünler, fiyat değişimleri)
   */
  previewCampaign: async (id) => {
    if (shouldUseMockData()) {
      // Mock data için basit önizleme
      return {
        message: "Bu kampanya 5 ürünü etkileyecek.",
        affectedProducts: [
          {
            productId: 1,
            productName: "Test Ürün 1",
            categoryName: "Elektronik",
            originalPrice: 100,
            newPrice: 90,
            discountAmount: 10,
            discountPercentage: 10,
          },
          {
            productId: 2,
            productName: "Test Ürün 2",
            categoryName: "Giyim",
            originalPrice: 200,
            newPrice: 180,
            discountAmount: 20,
            discountPercentage: 10,
          },
        ],
        totalDiscount: 30,
        totalProductCount: 2,
        averageDiscountPercentage: 10,
      };
    }
    ensureBackend();
    return api.get(`/api/admin/campaigns/${id}/preview`);
  },

  /**
   * Form verilerine göre kampanya önizlemesi yapar (henüz kaydedilmemiş)
   * @param {Object} campaignData - Kampanya form verileri
   * @returns {Promise<Object>} - Önizleme sonucu
   */
  previewCampaignData: async (campaignData) => {
    if (shouldUseMockData()) {
      // Mock data için basit önizleme
      const discountValue = campaignData.discountValue || 10;
      return {
        message: `Bu kampanya tüm ürünleri etkileyecek. (%${discountValue} indirim)`,
        affectedProducts: [
          {
            productId: 1,
            productName: "Test Ürün 1",
            categoryName: "Elektronik",
            originalPrice: 100,
            newPrice: 100 - discountValue,
            discountAmount: discountValue,
            discountPercentage: discountValue,
          },
          {
            productId: 2,
            productName: "Test Ürün 2",
            categoryName: "Giyim",
            originalPrice: 200,
            newPrice: 200 - (200 * discountValue) / 100,
            discountAmount: (200 * discountValue) / 100,
            discountPercentage: discountValue,
          },
        ],
        totalDiscount: discountValue + (200 * discountValue) / 100,
        totalProductCount: 2,
        averageDiscountPercentage: discountValue,
      };
    }
    ensureBackend();
    return api.post("/api/admin/campaigns/preview", campaignData);
  },

  // Get campaign statistics
  getCampaignStats: async () => {
    if (shouldUseMockData()) {
      return {
        total: mockCampaigns.length,
        active: mockCampaigns.filter((c) => c.isActive).length,
        percentage: 1,
        fixedAmount: 0,
        buyXPayY: 0,
        freeShipping: 0,
      };
    }
    ensureBackend();
    return api.get("/api/admin/campaigns/stats");
  },

  // ============================================================================
  // İADE TALEBİ YÖNETİM SERVİSLERİ (ADMİN / MÜŞTERİ HİZMETLERİ)
  // Admin panelinden iade taleplerini listeleme, onaylama ve reddetme
  // ============================================================================

  /**
   * Tüm iade taleplerini listeler
   * @param {string|null} status - Opsiyonel durum filtresi (pending, approved, rejected, refunded, etc.)
   * @returns {Object} { success, data: RefundRequestListDto[] }
   */
  getRefundRequests: async (status = null) => {
    ensureBackend();
    const params = status ? { status } : {};
    return api.get("/api/admin/orders/refund-requests", { params });
  },

  /**
   * Bekleyen iade taleplerini listeler (dashboard widget)
   * @returns {Object} { success, data, count }
   */
  getPendingRefundRequests: async () => {
    ensureBackend();
    return api.get("/api/admin/orders/refund-requests/pending");
  },

  /**
   * İade talebini onaylar veya reddeder
   * @param {number} refundRequestId - İade talebi ID
   * @param {Object} data - { approve: boolean, adminNote?: string, refundAmount?: number }
   * @returns {Object} İşlem sonucu
   */
  processRefundRequest: async (refundRequestId, data) => {
    ensureBackend();
    return api.post(
      `/api/admin/orders/refund-requests/${refundRequestId}/process`,
      data,
    );
  },

  /**
   * Başarısız para iadesini yeniden dener
   * @param {number} refundRequestId - İade talebi ID
   * @returns {Object} İşlem sonucu
   */
  retryRefund: async (refundRequestId) => {
    ensureBackend();
    return api.post(
      `/api/admin/orders/refund-requests/${refundRequestId}/retry`,
    );
  },

  /**
   * Belirli bir siparişin iade taleplerini getirir
   * @param {number} orderId - Sipariş ID
   * @returns {Object} { success, data }
   */
  getOrderRefundRequests: async (orderId) => {
    ensureBackend();
    return api.get(`/api/admin/orders/${orderId}/refund-requests`);
  },
};

export default AdminService;
