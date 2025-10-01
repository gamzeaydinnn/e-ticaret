import client from "../api/client";

export const adminService = {
  // Dashboard
  getDashboardStats: async () => {
    try {
      const response = await client.get("/admin/dashboard/stats");
      return response.data;
    } catch (error) {
      console.error("Dashboard stats error:", error);
      // Demo data fallback
      return {
        totalUsers: 156,
        totalProducts: 89,
        totalOrders: 234,
        todayOrders: 12,
        revenue: 45600,
      };
    }
  },

  // Products
  getProducts: async (page = 1, size = 10) => {
    try {
      const response = await client.get(
        `/admin/products?page=${page}&size=${size}`
      );
      return response.data;
    } catch (error) {
      console.error("Get products error:", error);
      return [];
    }
  },

  createProduct: async (productData) => {
    try {
      const response = await client.post("/admin/products", productData);
      return response.data;
    } catch (error) {
      console.error("Create product error:", error);
      throw error;
    }
  },

  updateProduct: async (id, productData) => {
    try {
      await client.put(`/admin/products/${id}`, productData);
    } catch (error) {
      console.error("Update product error:", error);
      throw error;
    }
  },

  deleteProduct: async (id) => {
    try {
      await client.delete(`/admin/products/${id}`);
    } catch (error) {
      console.error("Delete product error:", error);
      throw error;
    }
  },

  updateStock: async (id, stock) => {
    try {
      await client.patch(`/admin/products/${id}/stock`, stock);
    } catch (error) {
      console.error("Update stock error:", error);
      throw error;
    }
  },

  // Orders
  getOrders: async (page = 1, size = 20) => {
    try {
      const response = await client.get(
        `/admin/orders?page=${page}&size=${size}`
      );
      return response.data;
    } catch (error) {
      console.error("Get orders error:", error);
      return [];
    }
  },

  getOrder: async (id) => {
    try {
      const response = await client.get(`/admin/orders/${id}`);
      return response.data;
    } catch (error) {
      console.error("Get order error:", error);
      throw error;
    }
  },

  updateOrderStatus: async (id, status) => {
    try {
      await client.put(`/admin/orders/${id}/status`, status);
    } catch (error) {
      console.error("Update order status error:", error);
      throw error;
    }
  },

  getRecentOrders: async () => {
    try {
      const response = await client.get("/admin/orders/recent");
      return response.data;
    } catch (error) {
      console.error("Get recent orders error:", error);
      return [];
    }
  },
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