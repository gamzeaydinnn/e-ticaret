import React, { useEffect, useMemo, useState } from "react";
import { AdminService } from "../../services/adminService";

const initialForm = {
  id: 0,
  name: "",
  description: "",
  startDate: "",
  endDate: "",
  isActive: true,
  conditionJson: "",
  rewardType: "Percent",
  rewardValue: "",
};

const rewardLabels = {
  Percent: "% İndirim",
  Amount: "₺ İndirim",
  FreeShipping: "Ücretsiz Kargo",
};

const toInputDate = (value) => {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return date.toISOString().split("T")[0];
};

const formatDate = (value) => {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleDateString("tr-TR");
};

export default function AdminCampaigns() {
  const [campaigns, setCampaigns] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const [message, setMessage] = useState("");
  const [messageType, setMessageType] = useState("success");
  const [showModal, setShowModal] = useState(false);
  const [modalLoading, setModalLoading] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState(initialForm);

  useEffect(() => {
    loadCampaigns();
  }, []);

  async function loadCampaigns() {
    try {
      setLoading(true);
      setError("");
      const data = await AdminService.getCampaigns();
      setCampaigns(Array.isArray(data) ? data : []);
    } catch (err) {
      setError(err.message || "Kampanyalar yüklenemedi.");
    } finally {
      setLoading(false);
    }
  }

  const filteredCampaigns = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) return campaigns;
    return campaigns.filter((campaign) => {
      if (campaign.name?.toLowerCase().includes(query)) return true;
      if (campaign.description?.toLowerCase().includes(query)) return true;
      if (query === "aktif" && campaign.isActive) return true;
      if (query === "pasif" && !campaign.isActive) return true;
      return false;
    });
  }, [campaigns, search]);

  function openCreateModal() {
    setEditingId(null);
    setForm(initialForm);
    setShowModal(true);
    setModalLoading(false);
  }

  async function openEditModal(campaign) {
    setEditingId(campaign.id);
    setForm({
      ...initialForm,
      id: campaign.id,
      name: campaign.name || "",
      description: campaign.description || "",
      startDate: toInputDate(campaign.startDate),
      endDate: toInputDate(campaign.endDate),
      isActive: !!campaign.isActive,
    });
    setShowModal(true);
    setModalLoading(true);
    try {
      const detail = await AdminService.getCampaignById(campaign.id);
      setForm({
        id: detail.id,
        name: detail.name || "",
        description: detail.description || "",
        startDate: toInputDate(detail.startDate),
        endDate: toInputDate(detail.endDate),
        isActive: !!detail.isActive,
        conditionJson: detail.conditionJson || "",
        rewardType: detail.rewardType || "Percent",
        rewardValue:
          detail.rewardValue === null || detail.rewardValue === undefined
            ? ""
            : detail.rewardValue.toString(),
      });
    } catch (err) {
      setMessage(err.message || "Kampanya bilgileri yüklenemedi.");
      setMessageType("danger");
      setShowModal(false);
    } finally {
      setModalLoading(false);
    }
  }

  function closeModal() {
    setShowModal(false);
    setModalLoading(false);
    setEditingId(null);
    setForm(initialForm);
  }

  const onFormChange = (event) => {
    const { name, value, type, checked } = event.target;
    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  function buildPayload() {
    if (!form.startDate || !form.endDate) {
      throw new Error("Başlangıç ve bitiş tarihleri zorunludur.");
    }
    const rewardValue =
      form.rewardType === "FreeShipping"
        ? 0
        : form.rewardValue === ""
        ? NaN
        : parseFloat(form.rewardValue);

    if (Number.isNaN(rewardValue)) {
      throw new Error("Ödül değeri geçersiz.");
    }

    return {
      name: (form.name || "").trim(),
      description: form.description?.trim() || null,
      startDate: new Date(`${form.startDate}T00:00:00`),
      endDate: new Date(`${form.endDate}T23:59:59`),
      isActive: !!form.isActive,
      conditionJson: form.conditionJson?.trim() || null,
      rewardType: form.rewardType,
      rewardValue: rewardValue,
    };
  }

  async function handleSubmit(event) {
    event.preventDefault();
    try {
      if (!form.name.trim()) {
        throw new Error("Kampanya adı zorunludur.");
      }
      const payload = buildPayload();
      if (editingId) {
        await AdminService.updateCampaign(editingId, payload);
        setMessage("Kampanya güncellendi.");
      } else {
        await AdminService.createCampaign(payload);
        setMessage("Kampanya oluşturuldu.");
      }
      setMessageType("success");
      await loadCampaigns();
      closeModal();
    } catch (err) {
      setMessage(err.message || "İşlem gerçekleştirilemedi.");
      setMessageType("danger");
    } finally {
      setTimeout(() => setMessage(""), 3000);
    }
  }

  async function handleDelete(id) {
    const confirmed = window.confirm(
      "Kampanyayı silmek istediğinize emin misiniz?"
    );
    if (!confirmed) return;
    try {
      await AdminService.deleteCampaign(id);
      setMessage("Kampanya silindi.");
      setMessageType("success");
      await loadCampaigns();
    } catch (err) {
      setMessage(err.message || "Kampanya silinemedi.");
      setMessageType("danger");
    } finally {
      setTimeout(() => setMessage(""), 3000);
    }
  }

  const totalCount = campaigns.length;
  const activeCount = campaigns.filter((c) => c.isActive).length;

  return (
    <div
      className="container-fluid p-2 p-md-4"
      style={{ overflow: "hidden", maxWidth: "100%" }}
    >
      {/* Header - Responsive */}
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center mb-3 gap-2">
        <div className="mb-2 mb-md-0">
          <h1 className="h4 h3-md fw-bold mb-1" style={{ color: "#2d3748" }}>
            <i className="fas fa-gift me-2" style={{ color: "#f57c00" }}></i>
            Kampanya Yönetimi
          </h1>
          <p
            className="text-muted mb-0 d-none d-sm-block"
            style={{ fontSize: "0.8rem" }}
          >
            Kampanyalarınızı yönetin
          </p>
        </div>
        <div className="d-flex flex-column flex-sm-row gap-2 w-100 w-md-auto">
          <input
            type="text"
            className="form-control form-control-sm"
            placeholder="Ara: ad, aktif/pasif"
            style={{ minWidth: "150px", minHeight: "44px" }}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <div className="d-flex gap-2">
            <button
              className="btn btn-sm btn-outline-secondary"
              style={{ minHeight: "44px", minWidth: "44px" }}
              onClick={loadCampaigns}
              disabled={loading}
            >
              <i className="fas fa-sync-alt"></i>
            </button>
            <button
              className="btn btn-sm text-white fw-semibold flex-grow-1"
              style={{
                background: "linear-gradient(135deg, #f57c00, #ff9800)",
                minHeight: "44px",
                whiteSpace: "nowrap",
              }}
              onClick={openCreateModal}
            >
              <i className="fas fa-plus me-1"></i>Yeni Kampanya
            </button>
          </div>
        </div>
      </div>

      {error && (
        <div className="alert alert-danger py-2" style={{ fontSize: "0.8rem" }}>
          {error}
        </div>
      )}
      {message && (
        <div
          className={`alert alert-${messageType} py-2`}
          style={{ fontSize: "0.8rem" }}
        >
          {message}
        </div>
      )}

      <div className="card border-0 shadow-sm" style={{ borderRadius: "10px" }}>
        <div className="card-header bg-white py-2 px-3 d-flex justify-content-between align-items-center">
          <span style={{ fontSize: "0.9rem" }}>
            <i className="fas fa-layer-group me-2 text-primary"></i>Kampanyalar
          </span>
          <small className="text-muted" style={{ fontSize: "0.75rem" }}>
            Toplam: {totalCount} · Aktif: {activeCount}
          </small>
        </div>
        <div className="card-body p-2 p-md-3">
          {loading ? (
            <div className="text-muted text-center py-3">Yükleniyor...</div>
          ) : (
            <div className="table-responsive">
              <table
                className="table table-sm mb-0 admin-mobile-table"
                style={{ fontSize: "0.8rem" }}
              >
                <thead className="bg-light">
                  <tr>
                    <th className="px-2">ID</th>
                    <th className="px-2">Ad</th>
                    <th className="px-2 d-none d-md-table-cell">Tarih</th>
                    <th className="px-2">Durum</th>
                    <th className="px-2">İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredCampaigns.length ? (
                    filteredCampaigns.map((c) => (
                      <tr key={c.id}>
                        <td data-label="ID" className="px-2">
                          #{c.id}
                        </td>
                        <td
                          data-label="Ad"
                          className="px-2 fw-semibold text-truncate"
                          style={{ maxWidth: "150px" }}
                        >
                          {c.name}
                        </td>
                        <td
                          data-label="Tarih"
                          className="px-2 d-none d-md-table-cell"
                          style={{ fontSize: "0.75rem" }}
                        >
                          {formatDate(c.startDate)} - {formatDate(c.endDate)}
                        </td>
                        <td data-label="Durum" className="px-2">
                          <span
                            className={`badge ${
                              c.isActive ? "bg-success" : "bg-secondary"
                            }`}
                            style={{ fontSize: "0.7rem" }}
                          >
                            {c.isActive ? "Aktif" : "Pasif"}
                          </span>
                        </td>
                        <td data-label="İşlem" className="px-2">
                          <div className="d-flex gap-1 justify-content-end">
                            <button
                              className="btn btn-outline-primary btn-sm"
                              style={{ minWidth: "36px", minHeight: "36px" }}
                              onClick={() => openEditModal(c)}
                            >
                              <i className="fas fa-edit"></i>
                            </button>
                            <button
                              className="btn btn-outline-danger btn-sm"
                              style={{ minWidth: "36px", minHeight: "36px" }}
                              onClick={() => handleDelete(c.id)}
                            >
                              <i className="fas fa-trash"></i>
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan="5" className="text-muted text-center py-3">
                        Kayıt bulunamadı
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          )}
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
              <div className="modal-header border-0 py-3 px-3">
                <h5
                  className="modal-title fw-bold"
                  style={{ color: "#2d3748", fontSize: "1rem" }}
                >
                  <i
                    className="fas fa-gift me-2"
                    style={{ color: "#f57c00" }}
                  ></i>
                  {editingId ? "Kampanyayı Düzenle" : "Yeni Kampanya"}
                </h5>
                <button className="btn-close" onClick={closeModal}></button>
              </div>
              <form onSubmit={handleSubmit} className="admin-mobile-form">
                <div
                  className="modal-body p-3"
                  style={{ maxHeight: "70vh", overflowY: "auto" }}
                >
                  {modalLoading ? (
                    <div className="text-center text-muted py-4">
                      <div className="spinner-border spinner-border-sm me-2"></div>
                      Yükleniyor...
                    </div>
                  ) : (
                    <div className="row g-2 g-md-3">
                      <div className="col-12">
                        <label
                          className="form-label fw-semibold mb-1"
                          style={{ fontSize: "0.85rem" }}
                        >
                          Kampanya Adı
                        </label>
                        <input
                          className="form-control"
                          style={{ minHeight: "44px" }}
                          name="name"
                          value={form.name}
                          onChange={onFormChange}
                          required
                          placeholder="Örn: Sepette %10 İndirim"
                        />
                      </div>
                      <div className="col-12">
                        <label
                          className="form-label fw-semibold mb-1"
                          style={{ fontSize: "0.85rem" }}
                        >
                          Açıklama
                        </label>
                        <input
                          className="form-control"
                          style={{ minHeight: "44px" }}
                          name="description"
                          value={form.description}
                          onChange={onFormChange}
                          placeholder="Opsiyonel"
                        />
                      </div>
                      <div className="col-6">
                        <label
                          className="form-label fw-semibold mb-1"
                          style={{ fontSize: "0.85rem" }}
                        >
                          Başlangıç
                        </label>
                        <input
                          type="date"
                          className="form-control"
                          style={{ minHeight: "44px" }}
                          name="startDate"
                          value={form.startDate}
                          onChange={onFormChange}
                          required
                        />
                      </div>
                      <div className="col-6">
                        <label
                          className="form-label fw-semibold mb-1"
                          style={{ fontSize: "0.85rem" }}
                        >
                          Bitiş
                        </label>
                        <input
                          type="date"
                          className="form-control"
                          style={{ minHeight: "44px" }}
                          name="endDate"
                          value={form.endDate}
                          onChange={onFormChange}
                          required
                        />
                      </div>
                      <div className="col-6">
                        <label
                          className="form-label fw-semibold mb-1"
                          style={{ fontSize: "0.85rem" }}
                        >
                          Ödül Türü
                        </label>
                        <select
                          className="form-select"
                          style={{ minHeight: "44px" }}
                          name="rewardType"
                          value={form.rewardType}
                          onChange={onFormChange}
                        >
                          <option value="Percent">% İndirim</option>
                          <option value="Amount">₺ İndirim</option>
                          <option value="FreeShipping">Ücretsiz Kargo</option>
                        </select>
                      </div>
                      <div className="col-6">
                        <label
                          className="form-label fw-semibold mb-1"
                          style={{ fontSize: "0.85rem" }}
                        >
                          Ödül Değeri
                        </label>
                        <input
                          type="number"
                          className="form-control"
                          style={{ minHeight: "44px" }}
                          name="rewardValue"
                          value={form.rewardValue}
                          onChange={onFormChange}
                          min={0}
                          step="0.01"
                          disabled={form.rewardType === "FreeShipping"}
                        />
                      </div>
                      <div className="col-12">
                        <label
                          className="form-label fw-semibold mb-1"
                          style={{ fontSize: "0.85rem" }}
                        >
                          Kural (JSON)
                        </label>
                        <textarea
                          className="form-control"
                          rows="2"
                          name="conditionJson"
                          value={form.conditionJson}
                          onChange={onFormChange}
                          placeholder='{"minSubtotal": 250}'
                        />
                      </div>
                      <div className="col-12">
                        <div
                          className="form-check"
                          style={{
                            minHeight: "44px",
                            display: "flex",
                            alignItems: "center",
                          }}
                        >
                          <input
                            type="checkbox"
                            className="form-check-input"
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
                  )}
                </div>
                <div className="modal-footer border-0 py-3 px-3">
                  <button
                    type="button"
                    className="btn btn-light"
                    style={{ minHeight: "44px" }}
                    onClick={closeModal}
                  >
                    İptal
                  </button>
                  <button
                    type="submit"
                    className="btn text-white fw-semibold px-4"
                    style={{
                      background: "linear-gradient(135deg, #f57c00, #ff9800)",
                      minHeight: "44px",
                    }}
                    disabled={modalLoading}
                  >
                    {editingId ? "Güncelle" : "Kaydet"}
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
