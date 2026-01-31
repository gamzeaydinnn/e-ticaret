/**
 * AdminHomeBlocks.jsx - Ana Sayfa Ürün Blok Yönetimi
 *
 * Bu bileşen, ana sayfada gösterilecek ürün bloklarının
 * yönetimini sağlar. Her blok bir poster + ürün listesi içerir.
 *
 * Özellikler:
 * - Blok listeleme (tablo görünümü)
 * - Yeni blok ekleme
 * - Blok düzenleme
 * - Blok silme
 * - Aktif/Pasif toggle
 * - Manuel ürün seçimi
 * - Kategori bazlı otomatik ürün
 * - İndirimli ürünler otomatik
 * - Sıralama değiştirme
 *
 * @author Senior Developer
 * @version 2.0.0
 */

import React, { useState, useEffect, useCallback, useRef } from "react";
import homeBlockService, { BLOCK_TYPES } from "../../services/homeBlockService";
import bannerService, { validateFile } from "../../services/bannerService";

// ============================================
// SABİTLER
// ============================================

const INITIAL_FORM = {
  id: 0,
  name: "",
  slug: "",
  blockType: "manual",
  categoryId: null,
  posterImageUrl: "",
  backgroundColor: "#00BCD4",
  displayOrder: 0,
  maxProductCount: 6,
  viewAllUrl: "",
  viewAllText: "Tümünü Gör",
  isActive: true,
};

// ============================================
// ANA BİLEŞEN
// ============================================

export default function AdminHomeBlocks() {
  // State'ler
  const [blocks, setBlocks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [form, setForm] = useState(INITIAL_FORM);
  const [showModal, setShowModal] = useState(false);
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState({ msg: "", type: "" });
  const [categories, setCategories] = useState([]);
  const [products, setProducts] = useState([]);
  const [selectedProducts, setSelectedProducts] = useState([]);
  const [showProductModal, setShowProductModal] = useState(false);
  const [currentBlockId, setCurrentBlockId] = useState(null);
  const [productSearch, setProductSearch] = useState("");

  // Dosya yükleme state'leri
  const [imagePreview, setImagePreview] = useState("");
  const [uploading, setUploading] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef(null);
  const dragCounterRef = useRef(0);

  // ============================================
  // VERİ ÇEKME
  // ============================================

  const fetchBlocks = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await homeBlockService.getAllBlocks();
      setBlocks(data || []);
    } catch (err) {
      console.error("[AdminHomeBlocks] Veri çekme hatası:", err);
      setError(err.message || "Bloklar yüklenirken hata oluştu");
      setBlocks([]);
    } finally {
      setLoading(false);
    }
  }, []);

  const fetchCategories = useCallback(async () => {
    try {
      const response = await fetch("/api/categories");
      if (response.ok) {
        const data = await response.json();
        setCategories(data || []);
      }
    } catch (err) {
      console.error("[AdminHomeBlocks] Kategori çekme hatası:", err);
    }
  }, []);

  const fetchProducts = useCallback(async () => {
    try {
      const response = await fetch("/api/products/admin/all?size=500");
      if (response.ok) {
        const data = await response.json();
        setProducts(data?.items || data || []);
      }
    } catch (err) {
      console.error("[AdminHomeBlocks] Ürün çekme hatası:", err);
    }
  }, []);

  useEffect(() => {
    fetchBlocks();
    fetchCategories();
    fetchProducts();
  }, [fetchBlocks, fetchCategories, fetchProducts]);

  // ============================================
  // FEEDBACK
  // ============================================

  const showFeedback = useCallback((msg, type = "success") => {
    setFeedback({ msg, type });
    setTimeout(() => setFeedback({ msg: "", type: "" }), 4000);
  }, []);

  // ============================================
  // MODAL İŞLEMLERİ
  // ============================================

  const openCreateModal = () => {
    setForm({ ...INITIAL_FORM, displayOrder: blocks.length });
    setImagePreview("");
    setShowModal(true);
  };

  const openEditModal = (block) => {
    setForm({
      id: block.id,
      name: block.name || "",
      slug: block.slug || "",
      blockType: block.blockType || "manual",
      categoryId: block.categoryId || null,
      posterImageUrl: block.posterImageUrl || "",
      backgroundColor: block.backgroundColor || "#00BCD4",
      displayOrder: block.displayOrder || 0,
      maxProductCount: block.maxProductCount || 6,
      viewAllUrl: block.viewAllUrl || "",
      viewAllText: block.viewAllText || "Tümünü Gör",
      isActive: block.isActive ?? true,
    });
    setImagePreview(block.posterImageUrl || "");
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setForm(INITIAL_FORM);
    setImagePreview("");
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  // ============================================
  // DOSYA YÜKLEME İŞLEMLERİ
  // ============================================

  const handleFileUpload = useCallback(
    async (file) => {
      if (!file) return;

      // Dosya validasyonu
      const validation = validateFile(file);
      if (!validation.valid) {
        showFeedback(validation.error, "danger");
        return;
      }

      setUploading(true);
      try {
        // API'ye yükle (bannerService kullanıyoruz)
        const result = await bannerService.uploadImage(file);

        if (result && result.imageUrl) {
          setImagePreview(result.imageUrl);
          setForm((prev) => ({ ...prev, posterImageUrl: result.imageUrl }));
          showFeedback("Görsel başarıyla yüklendi", "success");
        } else {
          throw new Error("Görsel URL'si alınamadı");
        }
      } catch (err) {
        console.error("[AdminHomeBlocks] Dosya yükleme hatası:", err);
        showFeedback(err.message || "Görsel yüklenirken hata oluştu", "danger");

        // Hata durumunda local preview göster (fallback)
        const reader = new FileReader();
        reader.onload = (e) => {
          setImagePreview(e.target.result);
        };
        reader.readAsDataURL(file);
      } finally {
        setUploading(false);
      }
    },
    [showFeedback],
  );

  const handleInputFileChange = useCallback(
    (e) => {
      const file = e.target.files?.[0];
      if (file) {
        handleFileUpload(file);
      }
    },
    [handleFileUpload],
  );

  // Drag & Drop handlers
  const handleDragEnter = useCallback((e) => {
    e.preventDefault();
    e.stopPropagation();
    dragCounterRef.current++;
    if (e.dataTransfer?.items?.length > 0) {
      setIsDragging(true);
    }
  }, []);

  const handleDragLeave = useCallback((e) => {
    e.preventDefault();
    e.stopPropagation();
    dragCounterRef.current--;
    if (dragCounterRef.current === 0) {
      setIsDragging(false);
    }
  }, []);

  const handleDragOver = useCallback((e) => {
    e.preventDefault();
    e.stopPropagation();
  }, []);

  const handleDrop = useCallback(
    (e) => {
      e.preventDefault();
      e.stopPropagation();
      setIsDragging(false);
      dragCounterRef.current = 0;

      const files = e.dataTransfer?.files;
      if (files && files.length > 0) {
        handleFileUpload(files[0]);
      }
    },
    [handleFileUpload],
  );

  // ============================================
  // CRUD İŞLEMLERİ
  // ============================================

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!form.name?.trim()) {
      showFeedback("Blok adı zorunludur", "danger");
      return;
    }

    setSaving(true);
    try {
      if (form.id > 0) {
        await homeBlockService.updateBlock(form.id, form);
        showFeedback("Blok başarıyla güncellendi", "success");
      } else {
        await homeBlockService.createBlock(form);
        showFeedback("Blok başarıyla oluşturuldu", "success");
      }
      closeModal();
      await fetchBlocks();
    } catch (err) {
      console.error("[AdminHomeBlocks] Kaydetme hatası:", err);
      showFeedback(err.message || "Kaydetme sırasında hata oluştu", "danger");
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm("Bu bloğu silmek istediğinize emin misiniz?")) return;

    try {
      await homeBlockService.deleteBlock(id);
      showFeedback("Blok başarıyla silindi", "success");
      await fetchBlocks();
    } catch (err) {
      showFeedback(err.message || "Silme sırasında hata oluştu", "danger");
    }
  };

  const toggleActive = async (block) => {
    try {
      await homeBlockService.toggleBlock(block.id);
      showFeedback(
        block.isActive ? "Blok pasif yapıldı" : "Blok aktif yapıldı",
        "success",
      );
      await fetchBlocks();
    } catch (err) {
      showFeedback(err.message || "Durum güncellenirken hata oluştu", "danger");
    }
  };

  // ============================================
  // ÜRÜN SEÇİMİ
  // ============================================

  const openProductModal = async (blockId) => {
    setCurrentBlockId(blockId);
    try {
      const blockProducts = await homeBlockService.getBlockProducts(blockId);
      setSelectedProducts(blockProducts.map((p) => p.id));
    } catch {
      setSelectedProducts([]);
    }
    setShowProductModal(true);
  };

  const toggleProductSelection = (productId) => {
    setSelectedProducts((prev) =>
      prev.includes(productId)
        ? prev.filter((id) => id !== productId)
        : [...prev, productId],
    );
  };

  const saveProducts = async () => {
    try {
      await homeBlockService.setBlockProducts(currentBlockId, selectedProducts);
      showFeedback("Ürünler başarıyla kaydedildi", "success");
      setShowProductModal(false);
      await fetchBlocks();
    } catch (err) {
      showFeedback(
        err.message || "Ürünler kaydedilirken hata oluştu",
        "danger",
      );
    }
  };

  const filteredProducts = products.filter((p) =>
    p.name?.toLowerCase().includes(productSearch.toLowerCase()),
  );

  // ============================================
  // HELPER FONKSİYONLAR
  // ============================================

  const getBlockTypeLabel = (type) => {
    const found = BLOCK_TYPES.find((t) => t.value === type);
    return found?.label || type;
  };

  const getBlockTypeIcon = (type) => {
    const icons = {
      manual: "fas fa-hand-pointer",
      category: "fas fa-folder",
      discounted: "fas fa-tags",
      newest: "fas fa-clock",
      bestseller: "fas fa-fire",
    };
    return icons[type] || "fas fa-cube";
  };

  // ============================================
  // LOADING STATE
  // ============================================

  if (loading && blocks.length === 0) {
    return (
      <div className="container-fluid py-4">
        <div className="d-flex justify-content-between align-items-center mb-4">
          <div className="placeholder-glow">
            <span
              className="placeholder col-6"
              style={{ width: "250px", height: "32px" }}
            ></span>
          </div>
          <div className="placeholder-glow">
            <span
              className="placeholder"
              style={{ width: "140px", height: "42px", borderRadius: "8px" }}
            ></span>
          </div>
        </div>
        <div className="card border-0 shadow-sm">
          <div className="card-body p-4">
            {[1, 2, 3].map((i) => (
              <div key={i} className="placeholder-glow mb-3">
                <span
                  className="placeholder w-100"
                  style={{ height: "60px", borderRadius: "8px" }}
                ></span>
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  // ============================================
  // RENDER
  // ============================================

  return (
    <div className="container-fluid py-4">
      {/* Header */}
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-3">
        <div>
          <h4 className="mb-1 d-flex align-items-center gap-2">
            <i className="fas fa-th-large text-primary"></i>
            Ana Sayfa Blokları
          </h4>
          <p className="text-muted mb-0 small">
            Ana sayfada görünen poster + ürün bloklarını yönetin
          </p>
        </div>
        <div className="d-flex gap-2">
          <button
            className="btn btn-outline-secondary"
            onClick={fetchBlocks}
            disabled={loading}
          >
            <i className={`fas fa-sync-alt ${loading ? "fa-spin" : ""}`}></i>
          </button>
          <button
            className="btn btn-primary d-flex align-items-center gap-2"
            onClick={openCreateModal}
          >
            <i className="fas fa-plus"></i>
            Yeni Blok Ekle
          </button>
        </div>
      </div>

      {/* Feedback Alert */}
      {feedback.msg && (
        <div
          className={`alert alert-${feedback.type} alert-dismissible fade show`}
          role="alert"
        >
          <i
            className={`fas ${feedback.type === "success" ? "fa-check-circle" : "fa-exclamation-circle"} me-2`}
          ></i>
          {feedback.msg}
          <button
            type="button"
            className="btn-close"
            onClick={() => setFeedback({ msg: "", type: "" })}
          ></button>
        </div>
      )}

      {/* Error Alert */}
      {error && (
        <div className="alert alert-danger d-flex align-items-center gap-2">
          <i className="fas fa-exclamation-triangle"></i>
          <span>{error}</span>
          <button
            className="btn btn-sm btn-danger ms-auto"
            onClick={fetchBlocks}
          >
            Tekrar Dene
          </button>
        </div>
      )}

      {/* Blok Listesi */}
      <div className="card border-0 shadow-sm">
        <div
          className="card-header bg-gradient text-white py-3"
          style={{
            background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
          }}
        >
          <div className="d-flex align-items-center gap-2">
            <i className="fas fa-layer-group"></i>
            <span className="fw-semibold">Bloklar ({blocks.length})</span>
          </div>
        </div>
        <div className="card-body p-0">
          {blocks.length === 0 ? (
            <div className="text-center py-5">
              <i className="fas fa-inbox fa-3x text-muted mb-3"></i>
              <h5 className="text-muted">Henüz blok oluşturulmamış</h5>
              <p className="text-muted small mb-3">
                Ana sayfada gösterilecek ilk bloğunuzu oluşturun
              </p>
              <button className="btn btn-primary" onClick={openCreateModal}>
                <i className="fas fa-plus me-2"></i>
                İlk Bloğu Oluştur
              </button>
            </div>
          ) : (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead className="bg-light">
                  <tr>
                    <th style={{ width: "60px" }}>Sıra</th>
                    <th style={{ width: "80px" }}>Poster</th>
                    <th>Blok Adı</th>
                    <th style={{ width: "140px" }}>Tip</th>
                    <th style={{ width: "100px" }}>Ürün Sayısı</th>
                    <th style={{ width: "100px" }}>Durum</th>
                    <th style={{ width: "180px" }}>İşlemler</th>
                  </tr>
                </thead>
                <tbody>
                  {blocks
                    .sort((a, b) => a.displayOrder - b.displayOrder)
                    .map((block) => (
                      <tr
                        key={block.id}
                        className={!block.isActive ? "table-secondary" : ""}
                      >
                        <td>
                          <span className="badge bg-secondary rounded-pill">
                            #{block.displayOrder + 1}
                          </span>
                        </td>
                        <td>
                          {block.posterImageUrl ? (
                            <img
                              src={block.posterImageUrl}
                              alt={block.name}
                              style={{
                                width: "60px",
                                height: "60px",
                                objectFit: "cover",
                                borderRadius: "8px",
                                border: "2px solid #e5e7eb",
                              }}
                            />
                          ) : (
                            <div
                              style={{
                                width: "60px",
                                height: "60px",
                                borderRadius: "8px",
                                backgroundColor:
                                  block.backgroundColor || "#00BCD4",
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "center",
                              }}
                            >
                              <i className="fas fa-image text-white"></i>
                            </div>
                          )}
                        </td>
                        <td>
                          <div className="fw-semibold">{block.name}</div>
                          <small className="text-muted">/{block.slug}</small>
                        </td>
                        <td>
                          <span
                            className={`badge ${
                              block.blockType === "manual"
                                ? "bg-primary"
                                : block.blockType === "category"
                                  ? "bg-info"
                                  : block.blockType === "discounted"
                                    ? "bg-danger"
                                    : block.blockType === "newest"
                                      ? "bg-success"
                                      : "bg-warning"
                            }`}
                          >
                            <i
                              className={`${getBlockTypeIcon(block.blockType)} me-1`}
                            ></i>
                            {getBlockTypeLabel(block.blockType)}
                          </span>
                        </td>
                        <td>
                          <span className="badge bg-light text-dark">
                            {block.products?.length || 0} /{" "}
                            {block.maxProductCount}
                          </span>
                        </td>
                        <td>
                          <span
                            className={`badge ${block.isActive ? "bg-success" : "bg-secondary"}`}
                          >
                            {block.isActive ? "Aktif" : "Pasif"}
                          </span>
                        </td>
                        <td>
                          <div className="btn-group btn-group-sm">
                            <button
                              className="btn btn-outline-primary"
                              onClick={() => openEditModal(block)}
                              title="Düzenle"
                            >
                              <i className="fas fa-edit"></i>
                            </button>
                            {block.blockType === "manual" && (
                              <button
                                className="btn btn-outline-info"
                                onClick={() => openProductModal(block.id)}
                                title="Ürün Seç"
                              >
                                <i className="fas fa-boxes"></i>
                              </button>
                            )}
                            <button
                              className={`btn ${block.isActive ? "btn-outline-warning" : "btn-outline-success"}`}
                              onClick={() => toggleActive(block)}
                              title={block.isActive ? "Pasif Yap" : "Aktif Yap"}
                            >
                              <i
                                className={`fas ${block.isActive ? "fa-eye-slash" : "fa-eye"}`}
                              ></i>
                            </button>
                            <button
                              className="btn btn-outline-danger"
                              onClick={() => handleDelete(block.id)}
                              title="Sil"
                            >
                              <i className="fas fa-trash"></i>
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {/* Blok Tipleri Bilgi Kartı */}
      <div className="card border-0 shadow-sm mt-4">
        <div className="card-header bg-light">
          <i className="fas fa-lightbulb text-warning me-2"></i>
          <span className="fw-semibold">Blok Tipleri Hakkında</span>
        </div>
        <div className="card-body">
          <div className="row g-3">
            {BLOCK_TYPES.map((type) => (
              <div key={type.value} className="col-md-4 col-lg-2">
                <div className="d-flex align-items-start gap-2">
                  <i
                    className={`${getBlockTypeIcon(type.value)} text-${
                      type.value === "manual"
                        ? "primary"
                        : type.value === "category"
                          ? "info"
                          : type.value === "discounted"
                            ? "danger"
                            : type.value === "newest"
                              ? "success"
                              : "warning"
                    } mt-1`}
                  ></i>
                  <div>
                    <div className="fw-semibold small">{type.label}</div>
                    <small className="text-muted">{type.description}</small>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Blok Ekleme/Düzenleme Modal */}
      {showModal && (
        <div
          className="modal fade show d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-lg modal-dialog-centered">
            <div className="modal-content">
              <div className="modal-header bg-primary text-white">
                <h5 className="modal-title">
                  <i
                    className={`fas ${form.id > 0 ? "fa-edit" : "fa-plus"} me-2`}
                  ></i>
                  {form.id > 0 ? "Blok Düzenle" : "Yeni Blok Oluştur"}
                </h5>
                <button
                  type="button"
                  className="btn-close btn-close-white"
                  onClick={closeModal}
                ></button>
              </div>
              <form onSubmit={handleSubmit}>
                <div className="modal-body">
                  <div className="row g-3">
                    {/* Blok Adı */}
                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Blok Adı <span className="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        className="form-control"
                        value={form.name}
                        onChange={(e) =>
                          setForm({ ...form, name: e.target.value })
                        }
                        placeholder="Örn: İndirimli Ürünler"
                        required
                      />
                    </div>

                    {/* Slug */}
                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Slug (URL)
                      </label>
                      <input
                        type="text"
                        className="form-control"
                        value={form.slug}
                        onChange={(e) =>
                          setForm({ ...form, slug: e.target.value })
                        }
                        placeholder="Otomatik oluşturulur"
                      />
                      <small className="text-muted">
                        Boş bırakılırsa otomatik oluşturulur
                      </small>
                    </div>

                    {/* Blok Tipi */}
                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Blok Tipi
                      </label>
                      <select
                        className="form-select"
                        value={form.blockType}
                        onChange={(e) =>
                          setForm({ ...form, blockType: e.target.value })
                        }
                      >
                        {BLOCK_TYPES.map((type) => (
                          <option key={type.value} value={type.value}>
                            {type.label} - {type.description}
                          </option>
                        ))}
                      </select>
                    </div>

                    {/* Kategori (sadece category tipinde) */}
                    {form.blockType === "category" && (
                      <div className="col-md-6">
                        <label className="form-label fw-semibold">
                          Kategori
                        </label>
                        <select
                          className="form-select"
                          value={form.categoryId || ""}
                          onChange={(e) =>
                            setForm({
                              ...form,
                              categoryId: e.target.value
                                ? parseInt(e.target.value)
                                : null,
                            })
                          }
                        >
                          <option value="">Kategori seçin...</option>
                          {categories.map((cat) => (
                            <option key={cat.id} value={cat.id}>
                              {cat.name}
                            </option>
                          ))}
                        </select>
                      </div>
                    )}

                    {/* Poster Görseli - Dosya Yükleme */}
                    <div className="col-12">
                      <label className="form-label fw-semibold">
                        Poster Görseli
                      </label>
                      <div
                        className={`border rounded-3 p-4 text-center position-relative ${
                          isDragging
                            ? "border-primary bg-light"
                            : "border-dashed"
                        }`}
                        style={{
                          borderStyle: "dashed",
                          cursor: "pointer",
                          minHeight: "200px",
                          transition: "all 0.3s ease",
                        }}
                        onDragEnter={handleDragEnter}
                        onDragLeave={handleDragLeave}
                        onDragOver={handleDragOver}
                        onDrop={handleDrop}
                        onClick={() => fileInputRef.current?.click()}
                      >
                        <input
                          type="file"
                          ref={fileInputRef}
                          className="d-none"
                          accept="image/*"
                          onChange={handleInputFileChange}
                        />

                        {uploading ? (
                          <div className="py-4">
                            <div
                              className="spinner-border text-primary mb-3"
                              role="status"
                            >
                              <span className="visually-hidden">
                                Yükleniyor...
                              </span>
                            </div>
                            <p className="text-muted mb-0">
                              Görsel yükleniyor...
                            </p>
                          </div>
                        ) : imagePreview || form.posterImageUrl ? (
                          <div className="position-relative d-inline-block">
                            <img
                              src={imagePreview || form.posterImageUrl}
                              alt="Poster önizleme"
                              style={{
                                maxWidth: "100%",
                                maxHeight: "200px",
                                objectFit: "contain",
                                borderRadius: "8px",
                              }}
                            />
                            <button
                              type="button"
                              className="btn btn-danger btn-sm position-absolute top-0 end-0 m-1"
                              onClick={(e) => {
                                e.stopPropagation();
                                setImagePreview("");
                                setForm({ ...form, posterImageUrl: "" });
                              }}
                            >
                              <i className="fas fa-times"></i>
                            </button>
                          </div>
                        ) : (
                          <div className="py-4">
                            <i className="fas fa-cloud-upload-alt fa-3x text-muted mb-3"></i>
                            <p className="mb-2 text-muted">
                              <strong>Tıklayın</strong> veya{" "}
                              <strong>sürükleyip bırakın</strong>
                            </p>
                            <small className="text-muted d-block">
                              PNG, JPG, JPEG, WebP • Maks. 5MB
                            </small>
                            <div className="alert alert-info mt-3 mb-0 py-2 px-3 small">
                              <i className="fas fa-info-circle me-1"></i>
                              <strong>Önerilen Boyut:</strong> 220x320 piksel
                              (dikey poster)
                            </div>
                          </div>
                        )}

                        {isDragging && (
                          <div
                            className="position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center"
                            style={{
                              backgroundColor: "rgba(13, 110, 253, 0.1)",
                              borderRadius: "12px",
                            }}
                          >
                            <div className="text-primary">
                              <i className="fas fa-download fa-2x mb-2"></i>
                              <p className="mb-0 fw-semibold">Bırakın</p>
                            </div>
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Maksimum Ürün */}
                    <div className="col-md-4">
                      <label className="form-label fw-semibold">
                        Maks. Ürün Sayısı
                      </label>
                      <input
                        type="number"
                        className="form-control"
                        value={form.maxProductCount}
                        onChange={(e) =>
                          setForm({
                            ...form,
                            maxProductCount: parseInt(e.target.value) || 6,
                          })
                        }
                        min="1"
                        max="20"
                      />
                    </div>

                    {/* Sıralama */}
                    <div className="col-md-4">
                      <label className="form-label fw-semibold">Sıralama</label>
                      <input
                        type="number"
                        className="form-control"
                        value={form.displayOrder}
                        onChange={(e) =>
                          setForm({
                            ...form,
                            displayOrder: parseInt(e.target.value) || 0,
                          })
                        }
                        min="0"
                      />
                    </div>

                    {/* Durum */}
                    <div className="col-md-4">
                      <label className="form-label fw-semibold">Durum</label>
                      <div className="form-check form-switch mt-2">
                        <input
                          type="checkbox"
                          className="form-check-input"
                          id="isActiveSwitch"
                          checked={form.isActive}
                          onChange={(e) =>
                            setForm({ ...form, isActive: e.target.checked })
                          }
                        />
                        <label
                          className="form-check-label"
                          htmlFor="isActiveSwitch"
                        >
                          {form.isActive ? "Aktif" : "Pasif"}
                        </label>
                      </div>
                    </div>

                    {/* Tümünü Gör Linki */}
                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Tümünü Gör Linki
                      </label>
                      <input
                        type="text"
                        className="form-control"
                        value={form.viewAllUrl}
                        onChange={(e) =>
                          setForm({ ...form, viewAllUrl: e.target.value })
                        }
                        placeholder="/kategori/indirimli"
                      />
                    </div>

                    {/* Tümünü Gör Metni */}
                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Tümünü Gör Metni
                      </label>
                      <input
                        type="text"
                        className="form-control"
                        value={form.viewAllText}
                        onChange={(e) =>
                          setForm({ ...form, viewAllText: e.target.value })
                        }
                        placeholder="Tümünü Gör"
                      />
                    </div>
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={closeModal}
                  >
                    İptal
                  </button>
                  <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={saving}
                  >
                    {saving ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2"></span>
                        Kaydediliyor...
                      </>
                    ) : (
                      <>
                        <i className="fas fa-save me-2"></i>
                        Kaydet
                      </>
                    )}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {/* Ürün Seçimi Modal */}
      {showProductModal && (
        <div
          className="modal fade show d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable">
            <div className="modal-content">
              <div className="modal-header bg-info text-white">
                <h5 className="modal-title">
                  <i className="fas fa-boxes me-2"></i>
                  Ürün Seçimi ({selectedProducts.length} seçili)
                </h5>
                <button
                  type="button"
                  className="btn-close btn-close-white"
                  onClick={() => setShowProductModal(false)}
                ></button>
              </div>
              <div className="modal-body">
                {/* Arama */}
                <div className="mb-3">
                  <div className="input-group">
                    <span className="input-group-text">
                      <i className="fas fa-search"></i>
                    </span>
                    <input
                      type="text"
                      className="form-control"
                      placeholder="Ürün ara..."
                      value={productSearch}
                      onChange={(e) => setProductSearch(e.target.value)}
                    />
                  </div>
                </div>

                {/* Ürün Grid */}
                <div
                  className="row g-2"
                  style={{ maxHeight: "400px", overflowY: "auto" }}
                >
                  {filteredProducts.map((product) => (
                    <div key={product.id} className="col-6 col-md-4 col-lg-3">
                      <div
                        className={`card h-100 cursor-pointer ${selectedProducts.includes(product.id) ? "border-primary border-2" : ""}`}
                        style={{ cursor: "pointer" }}
                        onClick={() => toggleProductSelection(product.id)}
                      >
                        <div className="position-relative">
                          <img
                            src={product.imageUrl || "/placeholder.png"}
                            alt={product.name}
                            className="card-img-top"
                            style={{
                              height: "100px",
                              objectFit: "contain",
                              padding: "8px",
                            }}
                          />
                          {selectedProducts.includes(product.id) && (
                            <div className="position-absolute top-0 end-0 m-1">
                              <span className="badge bg-primary">
                                <i className="fas fa-check"></i>
                              </span>
                            </div>
                          )}
                        </div>
                        <div className="card-body p-2">
                          <p className="card-text small mb-1 text-truncate">
                            {product.name}
                          </p>
                          <small className="text-primary fw-bold">
                            {product.specialPrice || product.price}₺
                          </small>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowProductModal(false)}
                >
                  İptal
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={saveProducts}
                >
                  <i className="fas fa-save me-2"></i>
                  Kaydet ({selectedProducts.length} ürün)
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
