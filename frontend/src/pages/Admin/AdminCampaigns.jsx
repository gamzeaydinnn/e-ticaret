import React, { useEffect, useMemo, useState } from "react";
import AdminLayout from "../../components/AdminLayout";
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
    <AdminLayout>
      <div className="container-fluid p-4">
        <div className="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
          <div>
            <h1 className="h3 fw-bold mb-1" style={{ color: "#2d3748" }}>
              <i className="fas fa-gift me-2" style={{ color: "#f57c00" }}></i>
              Kampanya Yönetimi
            </h1>
            <div className="text-muted" style={{ fontSize: "0.9rem" }}>
              Kampanyalarınızı oluşturun, düzenleyin ve yönetin.
            </div>
          </div>
          <div className="d-flex gap-2">
            <input
              type="text"
              className="form-control form-control-sm"
              placeholder="Ara: kampanya adı, aktif/pasif"
              style={{ width: 240 }}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <button
              className="btn btn-sm btn-outline-secondary"
              onClick={loadCampaigns}
              disabled={loading}
            >
              Yenile
            </button>
            <button
              className="btn btn-sm text-white fw-semibold"
              style={{
                background: "linear-gradient(135deg, #f57c00, #ff9800)",
              }}
              onClick={openCreateModal}
            >
              <i className="fas fa-plus me-2"></i>
              Yeni Kampanya
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

        <div className="card border-0 shadow-sm">
          <div className="card-header bg-white d-flex justify-content-between align-items-center">
            <div>
              <i className="fas fa-layer-group me-2 text-primary"></i>
              Kampanyalar
              <small className="ms-2 text-muted">
                Toplam: {totalCount} · Aktif: {activeCount}
              </small>
            </div>
          </div>
          <div className="card-body">
            {loading ? (
              <div className="text-muted">Yükleniyor...</div>
            ) : (
              <div className="table-responsive">
                <table className="table table-sm align-middle">
                  <thead>
                    <tr>
                      <th style={{ width: 70 }}>ID</th>
                      <th>Kampanya Adı</th>
                      <th>Başlangıç</th>
                      <th>Bitiş</th>
                      <th>Açıklama</th>
                      <th>Aktif mi?</th>
                      <th style={{ width: 140 }}>İşlemler</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredCampaigns.length ? (
                      filteredCampaigns.map((campaign) => (
                        <tr key={campaign.id}>
                          <td>#{campaign.id}</td>
                          <td className="fw-semibold">{campaign.name}</td>
                          <td>{formatDate(campaign.startDate)}</td>
                          <td>{formatDate(campaign.endDate)}</td>
                          <td className="text-muted">
                            {campaign.description || "-"}
                          </td>
                          <td>
                            <span
                              className={`badge ${
                                campaign.isActive ? "bg-success" : "bg-secondary"
                              }`}
                            >
                              {campaign.isActive ? "Aktif" : "Pasif"}
                            </span>
                          </td>
                          <td>
                            <div className="d-flex gap-2">
                              <button
                                className="btn btn-outline-primary btn-sm"
                                onClick={() => openEditModal(campaign)}
                              >
                                <i className="fas fa-edit"></i>
                              </button>
                              <button
                                className="btn btn-outline-danger btn-sm"
                                onClick={() => handleDelete(campaign.id)}
                              >
                                <i className="fas fa-trash"></i>
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan="7" className="text-muted">
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

        {showModal && (
          <div
            className="modal d-block"
            style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
          >
            <div className="modal-dialog modal-lg modal-dialog-centered">
              <div className="modal-content border-0" style={{ borderRadius: 16 }}>
                <div className="modal-header border-0 p-4">
                  <h5 className="modal-title fw-bold" style={{ color: "#2d3748" }}>
                    <i
                      className="fas fa-gift me-2"
                      style={{ color: "#f57c00" }}
                    ></i>
                    {editingId ? "Kampanyayı Düzenle" : "Yeni Kampanya"}
                  </h5>
                  <button className="btn-close" onClick={closeModal}></button>
                </div>
                <form onSubmit={handleSubmit}>
                  <div className="modal-body p-4">
                    {modalLoading ? (
                      <div className="text-center text-muted py-4">
                        Bilgiler yükleniyor...
                      </div>
                    ) : (
                      <div className="row g-3">
                        <div className="col-md-6">
                          <label className="form-label fw-semibold mb-1">
                            Kampanya Adı
                          </label>
                          <input
                            className="form-control border-0 py-3"
                            style={{
                              background: "rgba(245,124,0,0.05)",
                              borderRadius: 12,
                            }}
                            name="name"
                            value={form.name}
                            onChange={onFormChange}
                            required
                            placeholder="Örn: Sepette %10 İndirim"
                          />
                        </div>
                        <div className="col-md-6">
                          <label className="form-label fw-semibold mb-1">
                            Açıklama
                          </label>
                          <input
                            className="form-control border-0 py-3"
                            style={{
                              background: "rgba(245,124,0,0.05)",
                              borderRadius: 12,
                            }}
                            name="description"
                            value={form.description}
                            onChange={onFormChange}
                            placeholder="Opsiyonel"
                          />
                        </div>
                        <div className="col-md-6">
                          <label className="form-label fw-semibold mb-1">
                            Başlangıç Tarihi
                          </label>
                          <input
                            type="date"
                            className="form-control border-0 py-3"
                            style={{
                              background: "rgba(245,124,0,0.05)",
                              borderRadius: 12,
                            }}
                            name="startDate"
                            value={form.startDate}
                            onChange={onFormChange}
                            required
                          />
                        </div>
                        <div className="col-md-6">
                          <label className="form-label fw-semibold mb-1">
                            Bitiş Tarihi
                          </label>
                          <input
                            type="date"
                            className="form-control border-0 py-3"
                            style={{
                              background: "rgba(245,124,0,0.05)",
                              borderRadius: 12,
                            }}
                            name="endDate"
                            value={form.endDate}
                            onChange={onFormChange}
                            required
                          />
                        </div>
                        <div className="col-md-6">
                          <label className="form-label fw-semibold mb-1">
                            Kural (JSON)
                          </label>
                          <textarea
                            className="form-control border-0"
                            rows="3"
                            style={{
                              background: "rgba(245,124,0,0.05)",
                              borderRadius: 12,
                            }}
                            name="conditionJson"
                            value={form.conditionJson}
                            onChange={onFormChange}
                            placeholder='Örn: {"minSubtotal": 250}'
                          />
                          <small className="text-muted">
                            Opsiyonel. Tek bir koşul JSON'u girilebilir.
                          </small>
                        </div>
                        <div className="col-md-3">
                          <label className="form-label fw-semibold mb-1">
                            Ödül Türü
                          </label>
                          <select
                            className="form-select border-0 py-3"
                            style={{
                              background: "rgba(245,124,0,0.05)",
                              borderRadius: 12,
                            }}
                            name="rewardType"
                            value={form.rewardType}
                            onChange={onFormChange}
                          >
                            <option value="Percent">% İndirim</option>
                            <option value="Amount">Tutar İndirimi</option>
                            <option value="FreeShipping">Ücretsiz Kargo</option>
                          </select>
                        </div>
                        <div className="col-md-3">
                          <label className="form-label fw-semibold mb-1">
                            Ödül Değeri
                          </label>
                          <input
                            type="number"
                            className="form-control border-0 py-3"
                            style={{
                              background: "rgba(245,124,0,0.05)",
                              borderRadius: 12,
                            }}
                            name="rewardValue"
                            value={form.rewardValue}
                            onChange={onFormChange}
                            min={0}
                            step="0.01"
                            disabled={form.rewardType === "FreeShipping"}
                            placeholder={
                              rewardLabels[form.rewardType] || "Ödül değeri"
                            }
                          />
                        </div>
                        <div className="col-md-12 d-flex align-items-center pt-2">
                          <div className="form-check">
                            <input
                              type="checkbox"
                              className="form-check-input"
                              id="campaignIsActive"
                              name="isActive"
                              checked={form.isActive}
                              onChange={onFormChange}
                            />
                            <label
                              className="form-check-label fw-semibold"
                              htmlFor="campaignIsActive"
                            >
                              Aktif mi?
                            </label>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                  <div className="modal-footer border-0 p-4">
                    <button type="button" className="btn btn-light" onClick={closeModal}>
                      İptal
                    </button>
                    <button
                      type="submit"
                      className="btn text-white fw-semibold px-4"
                      style={{
                        background: "linear-gradient(135deg, #f57c00, #ff9800)",
                        borderRadius: 8,
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
    </AdminLayout>
  );
}
