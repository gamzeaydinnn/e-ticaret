import api from "./api";
import { isBackendAvailable, debugLog } from "../config/apiConfig";

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

export const AdminService = {
  // Dashboard
  getDashboardStats: async () => {
    if (!isBackendAvailable()) {
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
    return api.get("/api/Admin/dashboard/stats").then((r) => r.data);
  },
  // Users
  getUsers: async () => {
    if (!isBackendAvailable()) {
      debugLog("Admin Users - Mock data kullanılıyor");
      return [
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
      ];
    }
    return api.get("/api/Admin/users").then((r) => r.data);
  },

  // Categories
  getCategories: async () => {
    if (!isBackendAvailable()) {
      debugLog("Admin Categories - Mock data kullanılıyor");
      return [...mockCategories];
    }
    return api.get("/api/Admin/categories").then((r) => r.data);
  },
  createCategory: async (formData) => {
    if (!isBackendAvailable()) {
      const newId = Math.max(...mockCategories.map((c) => c.id)) + 1;
      const newCategory = { ...formData, id: newId, productCount: 0 };
      mockCategories.push(newCategory);
      return newCategory;
    }
    return api.post("/api/Admin/categories", formData).then((r) => r.data);
  },
  updateCategory: async (id, formData) => {
    if (!isBackendAvailable()) {
      const index = mockCategories.findIndex((c) => c.id === id);
      if (index !== -1) {
        mockCategories[index] = { ...mockCategories[index], ...formData };
        return mockCategories[index];
      }
      throw new Error("Kategori bulunamadı");
    }
    return api.put(`/api/Admin/categories/${id}`, formData).then((r) => r.data);
  },
  deleteCategory: async (id) => {
    if (!isBackendAvailable()) {
      const index = mockCategories.findIndex((c) => c.id === id);
      if (index !== -1) {
        mockCategories.splice(index, 1);
        return { success: true };
      }
      throw new Error("Kategori bulunamadı");
    }
    return api.delete(`/api/Admin/categories/${id}`).then((r) => r.data);
  },

  // Products
  getProducts: async (page = 1, size = 10) => {
    if (!isBackendAvailable()) {
      debugLog("Admin Products - Mock data kullanılıyor");
      return [...mockProducts];
    }
    return api
      .get(`/api/Admin/products?page=${page}&size=${size}`)
      .then((r) => r.data);
  },
  createProduct: async (payload) => {
    if (!isBackendAvailable()) {
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
    return api.post("/api/Admin/products", payload).then((r) => r.data);
  },
  updateProduct: async (id, payload) => {
    if (!isBackendAvailable()) {
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
    return api.put(`/api/Admin/products/${id}`, payload).then((r) => r.data);
  },
  deleteProduct: async (id) => {
    if (!isBackendAvailable()) {
      const index = mockProducts.findIndex((p) => p.id === id);
      if (index !== -1) {
        mockProducts.splice(index, 1);
        return { success: true };
      }
      throw new Error("Ürün bulunamadı");
    }
    return api.delete(`/api/Admin/products/${id}`).then((r) => r.data);
  },
  updateStock: (id, stock) =>
    api.patch(`/api/Admin/products/${id}/stock`, stock).then((r) => r.data),

  // Orders
  getOrders: async (page = 1, size = 20) => {
    if (!isBackendAvailable()) {
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
    return api
      .get(`/api/Admin/orders?page=${page}&size=${size}`)
      .then((r) => r.data);
  },
  getOrder: (id) => api.get(`/api/Admin/orders/${id}`).then((r) => r.data),
  updateOrderStatus: (id, status) =>
    api.put(`/api/Admin/orders/${id}/status`, { status }).then((r) => r.data),
  getRecentOrders: () =>
    api.get("/api/Admin/orders/recent").then((r) => r.data),
};

/*import api from './api';
export const AdminService = {
// Categories
getCategories: () => api.get('/admin/categories').then(r => r.data),
createCategory: (formData) => api.post('/admin/categories', formData).then(r => r.data),
updateCategory: (id, formData) => api.put(`/admin/categories/${id}`, formData).then(r => r.data),
deleteCategory: (id) => api.delete(`/admin/categories/${id}`).then(r => r.data),
// Products
listProducts: () => api.get('/admin/products').then(r => r.data),
createProduct: (formData) => api.post('/admin/products', formData).then(r=>r.data),
updateProduct: (id, formData) => api.put(`/admin/products/${id}`, formData).then(r=>r.data),
// Orders
listOrders: () => api.get('/admin/orders').then(r => r.data),
updateOrderStatus: (orderId, status) => api.put(`/admin/orders/${orderId}/status`, { status }).then(r=>r.data),
assignCourier: (orderId, courierId) => api.post('/admin/orders/assign-courier', { orderId, courierId }).then(r=>r.data)
}
*/

/*// src/services/adminService.js
import api from "./api";

export const AdminService = {
  // Categories
  getCategories: () => api.get("/admin/categories"),
  createCategory: (formData) => api.post("/admin/categories", formData),
  updateCategory: (id, formData) => api.put(`/admin/categories/${id}`, formData),
  deleteCategory: (id) => api.delete(`/admin/categories/${id}`),

  // Products
  listProducts: (query = "") => api.get(`/admin/products${query}`),
  createProduct: (formData) => api.post("/admin/products", formData),
  updateProduct: (id, formData) => api.put(`/admin/products/${id}`, formData),
  deleteProduct: (id) => api.delete(`/admin/products/${id}`),

  // Orders
  listOrders: (query = "") => api.get(`/admin/orders${query}`),
  updateOrderStatus: (orderId, status) =>
    api.put(`/admin/orders/${orderId}/status`, { status }),
  assignCourier: (orderId, courierId) =>
    api.post("/admin/orders/assign-courier", { orderId, courierId }),

  // Users
  listUsers: () => api.get("/admin/users"),
  getUser: (id) => api.get(`/admin/users/${id}`),
  updateUser: (id, payload) => api.put(`/admin/users/${id}`, payload),
};
*/
