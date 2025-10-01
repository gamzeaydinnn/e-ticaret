/*import axios from 'axios';
const api = axios.create({ baseURL: import.meta.env.VITE_API || '/api' });
// Interceptor: token ekleme
api.interceptors.request.use(cfg => {
const token = localStorage.getItem('token');
if (token) cfg.headers.Authorization = `Bearer ${token}`;
return cfg;
});
export default api;*/

/*diğer öneri
src/services/api.js
import axios from 'axios';
const api = axios.create({ baseURL: import.meta.env.VITE_API || '/api' });
// Interceptor: token ekleme
api.interceptors.request.use(cfg => {
  const token = localStorage.getItem('token');
  if (token) cfg.headers.Authorization = `Bearer ${token}`;
  return cfg;
});
export default api;
src/services/adminService.js
import api from './api';
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