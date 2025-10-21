import api from "./api";

const base = "/api/Orders";

export const OrderService = {
  create: (payload) => api.post(base, payload),
  list: (userId) => api.get(base, { params: { userId } }),
  getById: (id) => api.get(`${base}/${id}`),
  updateStatus: (id, status) => api.patch(`${base}/${id}/status`, { status }),
  cancel: (id) => api.post(`${base}/${id}/cancel`),
  checkout: (payload) => api.post(`${base}/checkout`, payload),
  downloadInvoice: async (id) => {
    const blob = await api.get(`${base}/${id}/invoice`, {
      responseType: "blob",
      transformResponse: (value) => value,
    });

    const url = window.URL.createObjectURL(new Blob([blob]));
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", `invoice-${id}.pdf`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },
};
