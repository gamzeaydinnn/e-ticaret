import api from "./api";
import {
  isBackendAvailable,
  shouldUseMockData,
  debugLog,
} from "../config/apiConfig";

// Mock data storage
let mockCategories = [
  {
    id: 1,
    name: "Et ve Et Ürünleri",
    description: "Taze et ve şarküteri ürünleri",
    icon: "fa-drumstick-bite",
    productCount: 3,
    isActive: true,
  },
  {
    id: 2,
    name: "Süt ve Süt Ürünleri",
    description: "Süt, peynir, yoğurt ve türevleri",
    icon: "fa-cheese",
    productCount: 2,
    isActive: true,
  },
  {
    id: 3,
    name: "Meyve ve Sebze",
    description: "Taze meyve ve sebzeler",
    icon: "fa-apple-alt",
    productCount: 3,
    isActive: true,
  },
  {
    id: 4,
    name: "İçecekler",
    description: "Soğuk ve sıcak içecekler",
    icon: "fa-coffee",
    productCount: 3,
    isActive: true,
  },
  {
    id: 5,
    name: "Atıştırmalık",
    description: "Cipsi, kraker ve atıştırmalıklar",
    icon: "fa-cookie-bite",
    productCount: 1,
    isActive: true,
  },
  {
    id: 6,
    name: "Temizlik",
    description: "Ev temizlik ürünleri",
    icon: "fa-broom",
    productCount: 1,
    isActive: true,
  },
];

let mockProducts = [
  {
    id: 1,
    name: "Dana Kuşbaşı",
    categoryId: 1,
    categoryName: "Et ve Et Ürünleri",
    price: 89.9,
    stock: 25,
    description: "Taze dana eti kuşbaşı",
    imageUrl: "/images/dana-kusbasi.jpg",
    isActive: true,
  },
  {
    id: 2,
    name: "Kuzu İncik",
    categoryId: 1,
    categoryName: "Et ve Et Ürünleri",
    price: 95.5,
    stock: 15,
    description: "Taze kuzu incik eti",
    imageUrl: "/images/kuzu-incik.webp",
    isActive: true,
  },
  {
    id: 3,
    name: "Sucuk 250gr",
    categoryId: 1,
    categoryName: "Et ve Et Ürünleri",
    price: 24.9,
    stock: 30,
    description: "Geleneksel sucuk",
    imageUrl: "/images/sucuk.jpg",
    isActive: true,
  },
  {
    id: 4,
    name: "Pınar Süt 1L",
    categoryId: 2,
    categoryName: "Süt ve Süt Ürünleri",
    price: 12.5,
    stock: 50,
    description: "Taze tam yağlı süt",
    imageUrl: "/images/pınar-süt.jpg",
    isActive: true,
  },
  {
    id: 5,
    name: "Şek Kaşar Peyniri 200gr",
    categoryId: 2,
    categoryName: "Süt ve Süt Ürünleri",
    price: 35.9,
    stock: 20,
    description: "Eski kaşar peynir",
    imageUrl: "/images/sek-kasar-peyniri-200-gr-38be46-1650x1650.jpg",
    isActive: true,
  },
  {
    id: 6,
    name: "Domates Kg",
    categoryId: 3,
    categoryName: "Meyve ve Sebze",
    price: 8.75,
    stock: 100,
    description: "Taze domates",
    imageUrl: "/images/domates.webp",
    isActive: true,
  },
  {
    id: 7,
    name: "Salatalık Kg",
    categoryId: 3,
    categoryName: "Meyve ve Sebze",
    price: 6.5,
    stock: 80,
    description: "Taze salatalık",
    imageUrl: "/images/salatalik.jpg",
    isActive: true,
  },
  {
    id: 8,
    name: "Bulgur 1 Kg",
    categoryId: 3,
    categoryName: "Meyve ve Sebze",
    price: 15.9,
    stock: 40,
    description: "Pilavlık bulgur",
    imageUrl: "/images/bulgur.png",
    isActive: true,
  },
  {
    id: 9,
    name: "Coca Cola 330ml",
    categoryId: 4,
    categoryName: "İçecekler",
    price: 5.5,
    stock: 75,
    description: "Coca Cola teneke kutu",
    imageUrl: "/images/coca-cola.jpg",
    isActive: true,
  },
  {
    id: 10,
    name: "Lipton Ice Tea 330ml",
    categoryId: 4,
    categoryName: "İçecekler",
    price: 4.75,
    stock: 60,
    description: "Şeftali aromalı ice tea",
    imageUrl: "/images/lipton-ice-tea.jpg",
    isActive: true,
  },
  {
    id: 11,
    name: "Nescafe 200gr",
    categoryId: 4,
    categoryName: "İçecekler",
    price: 45.9,
    stock: 25,
    description: "Klasik nescafe",
    imageUrl: "/images/nescafe.jpg",
    isActive: true,
  },
  {
    id: 12,
    name: "Tahıl Cipsi 150gr",
    categoryId: 5,
    categoryName: "Atıştırmalık",
    price: 12.9,
    stock: 35,
    description: "Çıtır tahıl cipsi",
    imageUrl: "/images/tahil-cipsi.jpg",
    isActive: true,
  },
  {
    id: 13,
    name: "Cif Krem Temizleyici",
    categoryId: 6,
    categoryName: "Temizlik",
    price: 15.9,
    stock: 5,
    description: "Mutfak temizleyici",
    imageUrl: "/images/yeşil-cif-krem.jpg",
    isActive: false,
  },
];

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
      "Backend API devre dışı. Lütfen sunucu bağlantısını kontrol edin."
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
    return api.get("/api/Admin/dashboard/stats");
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

  // Categories
  getCategories: async () => {
    if (shouldUseMockData()) {
      debugLog("Admin Categories - Mock data kullanılıyor");
      return [...mockCategories];
    }
    ensureBackend();
    return api.get("/api/admin/categories");
  },
  createCategory: async (formData) => {
    if (shouldUseMockData()) {
      const newId = Math.max(...mockCategories.map((c) => c.id)) + 1;
      const newCategory = { ...formData, id: newId, productCount: 0 };
      mockCategories.push(newCategory);
      return newCategory;
    }
    ensureBackend();
    return api.post("/api/admin/categories", formData);
  },
  updateCategory: async (id, formData) => {
    if (shouldUseMockData()) {
      const index = mockCategories.findIndex((c) => c.id === id);
      if (index !== -1) {
        mockCategories[index] = { ...mockCategories[index], ...formData };
        return mockCategories[index];
      }
      throw new Error("Kategori bulunamadı");
    }
    ensureBackend();
    return api.put(`/api/admin/categories/${id}`, formData);
  },
  deleteCategory: async (id) => {
    if (shouldUseMockData()) {
      const index = mockCategories.findIndex((c) => c.id === id);
      if (index !== -1) {
        mockCategories.splice(index, 1);
        return { success: true };
      }
      throw new Error("Kategori bulunamadı");
    }
    ensureBackend();
    return api.delete(`/api/admin/categories/${id}`);
  },

  // Products
  getProducts: async (page = 1, size = 10) => {
    if (shouldUseMockData()) {
      debugLog("Admin Products - Mock data kullanılıyor");
      return [...mockProducts];
    }
    ensureBackend();
    return api.get(`/api/admin/products?page=${page}&size=${size}`);
  },
  createProduct: async (payload) => {
    if (shouldUseMockData()) {
      const newId = Math.max(...mockProducts.map((p) => p.id)) + 1;
      const category = mockCategories.find((c) => c.id == payload.categoryId);
      const newProduct = {
        ...payload,
        id: newId,
        categoryName: category ? category.name : "Kategori Yok",
      };
      mockProducts.push(newProduct);
      return newProduct;
    }
    ensureBackend();
    return api.post("/api/admin/products", payload);
  },
  updateProduct: async (id, payload) => {
    if (shouldUseMockData()) {
      const index = mockProducts.findIndex((p) => p.id === id);
      if (index !== -1) {
        const category = mockCategories.find((c) => c.id == payload.categoryId);
        mockProducts[index] = {
          ...mockProducts[index],
          ...payload,
          categoryName: category ? category.name : "Kategori Yok",
        };
        return mockProducts[index];
      }
      throw new Error("Ürün bulunamadı");
    }
    ensureBackend();
    return api.put(`/api/admin/products/${id}`, payload);
  },
  deleteProduct: async (id) => {
    if (shouldUseMockData()) {
      const index = mockProducts.findIndex((p) => p.id === id);
      if (index !== -1) {
        mockProducts.splice(index, 1);
        return { success: true };
      }
      throw new Error("Ürün bulunamadı");
    }
    ensureBackend();
    return api.delete(`/api/admin/products/${id}`);
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
  getRecentOrders: () =>
    api.get("/api/admin/orders/recent"),

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
    return res;
  },
  getSalesReport: async (period = "daily") => {
    if (shouldUseMockData()) {
      const key = String(period || "daily").toLowerCase();
      const data = mockSalesReports[key] || mockSalesReports.daily;
      return clone(data);
    }
    ensureBackend();
    const res = await api.get(`/api/admin/reports/sales?period=${encodeURIComponent(period)}`);
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
        mockCoupons.length > 0 ? Math.max(...mockCoupons.map((c) => c.id)) + 1 : 1;
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
};

export default AdminService;
