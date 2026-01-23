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

const mockLowStockReport = {
  threshold: 5,
  products: [
    { id: 6, name: "Domates Kg", stockQuantity: 4 },
    { id: 7, name: "Salatalık Kg", stockQuantity: 3 },
    { id: 13, name: "Cif Krem Temizleyici", stockQuantity: 2 },
  ],
};

const mockInventoryMovements = [
  {
    id: 1,
    productId: 4,
    productName: "Pınar Süt 1L",
    changeQuantity: 25,
    changeType: "Inbound",
    createdAt: "2024-10-08T08:30:00Z",
    note: "Depo girişi",
  },
  {
    id: 2,
    productId: 1,
    productName: "Dana Kuşbaşı",
    changeQuantity: -5,
    changeType: "Outbound",
    createdAt: "2024-10-08T11:45:00Z",
    note: "Online satış",
  },
  {
    id: 3,
    productId: 12,
    productName: "Tahıl Cipsi 150gr",
    changeQuantity: 40,
    changeType: "Inbound",
    createdAt: "2024-10-07T14:10:00Z",
    note: "Tedarikçi teslimatı",
  },
  {
    id: 4,
    productId: 6,
    productName: "Domates Kg",
    changeQuantity: -12,
    changeType: "Outbound",
    createdAt: "2024-10-06T09:20:00Z",
    note: "Mağaza satışı",
  },
];

const mockSalesReports = {
  daily: {
    ordersCount: 38,
    revenue: 12450.75,
    itemsSold: 92,
    topProducts: [
      { productId: 4, quantity: 18 },
      { productId: 7, quantity: 15 },
      { productId: 1, quantity: 11 },
    ],
  },
  weekly: {
    ordersCount: 210,
    revenue: 86540.1,
    itemsSold: 476,
    topProducts: [
      { productId: 6, quantity: 62 },
      { productId: 2, quantity: 55 },
      { productId: 13, quantity: 48 },
    ],
  },
  monthly: {
    ordersCount: 840,
    revenue: 342980.44,
    itemsSold: 1904,
    topProducts: [
      { productId: 4, quantity: 210 },
      { productId: 6, quantity: 198 },
      { productId: 1, quantity: 156 },
    ],
  },
};

const mockErpSyncStatus = {
  groups: [
    {
      entity: "Products",
      direction: "Outbound",
      lastAttemptAt: "2024-10-09T10:32:00Z",
      lastStatus: "Success",
      lastSuccessAt: "2024-10-09T10:32:00Z",
      updatedCount: 32,
      recentCount: 124,
      lastError: "",
    },
    {
      entity: "Stocks",
      direction: "Inbound",
      lastAttemptAt: "2024-10-09T09:15:00Z",
      lastStatus: "Success",
      lastSuccessAt: "2024-10-09T09:15:00Z",
      updatedCount: 68,
      recentCount: 68,
      lastError: "",
    },
    {
      entity: "Prices",
      direction: "Inbound",
      lastAttemptAt: "2024-10-08T21:00:00Z",
      lastStatus: "Failed",
      lastSuccessAt: "2024-10-08T08:20:00Z",
      updatedCount: 0,
      recentCount: 40,
      lastError: "ERP bağlantısı zaman aşımına uğradı",
    },
    {
      entity: "Orders",
      direction: "Outbound",
      lastAttemptAt: "2024-10-09T07:45:00Z",
      lastStatus: "Success",
      lastSuccessAt: "2024-10-09T07:45:00Z",
      updatedCount: 23,
      recentCount: 23,
      lastError: "",
    },
  ],
};

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
    if (shouldUseMockData()) {
      debugLog("Admin Dashboard - Mock data kullanılıyor");
      return {
        totalUsers: 156,
        totalProducts: 89,
        totalOrders: 234,
        totalRevenue: 45670.5,
        recentOrders: [
          {
            id: 1,
            customerName: "Ahmet Yılmaz",
            amount: 299.9,
            status: "Completed",
            date: "2024-10-08",
          },
          {
            id: 2,
            customerName: "Ayşe Kaya",
            amount: 156.75,
            status: "Processing",
            date: "2024-10-08",
          },
          {
            id: 3,
            customerName: "Mehmet Öz",
            amount: 489.2,
            status: "Shipped",
            date: "2024-10-07",
          },
        ],
        topProducts: [
          { name: "Cif Krem Temizleyici", sales: 45 },
          { name: "Pınar Süt 1L", sales: 38 },
          { name: "Domates Kg", sales: 32 },
        ],
      };
    }
    ensureBackend();
    // Backend route: /api/admin/dashboard/stats (küçük harfli)
    return api.get("/api/admin/dashboard/stats");
  },
  // Users
  getUsers: async () => {
    if (shouldUseMockData()) {
      debugLog("Admin Users - Mock data kullanılıyor");
      return {
        success: true,
        data: [
          {
            id: 1,
            firstName: "Ahmet",
            lastName: "Yılmaz",
            username: "ahmet123",
            email: "ahmet@example.com",
            phoneNumber: "0555 123 4567",
            role: "Customer",
            createdAt: "2024-01-15",
            isActive: true,
          },
          {
            id: 2,
            firstName: "Ayşe",
            lastName: "Kaya",
            username: "ayse456",
            email: "ayse@example.com",
            phoneNumber: "0555 234 5678",
            role: "Customer",
            createdAt: "2024-02-20",
            isActive: true,
          },
          {
            id: 3,
            firstName: "Mehmet",
            lastName: "Admin",
            username: "admin",
            email: "admin@admin.com",
            phoneNumber: "0555 999 9999",
            role: "Admin",
            createdAt: "2024-01-01",
            isActive: true,
          },
        ],
        count: 3,
      };
    }

    try {
      ensureBackend();
      return await api.get("/api/admin/users");
    } catch (error) {
      console.error("Users fetch error:", error);
      throw error;
    }
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
    if (shouldUseMockData()) {
      debugLog("Admin Orders - Mock data kullanılıyor");
      return [
        {
          id: 1,
          customerName: "Ahmet Yılmaz",
          userId: 1,
          totalPrice: 299.9,
          status: "Completed",
          orderDate: "2024-10-08",
          items: 3,
        },
        {
          id: 2,
          customerName: "Ayşe Kaya",
          userId: 2,
          totalPrice: 156.75,
          status: "Processing",
          orderDate: "2024-10-08",
          items: 2,
        },
        {
          id: 3,
          customerName: "Mehmet Öz",
          userId: 3,
          totalPrice: 489.2,
          status: "Shipped",
          orderDate: "2024-10-07",
          items: 5,
        },
      ];
    }
    ensureBackend();
    return api.get(`/api/admin/orders?page=${page}&size=${size}`);
  },
  getOrder: (id) => api.get(`/api/admin/orders/${id}`),
  updateOrderStatus: (id, status) =>
    api.put(`/api/admin/orders/${id}/status`, { status }),
  getRecentOrders: () => api.get("/api/admin/orders/recent"),

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
    if (shouldUseMockData()) {
      return clone(mockLowStockReport);
    }
    ensureBackend();
    const res = await api.get("/api/admin/reports/stock/low");
    return res;
  },
  getInventoryMovements: async ({ from, to } = {}) => {
    if (shouldUseMockData()) {
      const fromDate = from ? new Date(`${from}T00:00:00`) : null;
      const toDate = to ? new Date(`${to}T23:59:59`) : null;
      const filtered = mockInventoryMovements.filter((m) => {
        const ts = new Date(m.createdAt).getTime();
        if (fromDate && ts < fromDate.getTime()) return false;
        if (toDate && ts > toDate.getTime()) return false;
        return true;
      });
      return {
        start: fromDate ? fromDate.toISOString() : null,
        end: toDate ? toDate.toISOString() : null,
        movements: clone(filtered),
      };
    }
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
    if (shouldUseMockData()) {
      const key = String(period || "daily").toLowerCase();
      const data = mockSalesReports[key] || mockSalesReports.daily;
      return clone(data);
    }
    ensureBackend();
    const res = await api.get(
      `/api/admin/reports/sales?period=${encodeURIComponent(period)}`,
    );
    return res;
  },
  getErpSyncStatus: async ({ from, to } = {}) => {
    if (shouldUseMockData()) {
      return {
        from,
        to,
        groups: clone(mockErpSyncStatus.groups),
      };
    }
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
};

export default AdminService;
