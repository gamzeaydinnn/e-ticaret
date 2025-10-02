import api from "./api";

export const AdminService = {
  // Dashboard
  getDashboardStats: () => api.get("/api/Admin/dashboard/stats").then(r => r.data),
  // Categories
getCategories: () => api.get('/admin/categories').then(r => r.data),
createCategory: (formData) => api.post('/admin/categories', formData).then(r => r.data),
updateCategory: (id, formData) => api.put(`/admin/categories/${id}`, formData).then(r => r.data),
deleteCategory: (id) => api.delete(`/admin/categories/${id}`).then(r => r.data),

  // Products
  getProducts: (page = 1, size = 10) => api.get(`/api/Admin/products?page=${page}&size=${size}`).then(r => r.data),
  createProduct: (payload) => api.post("/api/Admin/products", payload).then(r => r.data),
  updateProduct: (id, payload) => api.put(`/api/Admin/products/${id}`, payload).then(r => r.data),
  deleteProduct: (id) => api.delete(`/api/Admin/products/${id}`).then(r => r.data),
  updateStock: (id, stock) => api.patch(`/api/Admin/products/${id}/stock`, stock).then(r => r.data),

  // Orders
  getOrders: (page = 1, size = 20) => api.get(`/api/Admin/orders?page=${page}&size=${size}`).then(r => r.data),
  getOrder: (id) => api.get(`/api/Admin/orders/${id}`).then(r => r.data),
  updateOrderStatus: (id, status) => api.put(`/api/Admin/orders/${id}/status`, { status }).then(r => r.data),
  getRecentOrders: () => api.get("/api/Admin/orders/recent").then(r => r.data),


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