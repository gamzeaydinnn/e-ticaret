import React, { useState, useEffect, useRef } from "react";
import posterService from "../../services/posterService";

const initialForm = {
  id: 0,
  title: "",
  imageUrl: "",
  linkUrl: "",
  isActive: true,
  displayOrder: 0,
  type: "slider",
};

const DIMENSION_GUIDELINES = {
  slider: { width: 1200, height: 400, text: "1200x400px (Ana Slider)" },
  promo: { width: 300, height: 200, text: "300x200px (Kampanya)" },
};

export default function PosterManagement() {
  const [posters, setPosters] = useState([]);
  const [form, setForm] = useState(initialForm);
  const [showModal, setShowModal] = useState(false);
  const [loading, setLoading] = useState(true);
  const [feedback, setFeedback] = useState({ msg: "", type: "" });
  const [filter, setFilter] = useState("all");
  const [imagePreview, setImagePreview] = useState("");
  const [uploading, setUploading] = useState(false);
  const fileInputRef = useRef(null);

  const fetchPosters = async () => {
    try {
      setLoading(true);
      const data = await posterService.getAll();
      setPosters(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error("Posterler yüklenirken hata:", err);
      setPosters([]);
      showFeedback("Posterler yüklenemedi - Backend API'ye bağlanılamadı", "danger");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPosters();
    // Gerçek backend API - subscription sistemi
    const unsubscribe = posterService.subscribe(fetchPosters);
    return () => unsubscribe && unsubscribe();
  }, []);

  const showFeedback = (msg, type = "success") => {
    setFeedback({ msg, type });
    setTimeout(() => setFeedback({ msg: "", type: "" }), 3000);
  };

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setForm((p) => ({ ...p, [name]: type === "checkbox" ? checked : value }));
  };

  // Resim yükleme - Base64 olarak kaydet ve boyut kontrolü yap
  const handleImageUpload = (e) => {
    const file = e.target.files[0];
    if (!file) return;

    // Dosya boyutu kontrolü (max 10MB)
    if (file.size > 10 * 1024 * 1024) {
      showFeedback("Dosya boyutu 10MB'dan küçük olmalı", "danger");
      return;
    }

    // Sadece resim dosyaları
    if (!file.type.startsWith("image/")) {
      showFeedback("Sadece resim dosyaları yüklenebilir", "danger");
      return;
    }

    setUploading(true);
    const reader = new FileReader();
    reader.onload = (event) => {
      const base64 = event.target.result;
      
      // Resim boyutlarını kontrol et
      const img = new Image();
      img.onload = () => {
        const guidelines = DIMENSION_GUIDELINES[form.type];
        const widthTolerance = 100; // ±100px tolerans
        const heightTolerance = 50;  // ±50px tolerans
        
        const widthInRange = Math.abs(img.width - guidelines.width) <= widthTolerance;
        const heightInRange = Math.abs(img.height - guidelines.height) <= heightTolerance;
        
        if (!widthInRange || !heightInRange) {
          showFeedback(
            `Görsel boyutu önerilen ölçülere uymuyor. Önerilen: ${guidelines.text} (±tolerans), Yüklenen: ${img.width}x${img.height}px. Yine de kullanılacak.`,
            "warning"
          );
        }
        
        setImagePreview(base64);
        setForm((p) => ({ ...p, imageUrl: base64 }));
        setUploading(false);
      };
      img.onerror = () => {
        showFeedback("Resim yüklenirken hata oluştu", "danger");
        setUploading(false);
      };
      img.src = base64;
    };
    reader.onerror = () => {
      showFeedback("Resim yüklenirken hata oluştu", "danger");
      setUploading(false);
    };
    reader.readAsDataURL(file);
  };

  const openModal = (poster = null) => {
    if (poster) {
      setForm(poster);
      setImagePreview(poster.imageUrl);
    } else {
      setForm(initialForm);
      setImagePreview("");
    }
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setForm(initialForm);
    setImagePreview("");
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!form.title.trim()) {
      showFeedback("Başlık zorunludur", "danger");
      return;
    }
    if (!form.imageUrl) {
      showFeedback("Görsel zorunludur", "danger");
      return;
    }

    try {
      if (form.id > 0) {
        await posterService.update(form.id, form);
        showFeedback("Poster güncellendi");
      } else {
        await posterService.create(form);
        showFeedback("Poster eklendi");
      }
      await fetchPosters(); // Listeyi yenile
      closeModal();
    } catch (err) {
      console.error("Poster kaydetme hatası:", err);
      showFeedback(err.message || "Hata oluştu", "danger");
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm("Bu posteri silmek istediğinize emin misiniz?")) return;
    try {
      await posterService.delete(id);
      showFeedback("Poster silindi");
      await fetchPosters(); // Listeyi yenile
    } catch (err) {
      console.error("Poster silme hatası:", err);
      showFeedback("Silinemedi", "danger");
    }
  };

  const toggleActive = async (poster) => {
    try {
      await posterService.toggleActive(poster);
      showFeedback(
        poster.isActive ? "Poster pasif yapıldı" : "Poster aktif yapıldı"
      );
      await fetchPosters(); // Listeyi yenile
    } catch (err) {
      console.error("Durum güncelleme hatası:", err);
      showFeedback("Durum güncellenemedi", "danger");
    }
  };

  const filtered =
    filter === "all" ? posters : posters.filter((p) => p.type === filter);
  const sliderPosters = posters.filter((p) => p.type === "slider");
  const promoPosters = posters.filter((p) => p.type === "promo");

  if (loading) {
    return (
      <div className="d-flex justify-content-center py-5">
        <div className="spinner-border text-primary"></div>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: "100%" }}>
      {/* Header */}
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
        <div className="d-flex gap-2">
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
          <button
            className="btn btn-outline-secondary btn-sm"
            onClick={() => {
              if (window.confirm("Tüm posterler varsayılana sıfırlanacak. Emin misiniz?")) {
                // JSON Server için: npm run mock-reset komutu ile sıfırlanır
                showFeedback("Sıfırlamak için terminalde 'npm run mock-reset' çalıştırın", "warning");
              }
            }}
            title="Varsayılana Sıfırla (Terminal: npm run mock-reset)"
          >
            <i className="fas fa-undo"></i>
          </button>
          <button
            className="btn btn-success btn-sm"
            onClick={() => openModal()}
          >
            <i className="fas fa-plus me-1"></i>Yeni Poster
          </button>
        </div>
      </div>

      {feedback.msg && (
        <div className={`alert alert-${feedback.type} py-2 mx-1`}>
          {feedback.msg}
        </div>
      )}

      {/* Slider Posterler - Büyük Önizleme */}
      <div className="card border-0 shadow-sm mb-3 mx-1">
        <div className="card-header bg-primary text-white py-2 d-flex justify-content-between align-items-center">
          <div>
            <i className="fas fa-images me-2"></i>
            Slider Posterler ({sliderPosters.length})
            <small className="ms-2 opacity-75">Önerilen: 1200x400px</small>
          </div>
          <button
            className="btn btn-light btn-sm"
            onClick={() => {
              setForm({ ...initialForm, type: "slider" });
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
                .sort((a, b) => {
                  if (a.displayOrder !== b.displayOrder) {
                    return a.displayOrder - b.displayOrder;
                  }
                  return a.id - b.id;
                })
                .map((p) => (
                  <div
                    key={p.id}
                    className={`position-relative ${!p.isActive ? "opacity-50" : ""}`}
                    style={{
                      width: "320px",
                      borderRadius: "12px",
                      overflow: "hidden",
                      boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
                      transition: "transform 0.2s",
                    }}
                    onMouseEnter={(e) => e.currentTarget.style.transform = "scale(1.02)"}
                    onMouseLeave={(e) => e.currentTarget.style.transform = "scale(1)"}
                  >
                    <img
                      src={p.imageUrl}
                      alt={p.title}
                      style={{
                        width: "100%",
                        height: "160px",
                        objectFit: "cover",
                        display: "block",
                      }}
                      onError={(e) => {
                        e.target.src = "/images/placeholder.png";
                      }}
                    />
                    {/* Overlay Badges */}
                    <div className="position-absolute top-0 start-0 m-2">
                      <span className="badge bg-dark">#{p.displayOrder}</span>
                    </div>
                    <div className="position-absolute top-0 end-0 m-2">
                      <span className={`badge ${p.isActive ? "bg-success" : "bg-secondary"}`}>
                        {p.isActive ? "Aktif" : "Pasif"}
                      </span>
                    </div>
                    {/* Bottom Bar */}
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
                        <span className="text-white fw-bold text-truncate" style={{ maxWidth: "150px" }}>
                          {p.title}
                        </span>
                        <div className="d-flex gap-1">
                          <button
                            className="btn btn-light btn-sm"
                            onClick={() => openModal(p)}
                            title="Düzenle"
                          >
                            <i className="fas fa-edit"></i>
                          </button>
                          <button
                            className={`btn btn-sm ${p.isActive ? "btn-warning" : "btn-success"}`}
                            onClick={() => toggleActive(p)}
                            title={p.isActive ? "Pasif Yap" : "Aktif Yap"}
                          >
                            <i className={`fas ${p.isActive ? "fa-eye-slash" : "fa-eye"}`}></i>
                          </button>
                          <button
                            className="btn btn-danger btn-sm"
                            onClick={() => handleDelete(p.id)}
                            title="Sil"
                          >
                            <i className="fas fa-trash"></i>
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
            </div>
          )}
        </div>
      </div>

      {/* Promo Posterler - 4'lü Grid */}
      <div className="card border-0 shadow-sm mx-1">
        <div className="card-header bg-info text-white py-2 d-flex justify-content-between align-items-center">
          <div>
            <i className="fas fa-th-large me-2"></i>
            Promosyon Görselleri ({promoPosters.length})
            <small className="ms-2 opacity-75">Önerilen: 300x200px</small>
          </div>
          <button
            className="btn btn-light btn-sm"
            onClick={() => {
              setForm({ ...initialForm, type: "promo" });
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
                .sort((a, b) => {
                  if (a.displayOrder !== b.displayOrder) {
                    return a.displayOrder - b.displayOrder;
                  }
                  return a.id - b.id;
                })
                .map((p) => (
                  <div
                    key={p.id}
                    className={`position-relative ${!p.isActive ? "opacity-50" : ""}`}
                    style={{
                      width: "200px",
                      borderRadius: "10px",
                      overflow: "hidden",
                      boxShadow: "0 3px 10px rgba(0,0,0,0.12)",
                      transition: "transform 0.2s",
                    }}
                    onMouseEnter={(e) => e.currentTarget.style.transform = "scale(1.03)"}
                    onMouseLeave={(e) => e.currentTarget.style.transform = "scale(1)"}
                  >
                    <img
                      src={p.imageUrl}
                      alt={p.title}
                      style={{
                        width: "100%",
                        height: "130px",
                        objectFit: "cover",
                        display: "block",
                      }}
                      onError={(e) => {
                        e.target.src = "/images/placeholder.png";
                      }}
                    />
                    {/* Badges */}
                    <div className="position-absolute top-0 end-0 m-1">
                      <span
                        className={`badge ${p.isActive ? "bg-success" : "bg-secondary"}`}
                        style={{ fontSize: "0.65rem" }}
                      >
                        {p.isActive ? "Aktif" : "Pasif"}
                      </span>
                    </div>
                    {/* Bottom Info */}
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
                        >
                          {p.title}
                        </span>
                        <div className="d-flex gap-1">
                          <button
                            className="btn btn-outline-primary p-1"
                            style={{ fontSize: "0.7rem" }}
                            onClick={() => openModal(p)}
                            title="Düzenle"
                          >
                            <i className="fas fa-edit"></i>
                          </button>
                          <button
                            className={`btn p-1 ${p.isActive ? "btn-outline-warning" : "btn-outline-success"}`}
                            style={{ fontSize: "0.7rem" }}
                            onClick={() => toggleActive(p)}
                          >
                            <i className={`fas ${p.isActive ? "fa-eye-slash" : "fa-eye"}`}></i>
                          </button>
                          <button
                            className="btn btn-outline-danger p-1"
                            style={{ fontSize: "0.7rem" }}
                            onClick={() => handleDelete(p.id)}
                          >
                            <i className="fas fa-trash"></i>
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
            </div>
          )}
        </div>
      </div>

      {/* Modal */}
      {showModal && (
        <div
          className="modal show d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-lg modal-dialog-centered">
            <div className="modal-content">
              <div className="modal-header py-2">
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
                ></button>
              </div>
              <form onSubmit={handleSubmit}>
                <div className="modal-body">
                  <div className="row">
                    {/* Sol: Form */}
                    <div className="col-md-6">
                      <div className="mb-3">
                        <label className="form-label">Başlık *</label>
                        <input
                          type="text"
                          className="form-control"
                          name="title"
                          value={form.title}
                          onChange={handleChange}
                          placeholder="Poster başlığı"
                          required
                        />
                      </div>
                      <div className="mb-3">
                        <label className="form-label">Poster Tipi *</label>
                        <select
                          className="form-select"
                          name="type"
                          value={form.type}
                          onChange={handleChange}
                        >
                          <option value="slider">
                            Slider (Ana Banner) -{" "}
                            {DIMENSION_GUIDELINES.slider.text}
                          </option>
                          <option value="promo">
                            Promosyon - {DIMENSION_GUIDELINES.promo.text}
                          </option>
                        </select>
                      </div>
                      <div className="mb-3">
                        <label className="form-label">Link URL</label>
                        <input
                          type="text"
                          className="form-control"
                          name="linkUrl"
                          value={form.linkUrl}
                          onChange={handleChange}
                          placeholder="/kategori/meyve-sebze veya https://..."
                        />
                        <small className="text-muted">
                          Tıklandığında gidilecek sayfa
                        </small>
                      </div>
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
                          />
                        </div>
                        <div className="col-6 d-flex align-items-end">
                          <div className="form-check">
                            <input
                              type="checkbox"
                              className="form-check-input"
                              name="isActive"
                              checked={form.isActive}
                              onChange={handleChange}
                              id="isActiveCheck"
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
                    {/* Sağ: Resim Yükleme */}
                    <div className="col-md-6">
                      <label className="form-label">Görsel *</label>
                      <div
                        className="border rounded p-3 text-center"
                        style={{
                          minHeight: "200px",
                          backgroundColor: "#f8f9fa",
                          cursor: "pointer",
                        }}
                        onClick={() => fileInputRef.current?.click()}
                      >
                        {uploading ? (
                          <div className="py-5">
                            <div className="spinner-border text-primary"></div>
                            <p className="mt-2 mb-0">Yükleniyor...</p>
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
                              Değiştirmek için tıklayın
                            </p>
                          </div>
                        ) : (
                          <div className="py-4">
                            <i className="fas fa-cloud-upload-alt fa-3x text-muted mb-2"></i>
                            <p className="mb-1">Resim yüklemek için tıklayın</p>
                        <small className="text-muted">
                          veya URL girin (max 10MB)
                        </small>
                          </div>
                        )}
                      </div>
                      <input
                        type="file"
                        ref={fileInputRef}
                        className="d-none"
                        accept="image/*"
                        onChange={handleImageUpload}
                      />
                      <div className="mt-2">
                        <small className="text-muted d-block mb-1">
                          veya URL ile ekleyin:
                        </small>
                        <input
                          type="text"
                          className="form-control form-control-sm"
                          name="imageUrl"
                          value={
                            form.imageUrl.startsWith("data:")
                              ? ""
                              : form.imageUrl
                          }
                          onChange={(e) => {
                            setForm((p) => ({
                              ...p,
                              imageUrl: e.target.value,
                            }));
                            setImagePreview(e.target.value);
                          }}
                          placeholder="/images/banner.png veya https://..."
                        />
                      </div>
                      <div
                        className="alert alert-info py-1 px-2 mt-2 mb-0"
                        style={{ fontSize: "0.75rem" }}
                      >
                        <i className="fas fa-info-circle me-1"></i>
                        Önerilen boyut:{" "}
                        <strong>{DIMENSION_GUIDELINES[form.type].text}</strong>
                      </div>
                    </div>
                  </div>
                </div>
                <div className="modal-footer py-2">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={closeModal}
                  >
                    İptal
                  </button>
                  <button
                    type="submit"
                    className="btn btn-success"
                    disabled={uploading}
                  >
                    <i
                      className={`fas ${
                        form.id > 0 ? "fa-save" : "fa-plus"
                      } me-1`}
                    ></i>
                    {form.id > 0 ? "Kaydet" : "Ekle"}
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
