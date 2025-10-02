import api from "./api";

const base = "/api/Orders";

export const OrderService = {
  create: (payload) => api.post(base, payload).then(r => r.data),
  list: (userId) => api.get(base, { params: { userId } }).then(r => r.data),
  getById: (id) => api.get(`${base}/${id}`).then(r => r.data),
  updateStatus: (id, status) => api.patch(`${base}/${id}/status`, { status }).then(r => r.data),
  cancel: (id, reason) => api.patch(`${base}/${id}/cancel`, { reason }).then(r => r.data),
  checkout: (payload) => api.post(`${base}/checkout`, payload).then(r => r.data),
  downloadInvoice: (id) =>
    api.get(`${base}/${id}/invoice`, { responseType: "blob" })
      .then(response => {
        const url = window.URL.createObjectURL(new Blob([response.data]));
        const link = document.createElement("a");
        link.href = url;
        link.setAttribute("download", `invoice-${id}.pdf`);
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.URL.revokeObjectURL(url);
      }),
};
