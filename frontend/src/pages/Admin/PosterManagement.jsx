/**
 * PosterManagement.jsx - Admin Poster/Banner Yönetim Sayfası
 *
 * Bu bileşen, ana sayfa slider ve promosyon görsellerinin
 * yönetimini sağlar. Gerçek API entegrasyonu ile çalışır.
 *
 * Özellikler:
 * - Poster listeleme (slider/promo filtresi)
 * - Yeni poster ekleme (dosya yükleme ile)
 * - Poster düzenleme
 * - Poster silme
 * - Aktif/Pasif toggle
 * - Varsayılana sıfırlama
 * - Drag & Drop dosya yükleme
 * - Responsive grid layout
 * - Loading skeleton
 * - Toast bildirimleri
 *
 * @author Senior Developer
 * @version 2.0.0
 */

import React, { useState, useEffect, useRef, useCallback } from "react";
import bannerService, {
  BANNER_DIMENSIONS,
  UPLOAD_CONFIG,
  validateFile,
} from "../../services/bannerService";

// ============================================
// SABİTLER
// ============================================

/** Form için başlangıç değerleri */
const INITIAL_FORM = {
  id: 0,
  title: "",
  imageUrl: "",
  linkUrl: "",
  isActive: true,
  displayOrder: 0,
  type: "slider", // Frontend'de "type" kullanıyoruz, API'ye "bannerType" olarak gönderiliyor
  subTitle: "",
  description: "",
  buttonText: "",
};

/** Feedback türleri için CSS class'ları */
const FEEDBACK_TYPES = {
  success: "success",
  danger: "danger",
  warning: "warning",
  info: "info",
};

// ============================================
// ANA BİLEŞEN
// ============================================

export default function PosterManagement() {
  // ============================================
  // STATE YÖNETİMİ
  // ============================================

  // Liste state'leri
  const [posters, setPosters] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Form state'leri
  const [form, setForm] = useState(INITIAL_FORM);
  const [showModal, setShowModal] = useState(false);
  const [imagePreview, setImagePreview] = useState("");
  const [uploading, setUploading] = useState(false);
  const [saving, setSaving] = useState(false);

  // Filtre ve feedback
  const [filter, setFilter] = useState("all");
  const [feedback, setFeedback] = useState({ msg: "", type: "" });

  // Drag & drop state
  const [isDragging, setIsDragging] = useState(false);

  // Refs
  const fileInputRef = useRef(null);
  const dragCounterRef = useRef(0);

  // ============================================
  // VERİ ÇEKME FONKSİYONLARI
  // ============================================

  /**
   * API'den poster listesini çeker
   * Admin endpoint kullanır (aktif/pasif tüm posterler)
   */
  const fetchPosters = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const data = await bannerService.getAdminBanners();

      // API'den gelen veriyi frontend formatına dönüştür
      const formattedData = data.map((item) => ({
        ...item,
        // API "position" kullanıyor, biz "type" olarak gösteriyoruz
        type: item.position === "homepage-middle" ? "promo" : "slider",
      }));

      setPosters(Array.isArray(formattedData) ? formattedData : []);
    } catch (err) {
      console.error("[PosterManagement] Veri çekme hatası:", err);
      setError(err.message || "Posterler yüklenirken hata oluştu");
      setPosters([]);
    } finally {
      setLoading(false);
    }
  }, []);

  // Sayfa yüklendiğinde verileri çek
  useEffect(() => {
    fetchPosters();
  }, [fetchPosters]);

  // ============================================
  // FEEDBACK (BİLDİRİM) YÖNETİMİ
  // ============================================

  /**
   * Kullanıcıya feedback mesajı gösterir
   * @param {string} msg - Mesaj metni
   * @param {string} type - Mesaj tipi (success, danger, warning, info)
   */
  const showFeedback = useCallback((msg, type = FEEDBACK_TYPES.success) => {
    setFeedback({ msg, type });

    // 4 saniye sonra otomatik kapat
    const timer = setTimeout(() => {
      setFeedback({ msg: "", type: "" });
    }, 4000);

    return () => clearTimeout(timer);
  }, []);

  // ============================================
  // FORM İŞLEMLERİ
  // ============================================

  /**
   * Form alanı değişikliklerini yönetir
   */
  const handleChange = useCallback((e) => {
    const { name, value, type, checked } = e.target;
    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  }, []);

  /**
   * Modal'ı açar (yeni ekleme veya düzenleme)
   * @param {Object|null} poster - Düzenlenecek poster veya null (yeni ekleme)
   */
  const openModal = useCallback((poster = null) => {
    if (poster) {
      // Düzenleme modu
      setForm({
        ...INITIAL_FORM,
        ...poster,
        type:
          poster.type ||
          (poster.position === "homepage-middle" ? "promo" : "slider"),
      });
      setImagePreview(poster.imageUrl || "");
    } else {
      // Yeni ekleme modu
      setForm(INITIAL_FORM);
      setImagePreview("");
    }
    setShowModal(true);
  }, []);

  /**
   * Modal'ı kapatır ve formu sıfırlar
   */
  const closeModal = useCallback(() => {
    setShowModal(false);
    setForm(INITIAL_FORM);
    setImagePreview("");
    setUploading(false);
    setSaving(false);

    // File input'u sıfırla
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  }, []);

  // ============================================
  // DOSYA YÜKLEME İŞLEMLERİ
  // ============================================

  /**
   * Dosya seçildiğinde veya sürüklendiğinde çağrılır
   * Dosyayı API'ye yükler ve URL'yi forma ekler
   * @param {File} file - Yüklenecek dosya
   */
  const handleFileUpload = useCallback(
    async (file) => {
      if (!file) return;

      // Dosya validasyonu
      const validation = validateFile(file);
      if (!validation.valid) {
        showFeedback(validation.error, FEEDBACK_TYPES.danger);
        return;
      }

      setUploading(true);

      try {
        // API'ye yükle
        const result = await bannerService.uploadImage(file);

        if (result && result.imageUrl) {
          setImagePreview(result.imageUrl);
          setForm((prev) => ({ ...prev, imageUrl: result.imageUrl }));
          showFeedback("Görsel başarıyla yüklendi", FEEDBACK_TYPES.success);
        } else {
          throw new Error("Görsel URL'si alınamadı");
        }
      } catch (err) {
        console.error("[PosterManagement] Dosya yükleme hatası:", err);
        showFeedback(
          err.message || "Görsel yüklenirken hata oluştu",
          FEEDBACK_TYPES.danger
        );

        // Hata durumunda local preview göster (fallback)
        const reader = new FileReader();
        reader.onload = (e) => {
          setImagePreview(e.target.result);
          // NOT: Bu durumda imageUrl base64 olacak, API kaydetmez
          // Kullanıcı URL girmelidir
        };
        reader.readAsDataURL(file);
      } finally {
        setUploading(false);
      }
    },
    [showFeedback]
  );

  /**
   * Input file change handler
   */
  const handleInputFileChange = useCallback(
    (e) => {
      const file = e.target.files?.[0];
      if (file) {
        handleFileUpload(file);
      }
    },
    [handleFileUpload]
  );

  // ============================================
  // DRAG & DROP İŞLEMLERİ
  // ============================================

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
    [handleFileUpload]
  );

  // ============================================
  // CRUD İŞLEMLERİ
  // ============================================

  /**
   * Form gönderildiğinde çağrılır (yeni ekleme veya güncelleme)
   */
  const handleSubmit = useCallback(
    async (e) => {
      e.preventDefault();

      // Validasyon
      if (!form.title?.trim()) {
        showFeedback("Başlık zorunludur", FEEDBACK_TYPES.danger);
        return;
      }
      if (!form.imageUrl?.trim()) {
        showFeedback("Görsel zorunludur", FEEDBACK_TYPES.danger);
        return;
      }

      setSaving(true);

      try {
        // API'ye gönderilecek veriyi hazırla (backend 'type' field'ını bekliyor)
        const bannerData = {
          ...form,
          type: form.type, // Form type'ı olduğu gibi gönder
          position: form.type === "promo" ? "homepage-middle" : "homepage-top",
        };

        if (form.id > 0) {
          // Güncelleme
          await bannerService.updateBanner(form.id, bannerData);
          showFeedback("Poster başarıyla güncellendi", FEEDBACK_TYPES.success);
        } else {
          // Yeni ekleme
          await bannerService.createBanner(bannerData);
          showFeedback("Poster başarıyla eklendi", FEEDBACK_TYPES.success);
        }

        closeModal();
        await fetchPosters(); // Listeyi yenile
      } catch (err) {
        console.error("[PosterManagement] Kaydetme hatası:", err);
        showFeedback(
          err.message || "Kaydetme sırasında hata oluştu",
          FEEDBACK_TYPES.danger
        );
      } finally {
        setSaving(false);
      }
    },
    [form, closeModal, fetchPosters, showFeedback]
  );

  /**
   * Poster siler
   * @param {number} id - Silinecek poster ID
   */
  const handleDelete = useCallback(
    async (id) => {
      if (!window.confirm("Bu posteri silmek istediğinize emin misiniz?")) {
        return;
      }

      try {
        await bannerService.deleteBanner(id);
        showFeedback("Poster başarıyla silindi", FEEDBACK_TYPES.success);
        await fetchPosters(); // Listeyi yenile
      } catch (err) {
        console.error("[PosterManagement] Silme hatası:", err);
        showFeedback(
          err.message || "Silme sırasında hata oluştu",
          FEEDBACK_TYPES.danger
        );
      }
    },
    [fetchPosters, showFeedback]
  );

  /**
   * Poster aktif/pasif durumunu değiştirir
   * @param {Object} poster - Toggle edilecek poster
   */
  const toggleActive = useCallback(
    async (poster) => {
      try {
        await bannerService.toggleBanner(poster.id);
        showFeedback(
          poster.isActive ? "Poster pasif yapıldı" : "Poster aktif yapıldı",
          FEEDBACK_TYPES.success
        );
        await fetchPosters(); // Listeyi yenile
      } catch (err) {
        console.error("[PosterManagement] Toggle hatası:", err);
        showFeedback(
          err.message || "Durum güncellenirken hata oluştu",
          FEEDBACK_TYPES.danger
        );
      }
    },
    [fetchPosters, showFeedback]
  );

  /**
   * Tüm posterleri varsayılan değerlere sıfırlar
   */
  const handleResetToDefault = useCallback(async () => {
    if (
      !window.confirm(
        "Tüm posterler varsayılana sıfırlanacak. Bu işlem geri alınamaz. Emin misiniz?"
      )
    ) {
      return;
    }

    try {
      setLoading(true);
      const result = await bannerService.resetToDefault();
      showFeedback(
        result.message || "Posterler varsayılana sıfırlandı",
        FEEDBACK_TYPES.success
      );
      await fetchPosters(); // Listeyi yenile
    } catch (err) {
      console.error("[PosterManagement] Sıfırlama hatası:", err);
      showFeedback(
        err.message || "Sıfırlama sırasında hata oluştu",
        FEEDBACK_TYPES.danger
      );
    } finally {
      setLoading(false);
    }
  }, [fetchPosters, showFeedback]);

  // ============================================
  // FİLTRELENMİŞ VERİLER
  // ============================================

  const sliderPosters = posters.filter(
    (p) => p.type === "slider" || p.position === "homepage-top"
  );
  const promoPosters = posters.filter(
    (p) => p.type === "promo" || p.position === "homepage-middle"
  );

  // ============================================
  // LOADING STATE
  // ============================================

  if (loading && posters.length === 0) {
    return (
      <div className="container-fluid py-4">
        {/* Loading Skeleton */}
        <div className="d-flex justify-content-between align-items-center mb-4">
          <div>
            <div className="placeholder-glow">
              <span
                className="placeholder col-6"
                style={{ width: "200px", height: "24px" }}
              ></span>
            </div>
          </div>
          <div className="placeholder-glow">
            <span
              className="placeholder col-4"
              style={{ width: "120px", height: "38px" }}
            ></span>
          </div>
        </div>

        {/* Slider Skeleton */}
        <div className="card border-0 shadow-sm mb-4">
          <div className="card-header bg-primary text-white py-3">
            <div className="placeholder-glow">
              <span
                className="placeholder bg-light"
                style={{ width: "180px" }}
              ></span>
            </div>
          </div>
          <div className="card-body p-4">
            <div className="d-flex flex-wrap gap-3 justify-content-center">
              {[1, 2, 3].map((i) => (
                <div
                  key={i}
                  className="placeholder-glow"
                  style={{ width: "320px", height: "200px" }}
                >
                  <span
                    className="placeholder w-100 h-100"
                    style={{ borderRadius: "12px" }}
                  ></span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Promo Skeleton */}
        <div className="card border-0 shadow-sm">
          <div className="card-header bg-info text-white py-3">
            <div className="placeholder-glow">
              <span
                className="placeholder bg-light"
                style={{ width: "200px" }}
              ></span>
            </div>
          </div>
          <div className="card-body p-4">
            <div className="d-flex flex-wrap gap-3 justify-content-center">
              {[1, 2, 3, 4].map((i) => (
                <div
                  key={i}
                  className="placeholder-glow"
                  style={{ width: "200px", height: "180px" }}
                >
                  <span
                    className="placeholder w-100 h-100"
                    style={{ borderRadius: "10px" }}
                  ></span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    );
  }

  // ============================================
  // ERROR STATE
  // ============================================

  if (error && posters.length === 0) {
    return (
      <div className="container-fluid py-4">
        <div
          className="alert alert-danger d-flex align-items-center"
          role="alert"
        >
          <i className="fas fa-exclamation-triangle me-3 fa-2x"></i>
          <div>
            <h5 className="alert-heading mb-1">Veri Yüklenemedi</h5>
            <p className="mb-2">{error}</p>
            <button
              className="btn btn-outline-danger btn-sm"
              onClick={fetchPosters}
            >
              <i className="fas fa-sync-alt me-1"></i>Tekrar Dene
            </button>
          </div>
        </div>
      </div>
    );
  }

  // ============================================
  // ANA RENDER
  // ============================================

  return (
    <div style={{ maxWidth: "100%" }}>
      {/* ========== HEADER ========== */}
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-3 px-1">
        <div>
          <h5 className="fw-bold mb-0">
            <i className="fas fa-image me-2" style={{ color: "#f97316" }}></i>
            Poster Yönetimi
          </h5>
          <small className="text-muted">
            Ana sayfa slider ve promosyon görselleri
          </small>
        </div>
        <div className="d-flex gap-2 flex-wrap">
          {/* Filtre Dropdown */}
          <select
            className="form-select form-select-sm"
            style={{ width: "auto" }}
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
          >
            <option value="all">Tümü ({posters.length})</option>
            <option value="slider">Slider ({sliderPosters.length})</option>
            <option value="promo">Promo ({promoPosters.length})</option>
          </select>

          {/* Varsayılana Sıfırla Butonu */}
          <button
            className="btn btn-outline-secondary btn-sm"
            onClick={handleResetToDefault}
            title="Varsayılana Sıfırla"
            disabled={loading}
          >
            <i className="fas fa-undo me-1"></i>
            <span className="d-none d-md-inline">Sıfırla</span>
          </button>

          {/* Yenile Butonu */}
          <button
            className="btn btn-outline-primary btn-sm"
            onClick={fetchPosters}
            title="Yenile"
            disabled={loading}
          >
            <i className={`fas fa-sync-alt ${loading ? "fa-spin" : ""}`}></i>
          </button>

          {/* Yeni Poster Butonu */}
          <button
            className="btn btn-success btn-sm"
            onClick={() => openModal()}
          >
            <i className="fas fa-plus me-1"></i>Yeni Poster
          </button>
        </div>
      </div>

      {/* ========== FEEDBACK ALERT ========== */}
      {feedback.msg && (
        <div
          className={`alert alert-${feedback.type} py-2 mx-1 d-flex align-items-center justify-content-between`}
          role="alert"
        >
          <span>
            <i
              className={`fas ${
                feedback.type === "success"
                  ? "fa-check-circle"
                  : "fa-exclamation-circle"
              } me-2`}
            ></i>
            {feedback.msg}
          </span>
          <button
            type="button"
            className="btn-close btn-sm"
            onClick={() => setFeedback({ msg: "", type: "" })}
          ></button>
        </div>
      )}

      {/* ========== SLIDER POSTERLER ========== */}
      <div className="card border-0 shadow-sm mb-3 mx-1">
        <div className="card-header bg-primary text-white py-2 d-flex justify-content-between align-items-center">
          <div>
            <i className="fas fa-images me-2"></i>
            Slider Posterler ({sliderPosters.length})
            <small className="ms-2 opacity-75">
              Önerilen: {BANNER_DIMENSIONS.slider.text}
            </small>
          </div>
          <button
            className="btn btn-light btn-sm"
            onClick={() => {
              setForm({ ...INITIAL_FORM, type: "slider" });
              setImagePreview("");
              setShowModal(true);
            }}
          >
            <i className="fas fa-plus me-1"></i>Slider Ekle
          </button>
        </div>
        <div className="card-body p-3">
          {sliderPosters.length === 0 ? (
            <p className="text-muted text-center py-4 mb-0">
              <i className="fas fa-image fa-2x mb-2 d-block opacity-50"></i>
              Henüz slider poster yok
            </p>
          ) : (
            <div className="d-flex flex-wrap gap-3 justify-content-center">
              {sliderPosters
                .sort((a, b) => (a.displayOrder || 0) - (b.displayOrder || 0))
                .map((p) => (
                  <PosterCard
                    key={p.id}
                    poster={p}
                    type="slider"
                    onEdit={() => openModal(p)}
                    onToggle={() => toggleActive(p)}
                    onDelete={() => handleDelete(p.id)}
                  />
                ))}
            </div>
          )}
        </div>
      </div>

      {/* ========== PROMO POSTERLER ========== */}
      <div className="card border-0 shadow-sm mx-1">
        <div className="card-header bg-info text-white py-2 d-flex justify-content-between align-items-center">
          <div>
            <i className="fas fa-th-large me-2"></i>
            Promosyon Görselleri ({promoPosters.length})
            <small className="ms-2 opacity-75">
              Önerilen: {BANNER_DIMENSIONS.promo.text}
            </small>
          </div>
          <button
            className="btn btn-light btn-sm"
            onClick={() => {
              setForm({ ...INITIAL_FORM, type: "promo" });
              setImagePreview("");
              setShowModal(true);
            }}
          >
            <i className="fas fa-plus me-1"></i>Promo Ekle
          </button>
        </div>
        <div className="card-body p-3">
          {promoPosters.length === 0 ? (
            <p className="text-muted text-center py-4 mb-0">
              <i className="fas fa-tags fa-2x mb-2 d-block opacity-50"></i>
              Henüz promosyon görseli yok
            </p>
          ) : (
            <div className="d-flex flex-wrap gap-3 justify-content-center">
              {promoPosters
                .sort((a, b) => (a.displayOrder || 0) - (b.displayOrder || 0))
                .map((p) => (
                  <PosterCard
                    key={p.id}
                    poster={p}
                    type="promo"
                    onEdit={() => openModal(p)}
                    onToggle={() => toggleActive(p)}
                    onDelete={() => handleDelete(p.id)}
                  />
                ))}
            </div>
          )}
        </div>
      </div>

      {/* ========== MODAL ========== */}
      {showModal && (
        <div
          className="modal show d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
          onClick={(e) => {
            if (e.target === e.currentTarget) closeModal();
          }}
        >
          <div className="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
            <div className="modal-content">
              {/* Modal Header */}
              <div className="modal-header py-2 bg-light">
                <h5 className="modal-title">
                  <i
                    className={`fas ${
                      form.id > 0 ? "fa-edit" : "fa-plus-circle"
                    } me-2`}
                  ></i>
                  {form.id > 0 ? "Poster Düzenle" : "Yeni Poster Ekle"}
                </h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={closeModal}
                  disabled={saving || uploading}
                ></button>
              </div>

              {/* Modal Body */}
              <form onSubmit={handleSubmit}>
                <div className="modal-body">
                  <div className="row">
                    {/* Sol Kolon: Form Alanları */}
                    <div className="col-md-6">
                      {/* Başlık */}
                      <div className="mb-3">
                        <label className="form-label fw-semibold">
                          Başlık <span className="text-danger">*</span>
                        </label>
                        <input
                          type="text"
                          className="form-control"
                          name="title"
                          value={form.title}
                          onChange={handleChange}
                          placeholder="Poster başlığı"
                          required
                          disabled={saving}
                        />
                      </div>

                      {/* Alt Başlık */}
                      <div className="mb-3">
                        <label className="form-label">Alt Başlık</label>
                        <input
                          type="text"
                          className="form-control"
                          name="subTitle"
                          value={form.subTitle || ""}
                          onChange={handleChange}
                          placeholder="Opsiyonel alt başlık"
                          disabled={saving}
                        />
                      </div>

                      {/* Poster Tipi */}
                      <div className="mb-3">
                        <label className="form-label fw-semibold">
                          Poster Tipi <span className="text-danger">*</span>
                        </label>
                        <select
                          className="form-select"
                          name="type"
                          value={form.type}
                          onChange={handleChange}
                          disabled={saving}
                        >
                          <option value="slider">
                            Slider (Ana Banner) -{" "}
                            {BANNER_DIMENSIONS.slider.text}
                          </option>
                          <option value="promo">
                            Promosyon - {BANNER_DIMENSIONS.promo.text}
                          </option>
                        </select>
                      </div>

                      {/* Link URL */}
                      <div className="mb-3">
                        <label className="form-label">Link URL</label>
                        <input
                          type="text"
                          className="form-control"
                          name="linkUrl"
                          value={form.linkUrl || ""}
                          onChange={handleChange}
                          placeholder="/kategori/meyve-sebze veya https://..."
                          disabled={saving}
                        />
                        <small className="text-muted">
                          Tıklandığında gidilecek sayfa
                        </small>
                      </div>

                      {/* Buton Metni */}
                      <div className="mb-3">
                        <label className="form-label">Buton Metni</label>
                        <input
                          type="text"
                          className="form-control"
                          name="buttonText"
                          value={form.buttonText || ""}
                          onChange={handleChange}
                          placeholder="Örn: Alışverişe Başla"
                          disabled={saving}
                        />
                      </div>

                      {/* Sıra ve Aktif */}
                      <div className="row">
                        <div className="col-6">
                          <label className="form-label">Sıra</label>
                          <input
                            type="number"
                            className="form-control"
                            name="displayOrder"
                            value={form.displayOrder}
                            onChange={handleChange}
                            min="0"
                            disabled={saving}
                          />
                        </div>
                        <div className="col-6 d-flex align-items-end pb-2">
                          <div className="form-check form-switch">
                            <input
                              type="checkbox"
                              className="form-check-input"
                              name="isActive"
                              checked={form.isActive}
                              onChange={handleChange}
                              id="isActiveCheck"
                              disabled={saving}
                            />
                            <label
                              className="form-check-label"
                              htmlFor="isActiveCheck"
                            >
                              Aktif
                            </label>
                          </div>
                        </div>
                      </div>
                    </div>

                    {/* Sağ Kolon: Görsel Yükleme */}
                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Görsel <span className="text-danger">*</span>
                      </label>

                      {/* Drag & Drop Alanı */}
                      <div
                        className={`border rounded p-3 text-center position-relative ${
                          isDragging
                            ? "border-primary bg-primary bg-opacity-10"
                            : "border-secondary"
                        }`}
                        style={{
                          minHeight: "200px",
                          backgroundColor: isDragging ? undefined : "#f8f9fa",
                          cursor: uploading ? "wait" : "pointer",
                          transition: "all 0.2s ease",
                        }}
                        onClick={() =>
                          !uploading && fileInputRef.current?.click()
                        }
                        onDragEnter={handleDragEnter}
                        onDragLeave={handleDragLeave}
                        onDragOver={handleDragOver}
                        onDrop={handleDrop}
                      >
                        {uploading ? (
                          <div className="py-5">
                            <div className="spinner-border text-primary mb-2"></div>
                            <p className="mb-0">Yükleniyor...</p>
                          </div>
                        ) : imagePreview ? (
                          <div>
                            <img
                              src={imagePreview}
                              alt="Önizleme"
                              style={{
                                maxWidth: "100%",
                                maxHeight: "180px",
                                objectFit: "contain",
                              }}
                              onError={(e) => {
                                e.target.src = "/images/placeholder.png";
                              }}
                            />
                            <p className="mt-2 mb-0 text-muted small">
                              Değiştirmek için tıklayın veya sürükleyin
                            </p>
                          </div>
                        ) : (
                          <div className="py-4">
                            <i
                              className={`fas ${
                                isDragging
                                  ? "fa-download"
                                  : "fa-cloud-upload-alt"
                              } fa-3x text-muted mb-2`}
                            ></i>
                            <p className="mb-1">
                              {isDragging
                                ? "Bırakın!"
                                : "Resim yüklemek için tıklayın"}
                            </p>
                            <small className="text-muted">
                              veya dosyayı buraya sürükleyin (max{" "}
                              {UPLOAD_CONFIG.maxSizeMB}MB)
                            </small>
                          </div>
                        )}
                      </div>

                      {/* Gizli File Input */}
                      <input
                        type="file"
                        ref={fileInputRef}
                        className="d-none"
                        accept={UPLOAD_CONFIG.allowedMimeTypes.join(",")}
                        onChange={handleInputFileChange}
                        disabled={uploading || saving}
                      />

                      {/* URL ile Ekleme */}
                      <div className="mt-2">
                        <small className="text-muted d-block mb-1">
                          veya URL ile ekleyin:
                        </small>
                        <input
                          type="text"
                          className="form-control form-control-sm"
                          name="imageUrl"
                          value={
                            form.imageUrl?.startsWith("data:")
                              ? ""
                              : form.imageUrl
                          }
                          onChange={(e) => {
                            const url = e.target.value;
                            setForm((prev) => ({ ...prev, imageUrl: url }));
                            setImagePreview(url);
                          }}
                          placeholder="/images/banner.png veya https://..."
                          disabled={uploading || saving}
                        />
                      </div>

                      {/* Boyut Bilgisi */}
                      <div
                        className="alert alert-info py-1 px-2 mt-2 mb-0"
                        style={{ fontSize: "0.75rem" }}
                      >
                        <i className="fas fa-info-circle me-1"></i>
                        Önerilen boyut:{" "}
                        <strong>
                          {BANNER_DIMENSIONS[form.type]?.text || "1200x400px"}
                        </strong>
                      </div>

                      {/* Format Bilgisi */}
                      <div className="mt-2 small text-muted">
                        <i className="fas fa-file-image me-1"></i>
                        İzin verilen formatlar:{" "}
                        {UPLOAD_CONFIG.allowedExtensions.join(", ")}
                      </div>
                    </div>
                  </div>
                </div>

                {/* Modal Footer */}
                <div className="modal-footer py-2 bg-light">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={closeModal}
                    disabled={saving || uploading}
                  >
                    <i className="fas fa-times me-1"></i>İptal
                  </button>
                  <button
                    type="submit"
                    className="btn btn-success"
                    disabled={saving || uploading}
                  >
                    {saving ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-1"></span>
                        Kaydediliyor...
                      </>
                    ) : (
                      <>
                        <i
                          className={`fas ${
                            form.id > 0 ? "fa-save" : "fa-plus"
                          } me-1`}
                        ></i>
                        {form.id > 0 ? "Kaydet" : "Ekle"}
                      </>
                    )}
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

// ============================================
// POSTER CARD BİLEŞENİ
// ============================================

/**
 * Tek bir poster kartını render eder
 * Slider ve promo tipleri için farklı boyutlarda gösterim
 */
function PosterCard({ poster, type, onEdit, onToggle, onDelete }) {
  const isSlider = type === "slider";

  return (
    <div
      className={`position-relative ${!poster.isActive ? "opacity-50" : ""}`}
      style={{
        width: isSlider ? "320px" : "200px",
        borderRadius: isSlider ? "12px" : "10px",
        overflow: "hidden",
        boxShadow: isSlider
          ? "0 4px 12px rgba(0,0,0,0.15)"
          : "0 3px 10px rgba(0,0,0,0.12)",
        transition: "transform 0.2s, box-shadow 0.2s",
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = "scale(1.02)";
        e.currentTarget.style.boxShadow = "0 6px 20px rgba(0,0,0,0.2)";
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = "scale(1)";
        e.currentTarget.style.boxShadow = isSlider
          ? "0 4px 12px rgba(0,0,0,0.15)"
          : "0 3px 10px rgba(0,0,0,0.12)";
      }}
    >
      {/* Görsel */}
      <img
        src={poster.imageUrl}
        alt={poster.title}
        style={{
          width: "100%",
          height: isSlider ? "160px" : "130px",
          objectFit: "cover",
          display: "block",
        }}
        onError={(e) => {
          e.target.src = "/images/placeholder.png";
        }}
        loading="lazy"
      />

      {/* Sıra Badge (Sol Üst) */}
      <div className="position-absolute top-0 start-0 m-2">
        <span className="badge bg-dark">#{poster.displayOrder || 0}</span>
      </div>

      {/* Aktif/Pasif Badge (Sağ Üst) */}
      <div className="position-absolute top-0 end-0 m-2">
        <span
          className={`badge ${poster.isActive ? "bg-success" : "bg-secondary"}`}
          style={{ fontSize: isSlider ? undefined : "0.65rem" }}
        >
          {poster.isActive ? "Aktif" : "Pasif"}
        </span>
      </div>

      {/* Alt Bar */}
      {isSlider ? (
        // Slider için gradient overlay
        <div
          style={{
            background: "linear-gradient(transparent, rgba(0,0,0,0.8))",
            padding: "30px 12px 12px",
            position: "absolute",
            bottom: 0,
            left: 0,
            right: 0,
          }}
        >
          <div className="d-flex justify-content-between align-items-center">
            <span
              className="text-white fw-bold text-truncate"
              style={{ maxWidth: "150px" }}
              title={poster.title}
            >
              {poster.title}
            </span>
            <div className="d-flex gap-1">
              <ActionButton
                icon="fa-edit"
                variant="light"
                onClick={onEdit}
                title="Düzenle"
              />
              <ActionButton
                icon={poster.isActive ? "fa-eye-slash" : "fa-eye"}
                variant={poster.isActive ? "warning" : "success"}
                onClick={onToggle}
                title={poster.isActive ? "Pasif Yap" : "Aktif Yap"}
              />
              <ActionButton
                icon="fa-trash"
                variant="danger"
                onClick={onDelete}
                title="Sil"
              />
            </div>
          </div>
        </div>
      ) : (
        // Promo için solid bar
        <div
          style={{
            background: "#fff",
            padding: "8px",
            borderTop: "1px solid #eee",
          }}
        >
          <div className="d-flex justify-content-between align-items-center">
            <span
              className="fw-semibold text-truncate"
              style={{ fontSize: "0.8rem", maxWidth: "100px" }}
              title={poster.title}
            >
              {poster.title}
            </span>
            <div className="d-flex gap-1">
              <ActionButton
                icon="fa-edit"
                variant="outline-primary"
                onClick={onEdit}
                title="Düzenle"
                small
              />
              <ActionButton
                icon={poster.isActive ? "fa-eye-slash" : "fa-eye"}
                variant={
                  poster.isActive ? "outline-warning" : "outline-success"
                }
                onClick={onToggle}
                title={poster.isActive ? "Pasif Yap" : "Aktif Yap"}
                small
              />
              <ActionButton
                icon="fa-trash"
                variant="outline-danger"
                onClick={onDelete}
                title="Sil"
                small
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ============================================
// ACTION BUTTON BİLEŞENİ
// ============================================

/**
 * Poster kartlarındaki aksiyon butonları
 */
function ActionButton({ icon, variant, onClick, title, small }) {
  return (
    <button
      className={`btn btn-${variant} ${small ? "p-1" : "btn-sm"}`}
      style={small ? { fontSize: "0.7rem" } : undefined}
      onClick={(e) => {
        e.stopPropagation();
        onClick();
      }}
      title={title}
    >
      <i className={`fas ${icon}`}></i>
    </button>
  );
}
