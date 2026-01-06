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
    <div style={{ overflow: "hidden", maxWidth: "100%" }}>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-3 gap-2 px-1">
        <div>
          <h5
            className="fw-bold mb-0"
            style={{ color: "#2d3748", fontSize: "1rem" }}
          >
            <i className="fas fa-gift me-2" style={{ color: "#f57c00" }}></i>
            Kampanya Yönetimi
          </h5>
          <p
            className="text-muted mb-0 d-none d-sm-block"
            style={{ fontSize: "0.7rem" }}
          >
            Kampanyalarınızı yönetin
          </p>
        </div>
        <div className="d-flex gap-1 flex-wrap">
          <input
            type="text"
            className="form-control form-control-sm"
            placeholder="Ara..."
            style={{ width: "100px", fontSize: "0.7rem" }}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <button
            className="btn btn-sm btn-outline-secondary px-2 py-1"
            style={{ fontSize: "0.65rem" }}
            onClick={loadCampaigns}
            disabled={loading}
          >
            <i className="fas fa-sync-alt"></i>
          </button>
          <button
            className="btn btn-sm text-white fw-semibold px-2 py-1"
            style={{
              background: "linear-gradient(135deg, #f57c00, #ff9800)",
              fontSize: "0.65rem",
            }}
            onClick={openCreateModal}
          >
            <i className="fas fa-plus me-1"></i>Yeni
          </button>
        </div>
      </div>

      {error && (
        <div
          className="alert alert-danger py-2 mx-1"
          style={{ fontSize: "0.7rem" }}
        >
          {error}
        </div>
      )}
      {message && (
        <div
          className={`alert alert-${messageType} py-2 mx-1`}
          style={{ fontSize: "0.7rem" }}
        >
          {message}
        </div>
      )}

      <div
        className="card border-0 shadow-sm mx-1"
        style={{ borderRadius: "8px" }}
      >
        <div className="card-header bg-white py-2 px-2 d-flex justify-content-between align-items-center">
          <span style={{ fontSize: "0.8rem" }}>
            <i className="fas fa-layer-group me-1 text-primary"></i>Kampanyalar
          </span>
          <small className="text-muted" style={{ fontSize: "0.65rem" }}>
            Toplam: {totalCount} · Aktif: {activeCount}
          </small>
        </div>
        <div className="card-body p-0">
          {loading ? (
            <div className="text-muted small p-3">Yükleniyor...</div>
          ) : (
            <div className="table-responsive">
              <table
                className="table table-sm mb-0"
                style={{ fontSize: "0.65rem" }}
              >
                <thead className="bg-light">
                  <tr>
                    <th className="px-1">ID</th>
                    <th className="px-1">Ad</th>
                    <th className="px-1 d-none d-sm-table-cell">Tarih</th>
                    <th className="px-1">Durum</th>
                    <th className="px-1">İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredCampaigns.length ? (
                    filteredCampaigns.map((c) => (
                      <tr key={c.id}>
                        <td className="px-1">#{c.id}</td>
                        <td
                          className="px-1 fw-semibold text-truncate"
                          style={{ maxWidth: "100px" }}
                        >
                          {c.name}
                        </td>
                        <td
                          className="px-1 d-none d-sm-table-cell"
                          style={{ fontSize: "0.6rem" }}
                        >
                          {formatDate(c.startDate)} - {formatDate(c.endDate)}
                        </td>
                        <td className="px-1">
                          <span
                            className={`badge ${
                              c.isActive ? "bg-success" : "bg-secondary"
                            }`}
                            style={{ fontSize: "0.55rem" }}
                          >
                            {c.isActive ? "Aktif" : "Pasif"}
                          </span>
                        </td>
                        <td className="px-1">
                          <div className="d-flex gap-1">
                            <button
                              className="btn btn-outline-primary p-1"
                              style={{ fontSize: "0.55rem", lineHeight: 1 }}
                              onClick={() => openEditModal(c)}
                            >
                              <i className="fas fa-edit"></i>
                            </button>
                            <button
                              className="btn btn-outline-danger p-1"
                              style={{ fontSize: "0.55rem", lineHeight: 1 }}
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

      {showModal && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-dialog-centered mx-2">
            <div
              className="modal-content border-0"
              style={{ borderRadius: "12px" }}
            >
              <div className="modal-header border-0 py-2 px-3">
                <h6
                  className="modal-title fw-bold"
                  style={{ color: "#2d3748", fontSize: "0.9rem" }}
                >
                  <i
                    className="fas fa-gift me-2"
                    style={{ color: "#f57c00" }}
                  ></i>
                  {editingId ? "Düzenle" : "Yeni Kampanya"}
                </h6>
                <button
                  className="btn-close btn-close-sm"
                  onClick={closeModal}
                ></button>
              </div>
              <form onSubmit={handleSubmit}>
                <div
                  className="modal-body p-3"
                  style={{ maxHeight: "60vh", overflowY: "auto" }}
                >
                  {modalLoading ? (
                    <div className="text-center text-muted py-3">
                      Yükleniyor...
                    </div>
                  ) : (
                    <div className="row g-2">
                      <div className="col-12">
                        <label
                          className="form-label fw-semibold mb-1"
                          style={{ fontSize: "0.7rem" }}
                        >
                          Kampanya Adı
                        </label>
                        <input
                          className="form-control form-control-sm"
                          style={{ fontSize: "0.75rem" }}
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
                          style={{ fontSize: "0.7rem" }}
                        >
                          Açıklama
                        </label>
                        <input
                          className="form-control form-control-sm"
                          style={{ fontSize: "0.75rem" }}
                          name="description"
                          value={form.description}
                          onChange={onFormChange}
                          placeholder="Opsiyonel"
                        />
                      </div>
                      <div className="col-6">
                        <label
                          className="form-label fw-semibold mb-1"
                          style={{ fontSize: "0.7rem" }}
                        >
                          Başlangıç
                        </label>
                        <input
                          type="date"
                          className="form-control form-control-sm"
                          style={{ fontSize: "0.7rem" }}
                          name="startDate"
                          value={form.startDate}
                          onChange={onFormChange}
                          required
                        />
                      </div>
                      <div className="col-6">
                        <label
                          className="form-label fw-semibold mb-1"
                          style={{ fontSize: "0.7rem" }}
                        >
                          Bitiş
                        </label>
                        <input
                          type="date"
                          className="form-control form-control-sm"
                          style={{ fontSize: "0.7rem" }}
                          name="endDate"
                          value={form.endDate}
                          onChange={onFormChange}
                          required
                        />
                      </div>
                      <div className="col-6">
                        <label
                          className="form-label fw-semibold mb-1"
                          style={{ fontSize: "0.7rem" }}
                        >
                          Ödül Türü
                        </label>
                        <select
                          className="form-select form-select-sm"
                          style={{ fontSize: "0.7rem" }}
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
                          style={{ fontSize: "0.7rem" }}
                        >
                          Ödül Değeri
                        </label>
                        <input
                          type="number"
                          className="form-control form-control-sm"
                          style={{ fontSize: "0.7rem" }}
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
                          style={{ fontSize: "0.7rem" }}
                        >
                          Kural (JSON)
                        </label>
                        <textarea
                          className="form-control form-control-sm"
                          rows="2"
                          style={{ fontSize: "0.7rem" }}
                          name="conditionJson"
                          value={form.conditionJson}
                          onChange={onFormChange}
                          placeholder='{"minSubtotal": 250}'
                        />
                      </div>
                      <div className="col-12">
                        <div className="form-check">
                          <input
                            type="checkbox"
                            className="form-check-input"
                            name="isActive"
                            checked={form.isActive}
                            onChange={onFormChange}
                          />
                          <label
                            className="form-check-label"
                            style={{ fontSize: "0.75rem" }}
                          >
                            Aktif
                          </label>
                        </div>
                      </div>
                    </div>
                  )}
                </div>
                <div className="modal-footer border-0 py-2 px-3">
                  <button
                    type="button"
                    className="btn btn-light btn-sm"
                    style={{ fontSize: "0.7rem" }}
                    onClick={closeModal}
                  >
                    İptal
                  </button>
                  <button
                    type="submit"
                    className="btn btn-sm text-white fw-semibold px-3"
                    style={{
                      background: "linear-gradient(135deg, #f57c00, #ff9800)",
                      fontSize: "0.7rem",
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
