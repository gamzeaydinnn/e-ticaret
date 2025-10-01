const API_BASE = import.meta.env.VITE_API || 'http://localhost:5000/api';
async function request(path, { method = 'GET', body, token } = {}){
  const headers = { 'Content-Type': 'application/json' };
  if(token) headers['Authorization'] = `Bearer ${token}`;
  const res = await fetch(`${API_BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined
  });
  if(!res.ok) throw new Error(await res.text());
  return res.json();
}
export const productApi = {
  list: (query) => request(`/products${query || ''}`),
  get: (id) => request(`/products/${id}`),
};
export const authApi = {
  login: (cred) => request('/auth/login', { method: 'POST', body: cred }),
  register: (data) => request('/auth/register', { method: 'POST', body: data })
};
export const orderApi = {
  create: (payload, token) => request('/orders', { method: 'POST', body: payload, token }),
};
export const courierApi = {
  myOrders: (token) => request('/courier/orders', { token }),
  updateStatus: (orderId, status, token) => request(`/courier/orders/${orderId}/status`, { method: 'POST', body: { status }, token })
};
