import React, { useEffect, useMemo, useState } from "react";
import { AdminService } from "../../services/adminService";

const initialForm = {
  id: 0,
  code: "",
  isPercentage: false,
  value: "",
  expirationDate: "",
  minOrderAmount: "",
  usageLimit: 1,
  isActive: true,
};

export default function CouponManagement() {
  const [coupons, setCoupons] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState(null); // coupon or null
  const [form, setForm] = useState(initialForm);
  const [message, setMessage] = useState("");
  const [messageType, setMessageType] = useState("success"); // success | danger

  useEffect(() => {
    loadCoupons();
  }, []);

  async function loadCoupons() {
    try {
      setLoading(true);
      const data = await AdminService.getCoupons();
      setCoupons(Array.isArray(data) ? data : []);
    } catch (e) {
      setError(e.message || "Kuponlar yüklenemedi");
    } finally {
      setLoading(false);
    }
  }

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return coupons;
    return coupons.filter((c) => {
      if (c.code?.toLowerCase().includes(q)) return true;
      if ((q === "aktif" && c.isActive) || (q === "pasif" && !c.isActive))
        return true;
      return false;
    });
  }, [coupons, search]);

  function openCreate() {
    setEditing(null);
    setForm(initialForm);
    setShowModal(true);
  }

  function openEdit(coupon) {
    setEditing(coupon);
    setForm({
      id: coupon.id,
      code: coupon.code || "",
      isPercentage: !!coupon.isPercentage,
      value: coupon.value?.toString?.() ?? "",
      expirationDate: coupon.expirationDate
        ? coupon.expirationDate.slice(0, 10)
        : "",
      minOrderAmount: coupon.minOrderAmount?.toString?.() ?? "",
      usageLimit: coupon.usageLimit ?? 1,
      isActive: coupon.isActive,
    });
    setShowModal(true);
  }

  function closeModal() {
    setShowModal(false);
    setEditing(null);
    setForm(initialForm);
  }

  const onFormChange = (e) => {
    const { name, value, type, checked } = e.target;
    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  function buildPayload() {
    const payload = {
      id: form.id || 0,
      code: (form.code || "").trim(),
      isPercentage: !!form.isPercentage,
      value: form.value === "" ? 0 : parseFloat(form.value),
      expirationDate: form.expirationDate
        ? new Date(form.expirationDate + "T00:00:00")
        : null,
      minOrderAmount:
        form.minOrderAmount === "" || form.minOrderAmount === null
          ? null
          : parseFloat(form.minOrderAmount),
      usageLimit: form.usageLimit ? parseInt(form.usageLimit, 10) : 1,
      isActive: !!form.isActive,
    };
    return payload;
  }

  async function onSubmit(e) {
    e.preventDefault();
    try {
      const payload = buildPayload();
      if (!payload.code) throw new Error("Kupon kodu gereklidir");
      if (isNaN(payload.value)) throw new Error("İndirim değeri geçersiz");
      if (!payload.expirationDate) throw new Error("Bitiş tarihi gereklidir");

      if (editing) {
        await AdminService.updateCoupon(editing.id, payload);
        setMessage("Kupon güncellendi");
        setMessageType("success");
      } else {
        await AdminService.createCoupon(payload);
        setMessage("Kupon eklendi");
        setMessageType("success");
      }
      await loadCoupons();
      closeModal();
    } catch (e2) {
      setMessage(e2.message || "İşlem başarısız");
      setMessageType("danger");
    }
    setTimeout(() => setMessage(""), 2500);
  }

  async function onDelete(id) {
    if (!window.confirm("Kuponu silmek istediğinize emin misiniz?")) return;
    try {
      await AdminService.deleteCoupon(id);
      setMessage("Kupon silindi");
      setMessageType("success");
      await loadCoupons();
    } catch (e) {
      setMessage(e.message || "Silme işlemi başarısız");
      setMessageType("danger");
    }
    setTimeout(() => setMessage(""), 2500);
  }

  const totalCount = coupons.length;
  const activeCount = coupons.filter((c) => c.isActive).length;

  return (
    <div className="container-fluid p-2 p-md-4">
      {/* Header - Responsive */}
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center mb-3 mb-md-4 gap-2">
        <div className="mb-2 mb-md-0">
          <h1 className="h4 h3-md fw-bold mb-1" style={{ color: "#2d3748" }}>
            <i
              className="fas fa-ticket-alt me-2"
              style={{ color: "#f57c00" }}
            />
            Kupon Yönetimi
          </h1>
          <div className="text-muted" style={{ fontSize: "0.8rem" }}>
            İndirim kuponlarını oluşturun, düzenleyin ve yönetin
          </div>
        </div>
        <div className="d-flex flex-column flex-sm-row gap-2 w-100 w-md-auto">
          <input
            type="text"
            className="form-control form-control-sm"
            style={{ minWidth: 180, minHeight: "44px" }}
            placeholder="Ara: kod, aktif/pasif"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <button
            className="btn btn-sm text-white fw-semibold"
            style={{
              background: "linear-gradient(135deg, #f57c00, #ff9800)",
              minHeight: "44px",
              whiteSpace: "nowrap",
            }}
            onClick={openCreate}
          >
            <i className="fas fa-plus me-2"></i>
            Yeni Kupon
          </button>
        </div>
      </div>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}
      {message && (
        <div className={`alert alert-${messageType}`} role="alert">
          {message}
        </div>
      )}

      <div className="row g-2 g-md-4">
        <div className="col-12">
          <div className="card border-0 shadow-sm">
            <div className="card-header bg-white d-flex align-items-center justify-content-between py-2 px-3">
              <div style={{ fontSize: "0.85rem" }}>
                <i className="fas fa-tags me-2 text-primary"></i>
                Kuponlar
                <small className="ms-2 text-muted d-none d-sm-inline">
                  Toplam: {totalCount} · Aktif: {activeCount}
                </small>
              </div>
              <button
                className="btn btn-sm btn-outline-secondary"
                onClick={loadCoupons}
                disabled={loading}
                style={{ minHeight: "36px" }}
              >
                Yenile
              </button>
            </div>
            <div className="card-body p-2 p-md-3">
              {loading ? (
                <div className="text-muted text-center py-3">Yükleniyor...</div>
              ) : (
                <div className="table-responsive">
                  <table
                    className="table table-sm align-middle admin-mobile-table"
                    style={{ fontSize: "0.8rem" }}
                  >
                    <thead>
                      <tr>
                        <th style={{ width: 50 }}>ID</th>
                        <th>Kod</th>
                        <th>İndirim</th>
                        <th className="d-none d-lg-table-cell">Tür</th>
                        <th>Bitiş</th>
                        <th className="d-none d-md-table-cell">Min Tutar</th>
                        <th className="d-none d-lg-table-cell">Kullanım</th>
                        <th>Durum</th>
                        <th style={{ width: 100 }}>İşlem</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filtered.length ? (
                        filtered.map((c) => (
                          <tr key={c.id}>
                            <td data-label="ID">#{c.id}</td>
                            <td data-label="Kod" className="fw-semibold">
                              {c.code}
                            </td>
                            <td data-label="İndirim">
                              {c.isPercentage
                                ? `${Number(c.value || 0)}%`
                                : `₺${Number(c.value || 0).toLocaleString(
                                    "tr-TR",
                                    { minimumFractionDigits: 2 }
                                  )}`}
                            </td>
                            <td
                              data-label="Tür"
                              className="d-none d-lg-table-cell"
                            >
                              {c.isPercentage ? "Yüzde" : "Tutar"}
                            </td>
                            <td data-label="Bitiş">
                              {c.expirationDate
                                ? new Date(c.expirationDate).toLocaleDateString(
                                    "tr-TR"
                                  )
                                : "-"}
                            </td>
                            <td
                              data-label="Min Tutar"
                              className="d-none d-md-table-cell"
                            >
                              {c.minOrderAmount != null
                                ? `₺${Number(c.minOrderAmount).toLocaleString(
                                    "tr-TR",
                                    { minimumFractionDigits: 0 }
                                  )}`
                                : "-"}
                            </td>
                            <td
                              data-label="Kullanım"
                              className="d-none d-lg-table-cell"
                            >
                              {c.usageLimit ?? 1}
                            </td>
                            <td data-label="Durum">
                              <span
                                className={`badge ${
                                  c.isActive ? "bg-success" : "bg-secondary"
                                }`}
                                style={{ fontSize: "0.7rem" }}
                              >
                                {c.isActive ? "Aktif" : "Pasif"}
                              </span>
                            </td>
                            <td data-label="İşlem">
                              <div className="d-flex gap-1 justify-content-end">
                                <button
                                  className="btn btn-outline-primary btn-sm"
                                  onClick={() => openEdit(c)}
                                  style={{
                                    minWidth: "36px",
                                    minHeight: "36px",
                                  }}
                                >
                                  <i className="fas fa-edit"></i>
                                </button>
                                <button
                                  className="btn btn-outline-danger btn-sm"
                                  onClick={() => onDelete(c.id)}
                                  style={{
                                    minWidth: "36px",
                                    minHeight: "36px",
                                  }}
                                >
                                  <i className="fas fa-trash"></i>
                                </button>
                              </div>
                            </td>
                          </tr>
                        ))
                      ) : (
                        <tr>
                          <td
                            colSpan="9"
                            className="text-muted text-center py-3"
                          >
                            Kayıt bulunamadı.
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Modal - Mobilde full-width */}
      {showModal && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-fullscreen-sm-down">
            <div
              className="modal-content border-0"
              style={{ borderRadius: "16px" }}
            >
              <div className="modal-header border-0 p-3 p-md-4">
                <h5
                  className="modal-title fw-bold"
                  style={{ color: "#2d3748", fontSize: "1.1rem" }}
                >
                  <i
                    className="fas fa-ticket-alt me-2"
                    style={{ color: "#f57c00" }}
                  ></i>
                  {editing ? "Kuponu Düzenle" : "Yeni Kupon Ekle"}
                </h5>
                <button className="btn-close" onClick={closeModal}></button>
              </div>

              <form onSubmit={onSubmit} className="admin-mobile-form">
                <div className="modal-body p-3 p-md-4">
                  <div className="row g-2 g-md-3">
                    <div className="col-12 col-md-6">
                      <label
                        className="form-label fw-semibold mb-1"
                        style={{ fontSize: "0.85rem" }}
                      >
                        Kupon Kodu
                      </label>
                      <input
                        name="code"
                        value={form.code}
                        onChange={onFormChange}
                        className="form-control border-0 py-2"
                        style={{
                          background: "rgba(245,124,0,0.05)",
                          borderRadius: 12,
                          minHeight: "44px",
                        }}
                        required
                        placeholder="Örn: WELCOME10"
                      />
                    </div>
                    <div className="col-8 col-md-4">
                      <label
                        className="form-label fw-semibold mb-1"
                        style={{ fontSize: "0.85rem" }}
                      >
                        İndirim
                      </label>
                      <input
                        name="value"
                        type="number"
                        step="0.01"
                        value={form.value}
                        onChange={onFormChange}
                        className="form-control border-0 py-2"
                        style={{
                          background: "rgba(245,124,0,0.05)",
                          borderRadius: 12,
                          minHeight: "44px",
                        }}
                        required
                        placeholder="0.00"
                      />
                    </div>
                    <div className="col-4 col-md-2 d-flex align-items-end">
                      <div
                        className="form-check"
                        style={{
                          minHeight: "44px",
                          display: "flex",
                          alignItems: "center",
                        }}
                      >
                        <input
                          className="form-check-input"
                          type="checkbox"
                          name="isPercentage"
                          checked={form.isPercentage}
                          onChange={onFormChange}
                          style={{ width: "1.25rem", height: "1.25rem" }}
                        />
                        <label className="form-check-label fw-semibold ms-2">
                          Yüzde
                        </label>
                      </div>
                    </div>

                    <div className="col-12 col-md-6">
                      <label
                        className="form-label fw-semibold mb-1"
                        style={{ fontSize: "0.85rem" }}
                      >
                        Bitiş Tarihi
                      </label>
                      <input
                        type="date"
                        name="expirationDate"
                        value={form.expirationDate}
                        onChange={onFormChange}
                        className="form-control border-0 py-2"
                        style={{
                          background: "rgba(245,124,0,0.05)",
                          borderRadius: 12,
                          minHeight: "44px",
                        }}
                        required
                      />
                    </div>
                    <div className="col-12 col-md-6">
                      <label
                        className="form-label fw-semibold mb-1"
                        style={{ fontSize: "0.85rem" }}
                      >
                        Minimum Sipariş Tutarı
                      </label>
                      <input
                        type="number"
                        name="minOrderAmount"
                        step="0.01"
                        value={form.minOrderAmount}
                        onChange={onFormChange}
                        className="form-control border-0 py-2"
                        style={{
                          background: "rgba(245,124,0,0.05)",
                          borderRadius: 12,
                          minHeight: "44px",
                        }}
                        placeholder="Opsiyonel"
                      />
                    </div>

                    <div className="col-8 col-md-6">
                      <label
                        className="form-label fw-semibold mb-1"
                        style={{ fontSize: "0.85rem" }}
                      >
                        Kullanım Limiti
                      </label>
                      <input
                        type="number"
                        name="usageLimit"
                        value={form.usageLimit}
                        onChange={onFormChange}
                        className="form-control border-0 py-2"
                        style={{
                          background: "rgba(245,124,0,0.05)",
                          borderRadius: 12,
                          minHeight: "44px",
                        }}
                        min={1}
                      />
                    </div>
                    <div className="col-4 col-md-6 d-flex align-items-end">
                      <div
                        className="form-check"
                        style={{
                          minHeight: "44px",
                          display: "flex",
                          alignItems: "center",
                        }}
                      >
                        <input
                          className="form-check-input"
                          type="checkbox"
                          name="isActive"
                          checked={form.isActive}
                          onChange={onFormChange}
                          style={{ width: "1.25rem", height: "1.25rem" }}
                        />
                        <label className="form-check-label fw-semibold ms-2">
                          Aktif
                        </label>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="modal-footer border-0 p-3 p-md-4">
                  <button
                    type="button"
                    className="btn btn-light"
                    onClick={closeModal}
                    style={{ minHeight: "44px" }}
                  >
                    İptal
                  </button>
                  <button
                    type="submit"
                    className="btn text-white fw-semibold px-4"
                    style={{
                      background: "linear-gradient(135deg, #f57c00, #ff9800)",
                      borderRadius: 8,
                      minHeight: "44px",
                    }}
                  >
                    {editing ? "Güncelle" : "Kaydet"}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
