import React, { useEffect, useMemo, useState, useCallback } from "react";
import { AdminService } from "../../services/adminService";

// =============================================================================
// Kampanya Yönetimi - Admin Panel
// =============================================================================
// Bu component, gelişmiş kampanya yönetim sistemini sağlar.
// Desteklenen kampanya türleri:
// - Percentage: Yüzdelik indirim (%10, %20 vb.)
// - FixedAmount: Sabit tutar indirim (50 TL, 100 TL vb.)
// - BuyXPayY: X al Y öde (3 al 2 öde vb.)
// - FreeShipping: Ücretsiz kargo
//
// Kampanya hedefleri:
// - All: Tüm ürünler
// - Category: Belirli kategoriler
// - Product: Belirli ürünler
// =============================================================================

// Kampanya türleri - Backend enum ile uyumlu
const CAMPAIGN_TYPES = {
  Percentage: {
    value: 0,
    label: "Yüzde İndirim",
    icon: "fa-percent",
    color: "danger",
  },
  FixedAmount: {
    value: 1,
    label: "Sabit Tutar İndirim",
    icon: "fa-tag",
    color: "warning",
  },
  BuyXPayY: {
    value: 2,
    label: "X Al Y Öde",
    icon: "fa-shopping-basket",
    color: "success",
  },
  FreeShipping: {
    value: 3,
    label: "Ücretsiz Kargo",
    icon: "fa-truck",
    color: "info",
  },
};

// Hedef türleri - Backend enum ile uyumlu
const TARGET_TYPES = {
  All: { value: 0, label: "Tüm Ürünler", icon: "fa-globe" },
  Category: { value: 1, label: "Kategori Bazlı", icon: "fa-folder" },
  Product: { value: 2, label: "Ürün Bazlı", icon: "fa-box" },
};

// Form başlangıç değerleri
const initialForm = {
  id: 0,
  name: "",
  description: "",
  startDate: "",
  endDate: "",
  isActive: true,
  // Yeni alanlar
  type: 0, // Percentage
  targetType: 0, // All
  discountValue: "",
  maxDiscountAmount: "",
  minCartTotal: "",
  minQuantity: "",
  buyQty: "3",
  payQty: "2",
  priority: "100",
  isStackable: true,
  targetIds: [],
  // Geriye dönük uyumluluk
  conditionJson: "",
  rewardType: "Percent",
  rewardValue: "",
};

// Tarih formatları
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

// Kampanya türü adını getir
const getCampaignTypeName = (type) => {
  const typeNum = typeof type === "string" ? parseInt(type) : type;
  const entry = Object.entries(CAMPAIGN_TYPES).find(
    ([_, v]) => v.value === typeNum,
  );
  return entry ? entry[1].label : "Bilinmiyor";
};

// Hedef türü adını getir
const getTargetTypeName = (targetType) => {
  const typeNum =
    typeof targetType === "string" ? parseInt(targetType) : targetType;
  const entry = Object.entries(TARGET_TYPES).find(
    ([_, v]) => v.value === typeNum,
  );
  return entry ? entry[1].label : "Bilinmiyor";
};

// Kampanya türü badge rengi
const getCampaignTypeColor = (type) => {
  const typeNum = typeof type === "string" ? parseInt(type) : type;
  const entry = Object.entries(CAMPAIGN_TYPES).find(
    ([_, v]) => v.value === typeNum,
  );
  return entry ? entry[1].color : "secondary";
};

export default function AdminCampaigns() {
  // State tanımlamaları
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

  // Ürün ve kategori listeleri (hedef seçimi için)
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [productsLoading, setProductsLoading] = useState(false);

  // Kampanyaları yükle
  const loadCampaigns = useCallback(async () => {
    try {
      setLoading(true);
      setError("");
      const data = await AdminService.getCampaigns();
      setCampaigns(Array.isArray(data) ? data : []);
    } catch (err) {
      setError(err.message || "Kampanyalar yüklenemedi.");
      console.error("Kampanya yükleme hatası:", err);
    } finally {
      setLoading(false);
    }
  }, []);

  // Ürün ve kategorileri yükle
  const loadProductsAndCategories = useCallback(async () => {
    try {
      setProductsLoading(true);
      const [productsData, categoriesData] = await Promise.all([
        AdminService.getCampaignProducts?.() ||
          AdminService.getProducts?.() ||
          [],
        AdminService.getCampaignCategories?.() ||
          AdminService.getCategories?.() ||
          [],
      ]);
      setProducts(Array.isArray(productsData) ? productsData : []);
      setCategories(Array.isArray(categoriesData) ? categoriesData : []);
    } catch (err) {
      console.error("Ürün/kategori yükleme hatası:", err);
    } finally {
      setProductsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadCampaigns();
    loadProductsAndCategories();
  }, [loadCampaigns, loadProductsAndCategories]);

  // Filtrelenmiş kampanyalar
  const filteredCampaigns = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) return campaigns;
    return campaigns.filter((campaign) => {
      if (campaign.name?.toLowerCase().includes(query)) return true;
      if (campaign.description?.toLowerCase().includes(query)) return true;
      if (query === "aktif" && campaign.isActive) return true;
      if (query === "pasif" && !campaign.isActive) return true;
      // Tür araması
      const typeName = getCampaignTypeName(campaign.type).toLowerCase();
      if (typeName.includes(query)) return true;
      return false;
    });
  }, [campaigns, search]);

  // Modal açma - Yeni kampanya
  function openCreateModal() {
    setEditingId(null);
    setForm(initialForm);
    setShowModal(true);
    setModalLoading(false);
  }

  // Modal açma - Düzenleme
  async function openEditModal(campaign) {
    setEditingId(campaign.id);
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
        // Yeni alanlar
        type: detail.type ?? 0,
        targetType: detail.targetType ?? 0,
        discountValue: detail.discountValue?.toString() || "",
        maxDiscountAmount: detail.maxDiscountAmount?.toString() || "",
        minCartTotal: detail.minCartTotal?.toString() || "",
        minQuantity: detail.minQuantity?.toString() || "",
        buyQty: detail.buyQty?.toString() || "3",
        payQty: detail.payQty?.toString() || "2",
        priority: detail.priority?.toString() || "100",
        isStackable: detail.isStackable ?? true,
        targetIds: detail.targets?.map((t) => t.targetId) || [],
        // Geriye dönük uyumluluk
        conditionJson: detail.conditionJson || "",
        rewardType: detail.rewardType || "Percent",
        rewardValue: detail.rewardValue?.toString() || "",
      });
    } catch (err) {
      setMessage(err.message || "Kampanya bilgileri yüklenemedi.");
      setMessageType("danger");
      setShowModal(false);
    } finally {
      setModalLoading(false);
    }
  }

  // Modal kapatma
  function closeModal() {
    setShowModal(false);
    setModalLoading(false);
    setEditingId(null);
    setForm(initialForm);
  }

  // Form değişikliği
  const onFormChange = (event) => {
    const { name, value, type, checked } = event.target;
    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  // Hedef ID'leri değişikliği (multi-select)
  const onTargetIdsChange = (selectedIds) => {
    setForm((prev) => ({
      ...prev,
      targetIds: selectedIds,
    }));
  };

  // Payload oluştur
  function buildPayload() {
    if (!form.startDate || !form.endDate) {
      throw new Error("Başlangıç ve bitiş tarihleri zorunludur.");
    }

    const typeNum = parseInt(form.type);
    const targetTypeNum = parseInt(form.targetType);

    // Kampanya türüne göre değer validasyonu
    let discountValue = 0;
    if (typeNum === 0) {
      // Percentage
      discountValue = parseFloat(form.discountValue) || 0;
      if (discountValue <= 0 || discountValue > 100) {
        throw new Error("Yüzde indirim 1-100 arasında olmalıdır.");
      }
    } else if (typeNum === 1) {
      // FixedAmount
      discountValue = parseFloat(form.discountValue) || 0;
      if (discountValue <= 0) {
        throw new Error("İndirim tutarı 0'dan büyük olmalıdır.");
      }
    } else if (typeNum === 2) {
      // BuyXPayY
      const buyQty = parseInt(form.buyQty) || 0;
      const payQty = parseInt(form.payQty) || 0;
      if (buyQty < 2 || payQty < 1 || payQty >= buyQty) {
        throw new Error(
          "X Al Y Öde için geçerli değerler giriniz (X > Y >= 1).",
        );
      }
    }

    // Hedef tipi kontrolü
    if (
      targetTypeNum !== 0 &&
      (!form.targetIds || form.targetIds.length === 0)
    ) {
      throw new Error(
        "Kategori veya ürün bazlı kampanyalar için en az bir hedef seçilmelidir.",
      );
    }

    return {
      name: (form.name || "").trim(),
      description: form.description?.trim() || null,
      startDate: new Date(`${form.startDate}T00:00:00`),
      endDate: new Date(`${form.endDate}T23:59:59`),
      isActive: !!form.isActive,
      // Yeni alanlar
      type: typeNum,
      targetType: targetTypeNum,
      discountValue: discountValue,
      maxDiscountAmount: form.maxDiscountAmount
        ? parseFloat(form.maxDiscountAmount)
        : null,
      minCartTotal: form.minCartTotal ? parseFloat(form.minCartTotal) : null,
      minQuantity: form.minQuantity ? parseInt(form.minQuantity) : null,
      buyQty: typeNum === 2 ? parseInt(form.buyQty) : null,
      payQty: typeNum === 2 ? parseInt(form.payQty) : null,
      priority: parseInt(form.priority) || 100,
      isStackable: !!form.isStackable,
      targetIds: targetTypeNum !== 0 ? form.targetIds : null,
      // Geriye dönük uyumluluk
      conditionJson: form.conditionJson?.trim() || null,
      rewardType: form.rewardType || "Percent",
      rewardValue: discountValue,
    };
  }

  // Form gönderimi
  async function handleSubmit(event) {
    event.preventDefault();
    try {
      if (!form.name.trim()) {
        throw new Error("Kampanya adı zorunludur.");
      }
      const payload = buildPayload();

      if (editingId) {
        await AdminService.updateCampaign(editingId, payload);
        setMessage("Kampanya başarıyla güncellendi.");
      } else {
        await AdminService.createCampaign(payload);
        setMessage("Kampanya başarıyla oluşturuldu.");
      }
      setMessageType("success");
      await loadCampaigns();
      closeModal();
    } catch (err) {
      setMessage(err.message || "İşlem gerçekleştirilemedi.");
      setMessageType("danger");
    } finally {
      setTimeout(() => setMessage(""), 4000);
    }
  }

  // Kampanya silme
  async function handleDelete(id) {
    const confirmed = window.confirm(
      "Bu kampanyayı silmek istediğinize emin misiniz?\nBu işlem geri alınamaz.",
    );
    if (!confirmed) return;

    try {
      await AdminService.deleteCampaign(id);
      setMessage("Kampanya başarıyla silindi.");
      setMessageType("success");
      await loadCampaigns();
    } catch (err) {
      setMessage(err.message || "Kampanya silinemedi.");
      setMessageType("danger");
    } finally {
      setTimeout(() => setMessage(""), 3000);
    }
  }

  // Kampanya durumunu değiştir
  async function handleToggleStatus(id) {
    try {
      await AdminService.toggleCampaignStatus?.(id);
      setMessage("Kampanya durumu güncellendi.");
      setMessageType("success");
      await loadCampaigns();
    } catch (err) {
      setMessage(err.message || "Durum güncellenemedi.");
      setMessageType("danger");
    } finally {
      setTimeout(() => setMessage(""), 3000);
    }
  }

  // İstatistikler
  const totalCount = campaigns.length;
  const activeCount = campaigns.filter((c) => c.isActive).length;

  // Kampanya türüne göre indirim gösterimi
  const getDiscountDisplay = (campaign) => {
    const type =
      typeof campaign.type === "string"
        ? parseInt(campaign.type)
        : campaign.type;
    switch (type) {
      case 0: // Percentage
        return `%${campaign.discountValue || 0}`;
      case 1: // FixedAmount
        return `₺${campaign.discountValue || 0}`;
      case 2: // BuyXPayY
        return `${campaign.buyQty || 3} Al ${campaign.payQty || 2} Öde`;
      case 3: // FreeShipping
        return "Ücretsiz Kargo";
      default:
        return "-";
    }
  };

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
            İndirim kampanyalarını oluşturun ve yönetin
          </p>
        </div>
        <div className="d-flex flex-column flex-sm-row gap-2 w-100 w-md-auto">
          <input
            type="text"
            className="form-control form-control-sm"
            placeholder="Ara: ad, tür, aktif/pasif"
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
              title="Yenile"
            >
              <i className={`fas fa-sync-alt ${loading ? "fa-spin" : ""}`}></i>
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

      {/* Mesajlar */}
      {error && (
        <div
          className="alert alert-danger py-2 d-flex align-items-center"
          style={{ fontSize: "0.85rem" }}
        >
          <i className="fas fa-exclamation-circle me-2"></i>
          {error}
        </div>
      )}
      {message && (
        <div
          className={`alert alert-${messageType} py-2 d-flex align-items-center`}
          style={{ fontSize: "0.85rem" }}
        >
          <i
            className={`fas ${messageType === "success" ? "fa-check-circle" : "fa-exclamation-triangle"} me-2`}
          ></i>
          {message}
        </div>
      )}

      {/* İstatistik Kartları - Mobilde gizli */}
      <div className="row g-2 mb-3 d-none d-md-flex">
        <div className="col-md-3">
          <div
            className="card border-0 shadow-sm h-100"
            style={{ borderRadius: "10px" }}
          >
            <div className="card-body py-2 px-3">
              <div className="d-flex align-items-center">
                <div
                  className="rounded-circle p-2 me-2"
                  style={{ backgroundColor: "#e3f2fd" }}
                >
                  <i className="fas fa-gift text-primary"></i>
                </div>
                <div>
                  <div className="text-muted" style={{ fontSize: "0.75rem" }}>
                    Toplam
                  </div>
                  <div className="fw-bold">{totalCount}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div
            className="card border-0 shadow-sm h-100"
            style={{ borderRadius: "10px" }}
          >
            <div className="card-body py-2 px-3">
              <div className="d-flex align-items-center">
                <div
                  className="rounded-circle p-2 me-2"
                  style={{ backgroundColor: "#e8f5e9" }}
                >
                  <i className="fas fa-check-circle text-success"></i>
                </div>
                <div>
                  <div className="text-muted" style={{ fontSize: "0.75rem" }}>
                    Aktif
                  </div>
                  <div className="fw-bold text-success">{activeCount}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div
            className="card border-0 shadow-sm h-100"
            style={{ borderRadius: "10px" }}
          >
            <div className="card-body py-2 px-3">
              <div className="d-flex align-items-center">
                <div
                  className="rounded-circle p-2 me-2"
                  style={{ backgroundColor: "#fff3e0" }}
                >
                  <i className="fas fa-pause-circle text-warning"></i>
                </div>
                <div>
                  <div className="text-muted" style={{ fontSize: "0.75rem" }}>
                    Pasif
                  </div>
                  <div className="fw-bold text-warning">
                    {totalCount - activeCount}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div
            className="card border-0 shadow-sm h-100"
            style={{ borderRadius: "10px" }}
          >
            <div className="card-body py-2 px-3">
              <div className="d-flex align-items-center">
                <div
                  className="rounded-circle p-2 me-2"
                  style={{ backgroundColor: "#fce4ec" }}
                >
                  <i className="fas fa-percent text-danger"></i>
                </div>
                <div>
                  <div className="text-muted" style={{ fontSize: "0.75rem" }}>
                    Sonuç
                  </div>
                  <div className="fw-bold">{filteredCampaigns.length}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Kampanya Listesi */}
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
            <div className="text-center py-4">
              <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">Yükleniyor...</span>
              </div>
              <div className="text-muted mt-2" style={{ fontSize: "0.85rem" }}>
                Kampanyalar yükleniyor...
              </div>
            </div>
          ) : (
            <div className="table-responsive">
              <table
                className="table table-sm table-hover mb-0 admin-mobile-table"
                style={{ fontSize: "0.8rem" }}
              >
                <thead className="bg-light">
                  <tr>
                    <th className="px-2">ID</th>
                    <th className="px-2">Ad</th>
                    <th className="px-2">Tür</th>
                    <th className="px-2 d-none d-lg-table-cell">Hedef</th>
                    <th className="px-2 d-none d-md-table-cell">İndirim</th>
                    <th className="px-2 d-none d-lg-table-cell">Tarih</th>
                    <th className="px-2">Durum</th>
                    <th className="px-2 text-end">İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredCampaigns.length ? (
                    filteredCampaigns.map((c) => (
                      <tr key={c.id}>
                        <td data-label="ID" className="px-2">
                          <span className="badge bg-light text-dark">
                            #{c.id}
                          </span>
                        </td>
                        <td
                          data-label="Ad"
                          className="px-2 fw-semibold"
                          style={{ maxWidth: "200px" }}
                        >
                          <div className="text-truncate" title={c.name}>
                            {c.name}
                          </div>
                          {c.description && (
                            <small
                              className="text-muted d-block text-truncate"
                              style={{ fontSize: "0.7rem" }}
                            >
                              {c.description}
                            </small>
                          )}
                        </td>
                        <td data-label="Tür" className="px-2">
                          <span
                            className={`badge bg-${getCampaignTypeColor(c.type)}`}
                            style={{ fontSize: "0.7rem" }}
                          >
                            {getCampaignTypeName(c.type)}
                          </span>
                        </td>
                        <td
                          data-label="Hedef"
                          className="px-2 d-none d-lg-table-cell"
                        >
                          <span
                            className="text-muted"
                            style={{ fontSize: "0.75rem" }}
                          >
                            {getTargetTypeName(c.targetType)}
                          </span>
                        </td>
                        <td
                          data-label="İndirim"
                          className="px-2 d-none d-md-table-cell fw-semibold"
                        >
                          {getDiscountDisplay(c)}
                        </td>
                        <td
                          data-label="Tarih"
                          className="px-2 d-none d-lg-table-cell"
                          style={{ fontSize: "0.75rem" }}
                        >
                          <div>{formatDate(c.startDate)}</div>
                          <div className="text-muted">
                            {formatDate(c.endDate)}
                          </div>
                        </td>
                        <td data-label="Durum" className="px-2">
                          <span
                            className={`badge ${c.isActive ? "bg-success" : "bg-secondary"}`}
                            style={{ fontSize: "0.7rem", cursor: "pointer" }}
                            onClick={() => handleToggleStatus(c.id)}
                            title="Durumu değiştirmek için tıklayın"
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
                              title="Düzenle"
                            >
                              <i className="fas fa-edit"></i>
                            </button>
                            <button
                              className="btn btn-outline-danger btn-sm"
                              style={{ minWidth: "36px", minHeight: "36px" }}
                              onClick={() => handleDelete(c.id)}
                              title="Sil"
                            >
                              <i className="fas fa-trash"></i>
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan="8" className="text-muted text-center py-4">
                        <i className="fas fa-inbox fa-2x mb-2 d-block text-secondary"></i>
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

      {/* Modal - Kampanya Formu */}
      {showModal && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
          onClick={(e) => e.target === e.currentTarget && closeModal()}
        >
          <div className="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable modal-fullscreen-sm-down">
            <div
              className="modal-content border-0"
              style={{ borderRadius: "16px" }}
            >
              <div
                className="modal-header border-0 py-3 px-3"
                style={{
                  background: "linear-gradient(135deg, #f57c00, #ff9800)",
                }}
              >
                <h5
                  className="modal-title fw-bold text-white"
                  style={{ fontSize: "1rem" }}
                >
                  <i className="fas fa-gift me-2"></i>
                  {editingId ? "Kampanyayı Düzenle" : "Yeni Kampanya Oluştur"}
                </h5>
                <button
                  className="btn-close btn-close-white"
                  onClick={closeModal}
                  aria-label="Kapat"
                ></button>
              </div>
              <form onSubmit={handleSubmit} className="admin-mobile-form">
                <div
                  className="modal-body p-3"
                  style={{ maxHeight: "70vh", overflowY: "auto" }}
                >
                  {modalLoading ? (
                    <div className="text-center py-4">
                      <div
                        className="spinner-border text-primary"
                        role="status"
                      >
                        <span className="visually-hidden">Yükleniyor...</span>
                      </div>
                      <div className="text-muted mt-2">
                        Kampanya bilgileri yükleniyor...
                      </div>
                    </div>
                  ) : (
                    <div className="row g-3">
                      {/* Temel Bilgiler */}
                      <div className="col-12">
                        <div className="card border shadow-sm">
                          <div className="card-header bg-light py-2">
                            <i className="fas fa-info-circle me-2 text-primary"></i>
                            <span
                              className="fw-semibold"
                              style={{ fontSize: "0.9rem" }}
                            >
                              Temel Bilgiler
                            </span>
                          </div>
                          <div className="card-body">
                            <div className="row g-2">
                              <div className="col-12">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Kampanya Adı{" "}
                                  <span className="text-danger">*</span>
                                </label>
                                <input
                                  className="form-control"
                                  style={{ minHeight: "44px" }}
                                  name="name"
                                  value={form.name}
                                  onChange={onFormChange}
                                  required
                                  placeholder="Örn: Yaz İndirimi"
                                />
                              </div>
                              <div className="col-12">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Açıklama
                                </label>
                                <textarea
                                  className="form-control"
                                  rows="2"
                                  name="description"
                                  value={form.description}
                                  onChange={onFormChange}
                                  placeholder="Kampanya açıklaması (opsiyonel)"
                                />
                              </div>
                              <div className="col-6">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Başlangıç{" "}
                                  <span className="text-danger">*</span>
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
                                  Bitiş <span className="text-danger">*</span>
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
                            </div>
                          </div>
                        </div>
                      </div>

                      {/* Kampanya Türü */}
                      <div className="col-12">
                        <div className="card border shadow-sm">
                          <div className="card-header bg-light py-2">
                            <i className="fas fa-tags me-2 text-warning"></i>
                            <span
                              className="fw-semibold"
                              style={{ fontSize: "0.9rem" }}
                            >
                              Kampanya Türü
                            </span>
                          </div>
                          <div className="card-body">
                            <div className="row g-2">
                              <div className="col-md-6">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Tür <span className="text-danger">*</span>
                                </label>
                                <select
                                  className="form-select"
                                  style={{ minHeight: "44px" }}
                                  name="type"
                                  value={form.type}
                                  onChange={onFormChange}
                                >
                                  {Object.entries(CAMPAIGN_TYPES).map(
                                    ([key, val]) => (
                                      <option key={key} value={val.value}>
                                        {val.label}
                                      </option>
                                    ),
                                  )}
                                </select>
                              </div>

                              {/* Yüzde veya Sabit Tutar için İndirim Değeri */}
                              {(parseInt(form.type) === 0 ||
                                parseInt(form.type) === 1) && (
                                <div className="col-md-6">
                                  <label
                                    className="form-label fw-semibold mb-1"
                                    style={{ fontSize: "0.85rem" }}
                                  >
                                    {parseInt(form.type) === 0
                                      ? "İndirim Yüzdesi (%)"
                                      : "İndirim Tutarı (₺)"}{" "}
                                    <span className="text-danger">*</span>
                                  </label>
                                  <div className="input-group">
                                    <input
                                      type="number"
                                      className="form-control"
                                      style={{ minHeight: "44px" }}
                                      name="discountValue"
                                      value={form.discountValue}
                                      onChange={onFormChange}
                                      min={parseInt(form.type) === 0 ? 1 : 0.01}
                                      max={
                                        parseInt(form.type) === 0 ? 100 : 100000
                                      }
                                      step={
                                        parseInt(form.type) === 0 ? 1 : 0.01
                                      }
                                      required
                                    />
                                    <span className="input-group-text">
                                      {parseInt(form.type) === 0 ? "%" : "₺"}
                                    </span>
                                  </div>
                                </div>
                              )}

                              {/* Yüzde indirim için maksimum limit */}
                              {parseInt(form.type) === 0 && (
                                <div className="col-md-6">
                                  <label
                                    className="form-label fw-semibold mb-1"
                                    style={{ fontSize: "0.85rem" }}
                                  >
                                    Maksimum İndirim (₺)
                                  </label>
                                  <input
                                    type="number"
                                    className="form-control"
                                    style={{ minHeight: "44px" }}
                                    name="maxDiscountAmount"
                                    value={form.maxDiscountAmount}
                                    onChange={onFormChange}
                                    min={0}
                                    step={0.01}
                                    placeholder="Opsiyonel"
                                  />
                                  <small className="text-muted">
                                    Yüzde indirimin üst limiti
                                  </small>
                                </div>
                              )}

                              {/* X Al Y Öde için */}
                              {parseInt(form.type) === 2 && (
                                <>
                                  <div className="col-6">
                                    <label
                                      className="form-label fw-semibold mb-1"
                                      style={{ fontSize: "0.85rem" }}
                                    >
                                      Al (X){" "}
                                      <span className="text-danger">*</span>
                                    </label>
                                    <input
                                      type="number"
                                      className="form-control"
                                      style={{ minHeight: "44px" }}
                                      name="buyQty"
                                      value={form.buyQty}
                                      onChange={onFormChange}
                                      min={2}
                                      max={100}
                                      required
                                    />
                                    <small className="text-muted">
                                      Alınması gereken adet
                                    </small>
                                  </div>
                                  <div className="col-6">
                                    <label
                                      className="form-label fw-semibold mb-1"
                                      style={{ fontSize: "0.85rem" }}
                                    >
                                      Öde (Y){" "}
                                      <span className="text-danger">*</span>
                                    </label>
                                    <input
                                      type="number"
                                      className="form-control"
                                      style={{ minHeight: "44px" }}
                                      name="payQty"
                                      value={form.payQty}
                                      onChange={onFormChange}
                                      min={1}
                                      max={parseInt(form.buyQty) - 1 || 99}
                                      required
                                    />
                                    <small className="text-muted">
                                      Ödenecek adet
                                    </small>
                                  </div>
                                  <div className="col-12">
                                    <div
                                      className="alert alert-info py-2 mb-0"
                                      style={{ fontSize: "0.8rem" }}
                                    >
                                      <i className="fas fa-info-circle me-2"></i>
                                      <strong>
                                        {form.buyQty} Al {form.payQty} Öde:
                                      </strong>{" "}
                                      Müşteri {form.buyQty} adet alırsa{" "}
                                      {parseInt(form.buyQty) -
                                        parseInt(form.payQty)}{" "}
                                      tanesi bedava!
                                    </div>
                                  </div>
                                </>
                              )}

                              {/* Ücretsiz Kargo bilgisi */}
                              {parseInt(form.type) === 3 && (
                                <div className="col-12">
                                  <div
                                    className="alert alert-success py-2 mb-0"
                                    style={{ fontSize: "0.8rem" }}
                                  >
                                    <i className="fas fa-truck me-2"></i>
                                    Ücretsiz kargo kampanyası aktif edilecek.
                                  </div>
                                </div>
                              )}
                            </div>
                          </div>
                        </div>
                      </div>

                      {/* Hedef Seçimi */}
                      <div className="col-12">
                        <div className="card border shadow-sm">
                          <div className="card-header bg-light py-2">
                            <i className="fas fa-bullseye me-2 text-success"></i>
                            <span
                              className="fw-semibold"
                              style={{ fontSize: "0.9rem" }}
                            >
                              Kampanya Hedefi
                            </span>
                          </div>
                          <div className="card-body">
                            <div className="row g-2">
                              <div className="col-12">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Hedef Türü
                                </label>
                                <select
                                  className="form-select"
                                  style={{ minHeight: "44px" }}
                                  name="targetType"
                                  value={form.targetType}
                                  onChange={onFormChange}
                                >
                                  {Object.entries(TARGET_TYPES).map(
                                    ([key, val]) => (
                                      <option key={key} value={val.value}>
                                        {val.label}
                                      </option>
                                    ),
                                  )}
                                </select>
                              </div>

                              {/* Kategori seçimi */}
                              {parseInt(form.targetType) === 1 && (
                                <div className="col-12">
                                  <label
                                    className="form-label fw-semibold mb-1"
                                    style={{ fontSize: "0.85rem" }}
                                  >
                                    Kategoriler{" "}
                                    <span className="text-danger">*</span>
                                  </label>
                                  {productsLoading ? (
                                    <div className="text-muted">
                                      Kategoriler yükleniyor...
                                    </div>
                                  ) : (
                                    <select
                                      className="form-select"
                                      style={{ minHeight: "120px" }}
                                      multiple
                                      value={form.targetIds.map(String)}
                                      onChange={(e) => {
                                        const selected = Array.from(
                                          e.target.selectedOptions,
                                          (opt) => parseInt(opt.value),
                                        );
                                        onTargetIdsChange(selected);
                                      }}
                                    >
                                      {categories.map((cat) => (
                                        <option key={cat.id} value={cat.id}>
                                          {cat.name}
                                        </option>
                                      ))}
                                    </select>
                                  )}
                                  <small className="text-muted">
                                    Ctrl/Cmd tuşuyla birden fazla seçebilirsiniz
                                  </small>
                                </div>
                              )}

                              {/* Ürün seçimi */}
                              {parseInt(form.targetType) === 2 && (
                                <div className="col-12">
                                  <label
                                    className="form-label fw-semibold mb-1"
                                    style={{ fontSize: "0.85rem" }}
                                  >
                                    Ürünler{" "}
                                    <span className="text-danger">*</span>
                                  </label>
                                  {productsLoading ? (
                                    <div className="text-muted">
                                      Ürünler yükleniyor...
                                    </div>
                                  ) : (
                                    <select
                                      className="form-select"
                                      style={{ minHeight: "120px" }}
                                      multiple
                                      value={form.targetIds.map(String)}
                                      onChange={(e) => {
                                        const selected = Array.from(
                                          e.target.selectedOptions,
                                          (opt) => parseInt(opt.value),
                                        );
                                        onTargetIdsChange(selected);
                                      }}
                                    >
                                      {products.map((prod) => (
                                        <option key={prod.id} value={prod.id}>
                                          {prod.name} - ₺{prod.price}
                                        </option>
                                      ))}
                                    </select>
                                  )}
                                  <small className="text-muted">
                                    Ctrl/Cmd tuşuyla birden fazla seçebilirsiniz
                                  </small>
                                </div>
                              )}
                            </div>
                          </div>
                        </div>
                      </div>

                      {/* Koşullar */}
                      <div className="col-12">
                        <div className="card border shadow-sm">
                          <div className="card-header bg-light py-2">
                            <i className="fas fa-sliders-h me-2 text-info"></i>
                            <span
                              className="fw-semibold"
                              style={{ fontSize: "0.9rem" }}
                            >
                              Koşullar (Opsiyonel)
                            </span>
                          </div>
                          <div className="card-body">
                            <div className="row g-2">
                              <div className="col-md-4">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Min. Sepet Tutarı (₺)
                                </label>
                                <input
                                  type="number"
                                  className="form-control"
                                  style={{ minHeight: "44px" }}
                                  name="minCartTotal"
                                  value={form.minCartTotal}
                                  onChange={onFormChange}
                                  min={0}
                                  step={0.01}
                                  placeholder="0"
                                />
                              </div>
                              <div className="col-md-4">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Min. Ürün Adedi
                                </label>
                                <input
                                  type="number"
                                  className="form-control"
                                  style={{ minHeight: "44px" }}
                                  name="minQuantity"
                                  value={form.minQuantity}
                                  onChange={onFormChange}
                                  min={1}
                                  placeholder="1"
                                />
                              </div>
                              <div className="col-md-4">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Öncelik
                                </label>
                                <input
                                  type="number"
                                  className="form-control"
                                  style={{ minHeight: "44px" }}
                                  name="priority"
                                  value={form.priority}
                                  onChange={onFormChange}
                                  min={1}
                                  max={1000}
                                />
                                <small className="text-muted">
                                  Düşük = Yüksek öncelik
                                </small>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>

                      {/* Durum */}
                      <div className="col-12">
                        <div className="d-flex gap-3 align-items-center p-3 bg-light rounded">
                          <div className="form-check form-switch">
                            <input
                              type="checkbox"
                              className="form-check-input"
                              id="isActive"
                              name="isActive"
                              checked={form.isActive}
                              onChange={onFormChange}
                              style={{ width: "3rem", height: "1.5rem" }}
                            />
                            <label
                              className="form-check-label fw-semibold ms-2"
                              htmlFor="isActive"
                            >
                              {form.isActive ? "Aktif" : "Pasif"}
                            </label>
                          </div>
                          <div className="form-check form-switch ms-3">
                            <input
                              type="checkbox"
                              className="form-check-input"
                              id="isStackable"
                              name="isStackable"
                              checked={form.isStackable}
                              onChange={onFormChange}
                              style={{ width: "3rem", height: "1.5rem" }}
                            />
                            <label
                              className="form-check-label fw-semibold ms-2"
                              htmlFor="isStackable"
                            >
                              Birleştirilebilir
                            </label>
                          </div>
                        </div>
                      </div>
                    </div>
                  )}
                </div>
                <div className="modal-footer border-0 py-3 px-3 bg-light">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    style={{ minHeight: "44px", minWidth: "100px" }}
                    onClick={closeModal}
                  >
                    <i className="fas fa-times me-1"></i>İptal
                  </button>
                  <button
                    type="submit"
                    className="btn text-white fw-semibold px-4"
                    style={{
                      background: "linear-gradient(135deg, #f57c00, #ff9800)",
                      minHeight: "44px",
                      minWidth: "120px",
                    }}
                    disabled={modalLoading}
                  >
                    <i
                      className={`fas ${editingId ? "fa-save" : "fa-plus"} me-1`}
                    ></i>
                    {editingId ? "Güncelle" : "Oluştur"}
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
