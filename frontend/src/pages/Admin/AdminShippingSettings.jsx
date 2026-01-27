// ==========================================================================
// AdminShippingSettings.jsx - Kargo Ücreti Yönetim Sayfası
// ==========================================================================
// Admin panelinde araç tipine göre kargo ücretlerini yönetmek için kullanılır.
// Motosiklet ve Araba için ayrı fiyat kartları, anlık güncelleme.
// Mobil uyumlu, modern ve profesyonel tasarım.
// ==========================================================================

import React, { useState, useEffect, useCallback } from "react";
import {
  getAllSettingsAdmin,
  updateSetting,
  toggleActive,
  formatShippingPrice,
  VEHICLE_TYPES,
} from "../../services/shippingService";

// ============================================
// SABİTLER
// ============================================

/**
 * Form başlangıç değerleri
 */
const initialEditForm = {
  price: "",
  displayName: "",
  estimatedDeliveryTime: "",
  description: "",
};

// ============================================
// ANA COMPONENT
// ============================================

export default function AdminShippingSettings() {
  // State tanımlamaları
  const [settings, setSettings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);

  // Feedback mesajları
  const [message, setMessage] = useState("");
  const [messageType, setMessageType] = useState("success");

  // Düzenleme modları (her kart için ayrı)
  const [editingId, setEditingId] = useState(null);
  const [editForm, setEditForm] = useState(initialEditForm);

  // ============================================
  // VERİ YÜKLEME
  // ============================================

  const loadSettings = useCallback(async () => {
    try {
      setLoading(true);
      setError("");
      const data = await getAllSettingsAdmin();
      setSettings(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error("Kargo ayarları yüklenemedi:", err);
      setError(err.message || "Kargo ayarları yüklenirken bir hata oluştu");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadSettings();
  }, [loadSettings]);

  // ============================================
  // MESAJ GÖSTERİMİ
  // ============================================

  const showMessage = (msg, type = "success") => {
    setMessage(msg);
    setMessageType(type);
    // 4 saniye sonra mesajı kaldır
    setTimeout(() => setMessage(""), 4000);
  };

  // ============================================
  // DÜZENLEME İŞLEMLERİ
  // ============================================

  /**
   * Düzenleme moduna geçiş
   */
  const handleStartEdit = (setting) => {
    setEditingId(setting.id);
    setEditForm({
      price: setting.price?.toString() || "",
      displayName: setting.displayName || "",
      estimatedDeliveryTime: setting.estimatedDeliveryTime || "",
      description: setting.description || "",
    });
  };

  /**
   * Düzenleme iptal
   */
  const handleCancelEdit = () => {
    setEditingId(null);
    setEditForm(initialEditForm);
  };

  /**
   * Form değişikliği
   */
  const handleFormChange = (e) => {
    const { name, value } = e.target;
    setEditForm((prev) => ({ ...prev, [name]: value }));
  };

  /**
   * Kaydetme işlemi
   */
  const handleSave = async (id) => {
    // Validasyon
    const price = parseFloat(editForm.price);
    if (isNaN(price) || price < 0) {
      showMessage("Geçerli bir fiyat giriniz (0 veya üzeri)", "danger");
      return;
    }

    try {
      setSaving(true);

      await updateSetting(id, {
        price: price,
        displayName: editForm.displayName.trim() || undefined,
        estimatedDeliveryTime:
          editForm.estimatedDeliveryTime.trim() || undefined,
        description: editForm.description.trim() || undefined,
      });

      showMessage("✅ Kargo ücreti başarıyla güncellendi!", "success");
      handleCancelEdit();
      await loadSettings(); // Yeniden yükle
    } catch (err) {
      console.error("Güncelleme hatası:", err);
      showMessage(
        err.message || "Güncelleme sırasında bir hata oluştu",
        "danger",
      );
    } finally {
      setSaving(false);
    }
  };

  /**
   * Aktif/Pasif toggle
   */
  const handleToggleActive = async (id, currentStatus) => {
    try {
      await toggleActive(id, !currentStatus);
      showMessage(
        currentStatus
          ? "Kargo seçeneği pasif yapıldı"
          : "Kargo seçeneği aktif yapıldı",
        "success",
      );
      await loadSettings();
    } catch (err) {
      showMessage(err.message || "İşlem başarısız oldu", "danger");
    }
  };

  // ============================================
  // HIZLI FİYAT GÜNCELLEME (Inline)
  // ============================================

  const handleQuickPriceUpdate = async (id, newPrice) => {
    const price = parseFloat(newPrice);
    if (isNaN(price) || price < 0) return;

    try {
      setSaving(true);
      await updateSetting(id, { price });
      showMessage("✅ Fiyat güncellendi!", "success");
      await loadSettings();
    } catch (err) {
      showMessage("Fiyat güncellenemedi", "danger");
    } finally {
      setSaving(false);
    }
  };

  // ============================================
  // ARAÇ TİPİ BİLGİLERİ
  // ============================================

  const getVehicleInfo = (vehicleType) => {
    const type = vehicleType?.toLowerCase();
    return (
      VEHICLE_TYPES[type] || {
        key: type,
        icon: "fa-truck",
        label: vehicleType || "Bilinmiyor",
        color: "#6c757d",
        bgColor: "#f8f9fa",
      }
    );
  };

  // ============================================
  // LOADING STATE
  // ============================================

  if (loading) {
    return (
      <div className="container-fluid py-4">
        <div
          className="d-flex justify-content-center align-items-center"
          style={{ minHeight: "400px" }}
        >
          <div className="text-center">
            <div
              className="spinner-border text-primary mb-3"
              style={{ width: "3rem", height: "3rem" }}
            >
              <span className="visually-hidden">Yükleniyor...</span>
            </div>
            <p className="text-muted">Kargo ayarları yükleniyor...</p>
          </div>
        </div>
      </div>
    );
  }

  // ============================================
  // RENDER
  // ============================================

  return (
    <div className="container-fluid py-3 py-md-4">
      {/* ═══════════════════════════════════════════════════════════════════════════════
          SAYFA BAŞLIĞI
          ═══════════════════════════════════════════════════════════════════════════════ */}
      <div className="row mb-4">
        <div className="col-12">
          <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3">
            <div>
              <h2 className="mb-1 fw-bold d-flex align-items-center gap-2">
                <i className="fas fa-shipping-fast text-primary"></i>
                <span>Kargo Ücreti Ayarları</span>
              </h2>
              <p className="text-muted mb-0 small">
                Araç tipine göre kargo ücretlerini yönetin
              </p>
            </div>

            {/* Yenile Butonu */}
            <button
              className="btn btn-outline-primary d-flex align-items-center gap-2"
              onClick={loadSettings}
              disabled={loading}
            >
              <i className={`fas fa-sync-alt ${loading ? "fa-spin" : ""}`}></i>
              <span className="d-none d-sm-inline">Yenile</span>
            </button>
          </div>
        </div>
      </div>

      {/* ═══════════════════════════════════════════════════════════════════════════════
          BİLGİLENDİRME MESAJI
          ═══════════════════════════════════════════════════════════════════════════════ */}
      {message && (
        <div
          className={`alert alert-${messageType} alert-dismissible fade show d-flex align-items-center`}
          role="alert"
        >
          <i
            className={`fas ${messageType === "success" ? "fa-check-circle" : "fa-exclamation-triangle"} me-2`}
          ></i>
          <span>{message}</span>
          <button
            type="button"
            className="btn-close"
            onClick={() => setMessage("")}
            aria-label="Kapat"
          ></button>
        </div>
      )}

      {/* ═══════════════════════════════════════════════════════════════════════════════
          HATA MESAJI
          ═══════════════════════════════════════════════════════════════════════════════ */}
      {error && (
        <div
          className="alert alert-danger d-flex align-items-center"
          role="alert"
        >
          <i className="fas fa-exclamation-circle me-2"></i>
          <div className="flex-grow-1">{error}</div>
          <button
            className="btn btn-sm btn-outline-danger ms-2"
            onClick={loadSettings}
          >
            Tekrar Dene
          </button>
        </div>
      )}

      {/* ═══════════════════════════════════════════════════════════════════════════════
          KARGO AYARLARI KARTLARI
          ═══════════════════════════════════════════════════════════════════════════════ */}
      <div className="row g-3 g-md-4">
        {settings.map((setting) => {
          const vehicleInfo = getVehicleInfo(setting.vehicleType);
          const isEditing = editingId === setting.id;

          return (
            <div key={setting.id} className="col-12 col-md-6 col-xl-6">
              <div
                className={`card h-100 border-0 shadow-sm overflow-hidden transition-all ${
                  !setting.isActive ? "opacity-75" : ""
                }`}
                style={{
                  borderRadius: "16px",
                  transition: "all 0.3s ease",
                }}
              >
                {/* Kart Üst Kısım - Renkli Gradient */}
                <div
                  className="card-header border-0 py-3 py-md-4"
                  style={{
                    background: `linear-gradient(135deg, ${vehicleInfo.color}, ${vehicleInfo.color}dd)`,
                  }}
                >
                  <div className="d-flex justify-content-between align-items-center">
                    <div className="d-flex align-items-center gap-3">
                      {/* Araç İkonu */}
                      <div
                        className="d-flex align-items-center justify-content-center rounded-circle"
                        style={{
                          width: "50px",
                          height: "50px",
                          backgroundColor: "rgba(255,255,255,0.2)",
                        }}
                      >
                        <i
                          className={`fas ${vehicleInfo.icon} fa-lg text-white`}
                        ></i>
                      </div>

                      {/* Başlık */}
                      <div>
                        <h5 className="mb-0 text-white fw-bold">
                          {setting.displayName || vehicleInfo.label}
                        </h5>
                        <small className="text-white-50">
                          {setting.vehicleType}
                        </small>
                      </div>
                    </div>

                    {/* Aktif/Pasif Toggle */}
                    <div className="form-check form-switch">
                      <input
                        type="checkbox"
                        className="form-check-input"
                        id={`active-${setting.id}`}
                        checked={setting.isActive}
                        onChange={() =>
                          handleToggleActive(setting.id, setting.isActive)
                        }
                        style={{
                          width: "3rem",
                          height: "1.5rem",
                          cursor: "pointer",
                        }}
                      />
                      <label
                        className="form-check-label text-white small"
                        htmlFor={`active-${setting.id}`}
                        style={{ cursor: "pointer" }}
                      >
                        {setting.isActive ? "Aktif" : "Pasif"}
                      </label>
                    </div>
                  </div>
                </div>

                {/* Kart İçerik */}
                <div className="card-body p-3 p-md-4">
                  {isEditing ? (
                    /* ═══════════════════════════════════════════════════════════════════
                       DÜZENLEME MODU
                       ═══════════════════════════════════════════════════════════════════ */
                    <div className="edit-form">
                      {/* Fiyat Input */}
                      <div className="mb-3">
                        <label className="form-label fw-semibold">
                          <i className="fas fa-lira-sign me-1 text-muted"></i>
                          Kargo Ücreti (TL)
                        </label>
                        <div className="input-group input-group-lg">
                          <input
                            type="number"
                            name="price"
                            className="form-control"
                            value={editForm.price}
                            onChange={handleFormChange}
                            min="0"
                            step="0.01"
                            placeholder="0.00"
                            style={{
                              fontSize: "1.5rem",
                              fontWeight: "bold",
                              borderRadius: "12px 0 0 12px",
                            }}
                          />
                          <span
                            className="input-group-text fw-bold"
                            style={{ borderRadius: "0 12px 12px 0" }}
                          >
                            ₺
                          </span>
                        </div>
                      </div>

                      {/* Görüntüleme Adı */}
                      <div className="mb-3">
                        <label className="form-label fw-semibold">
                          <i className="fas fa-tag me-1 text-muted"></i>
                          Görüntüleme Adı
                        </label>
                        <input
                          type="text"
                          name="displayName"
                          className="form-control"
                          value={editForm.displayName}
                          onChange={handleFormChange}
                          placeholder="Örn: Motosiklet ile Teslimat"
                          style={{ borderRadius: "10px" }}
                        />
                      </div>

                      {/* Tahmini Süre */}
                      <div className="mb-3">
                        <label className="form-label fw-semibold">
                          <i className="fas fa-clock me-1 text-muted"></i>
                          Tahmini Teslimat Süresi
                        </label>
                        <input
                          type="text"
                          name="estimatedDeliveryTime"
                          className="form-control"
                          value={editForm.estimatedDeliveryTime}
                          onChange={handleFormChange}
                          placeholder="Örn: 30-45 dakika"
                          style={{ borderRadius: "10px" }}
                        />
                      </div>

                      {/* Açıklama */}
                      <div className="mb-4">
                        <label className="form-label fw-semibold">
                          <i className="fas fa-info-circle me-1 text-muted"></i>
                          Açıklama
                        </label>
                        <textarea
                          name="description"
                          className="form-control"
                          value={editForm.description}
                          onChange={handleFormChange}
                          rows="2"
                          placeholder="Müşteriye gösterilecek açıklama"
                          style={{ borderRadius: "10px", resize: "none" }}
                        />
                      </div>

                      {/* Butonlar */}
                      <div className="d-flex gap-2">
                        <button
                          className="btn btn-success flex-grow-1 d-flex align-items-center justify-content-center gap-2"
                          onClick={() => handleSave(setting.id)}
                          disabled={saving}
                          style={{ borderRadius: "10px", padding: "12px" }}
                        >
                          {saving ? (
                            <>
                              <span className="spinner-border spinner-border-sm"></span>
                              <span>Kaydediliyor...</span>
                            </>
                          ) : (
                            <>
                              <i className="fas fa-check"></i>
                              <span>Kaydet</span>
                            </>
                          )}
                        </button>
                        <button
                          className="btn btn-outline-secondary d-flex align-items-center justify-content-center gap-2"
                          onClick={handleCancelEdit}
                          disabled={saving}
                          style={{ borderRadius: "10px", padding: "12px" }}
                        >
                          <i className="fas fa-times"></i>
                          <span className="d-none d-sm-inline">İptal</span>
                        </button>
                      </div>
                    </div>
                  ) : (
                    /* ═══════════════════════════════════════════════════════════════════
                       GÖRÜNTÜLEME MODU
                       ═══════════════════════════════════════════════════════════════════ */
                    <div className="view-mode">
                      {/* Ana Fiyat Gösterimi */}
                      <div className="text-center mb-4">
                        <div
                          className="display-4 fw-bold mb-1"
                          style={{ color: vehicleInfo.color }}
                        >
                          {formatShippingPrice(setting.price)}
                        </div>
                        <small className="text-muted">Kargo Ücreti</small>
                      </div>

                      {/* Detaylar */}
                      <div className="bg-light rounded-3 p-3 mb-4">
                        {/* Tahmini Süre */}
                        <div className="d-flex align-items-center mb-2">
                          <div
                            className="d-flex align-items-center justify-content-center me-3"
                            style={{
                              width: "36px",
                              height: "36px",
                              backgroundColor: vehicleInfo.bgColor,
                              borderRadius: "10px",
                            }}
                          >
                            <i
                              className="fas fa-clock"
                              style={{ color: vehicleInfo.color }}
                            ></i>
                          </div>
                          <div>
                            <small className="text-muted d-block">
                              Tahmini Süre
                            </small>
                            <span className="fw-semibold">
                              {setting.estimatedDeliveryTime || "Belirtilmemiş"}
                            </span>
                          </div>
                        </div>

                        {/* Maksimum Ağırlık */}
                        {setting.maxWeight && (
                          <div className="d-flex align-items-center mb-2">
                            <div
                              className="d-flex align-items-center justify-content-center me-3"
                              style={{
                                width: "36px",
                                height: "36px",
                                backgroundColor: vehicleInfo.bgColor,
                                borderRadius: "10px",
                              }}
                            >
                              <i
                                className="fas fa-weight-hanging"
                                style={{ color: vehicleInfo.color }}
                              ></i>
                            </div>
                            <div>
                              <small className="text-muted d-block">
                                Maks. Ağırlık
                              </small>
                              <span className="fw-semibold">
                                {setting.maxWeight} kg
                              </span>
                            </div>
                          </div>
                        )}

                        {/* Açıklama */}
                        {setting.description && (
                          <div className="d-flex align-items-start">
                            <div
                              className="d-flex align-items-center justify-content-center me-3 flex-shrink-0"
                              style={{
                                width: "36px",
                                height: "36px",
                                backgroundColor: vehicleInfo.bgColor,
                                borderRadius: "10px",
                              }}
                            >
                              <i
                                className="fas fa-info"
                                style={{ color: vehicleInfo.color }}
                              ></i>
                            </div>
                            <div>
                              <small className="text-muted d-block">
                                Açıklama
                              </small>
                              <span className="text-secondary small">
                                {setting.description}
                              </span>
                            </div>
                          </div>
                        )}
                      </div>

                      {/* Son Güncelleme Bilgisi */}
                      {setting.updatedAt && (
                        <div className="text-center mb-3">
                          <small className="text-muted">
                            <i className="fas fa-history me-1"></i>
                            Son güncelleme:{" "}
                            {new Date(setting.updatedAt).toLocaleDateString(
                              "tr-TR",
                              {
                                day: "numeric",
                                month: "long",
                                year: "numeric",
                                hour: "2-digit",
                                minute: "2-digit",
                              },
                            )}
                            {setting.updatedByUserName && (
                              <span className="ms-1">
                                ({setting.updatedByUserName})
                              </span>
                            )}
                          </small>
                        </div>
                      )}

                      {/* Düzenle Butonu */}
                      <button
                        className="btn w-100 d-flex align-items-center justify-content-center gap-2"
                        onClick={() => handleStartEdit(setting)}
                        style={{
                          backgroundColor: vehicleInfo.bgColor,
                          color: vehicleInfo.color,
                          border: `1px solid ${vehicleInfo.color}40`,
                          borderRadius: "12px",
                          padding: "12px",
                          fontWeight: "600",
                          transition: "all 0.2s ease",
                        }}
                        onMouseOver={(e) => {
                          e.currentTarget.style.backgroundColor =
                            vehicleInfo.color;
                          e.currentTarget.style.color = "#fff";
                        }}
                        onMouseOut={(e) => {
                          e.currentTarget.style.backgroundColor =
                            vehicleInfo.bgColor;
                          e.currentTarget.style.color = vehicleInfo.color;
                        }}
                      >
                        <i className="fas fa-edit"></i>
                        <span>Düzenle</span>
                      </button>
                    </div>
                  )}
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {/* ═══════════════════════════════════════════════════════════════════════════════
          BOŞ DURUM
          ═══════════════════════════════════════════════════════════════════════════════ */}
      {settings.length === 0 && !loading && !error && (
        <div className="text-center py-5">
          <div
            className="mx-auto mb-4 d-flex align-items-center justify-content-center"
            style={{
              width: "80px",
              height: "80px",
              backgroundColor: "#f8f9fa",
              borderRadius: "50%",
            }}
          >
            <i className="fas fa-shipping-fast fa-2x text-muted"></i>
          </div>
          <h5 className="text-muted mb-2">Kargo Ayarı Bulunamadı</h5>
          <p className="text-muted small mb-4">
            Henüz kargo ücreti ayarı tanımlanmamış.
          </p>
          <button className="btn btn-primary" onClick={loadSettings}>
            <i className="fas fa-sync-alt me-2"></i>
            Yenile
          </button>
        </div>
      )}

      {/* ═══════════════════════════════════════════════════════════════════════════════
          BİLGİ KARTI
          ═══════════════════════════════════════════════════════════════════════════════ */}
      <div className="row mt-4">
        <div className="col-12">
          <div
            className="card border-0 bg-light"
            style={{ borderRadius: "12px" }}
          >
            <div className="card-body p-3 p-md-4">
              <div className="d-flex align-items-start gap-3">
                <div
                  className="d-flex align-items-center justify-content-center flex-shrink-0"
                  style={{
                    width: "40px",
                    height: "40px",
                    backgroundColor: "#e3f2fd",
                    borderRadius: "10px",
                  }}
                >
                  <i className="fas fa-info-circle text-primary"></i>
                </div>
                <div>
                  <h6 className="fw-bold mb-2">Kargo Ücreti Nasıl Çalışır?</h6>
                  <ul className="mb-0 ps-3 small text-muted">
                    <li className="mb-1">
                      Müşteriler ödeme sayfasında teslimat yöntemini seçer
                      (motosiklet veya araç).
                    </li>
                    <li className="mb-1">
                      Seçilen araç tipine göre burada belirlenen kargo ücreti
                      sepet toplamına eklenir.
                    </li>
                    <li className="mb-1">
                      Kargo ücretleri anında güncellenir - değişiklikler tüm
                      yeni siparişlere uygulanır.
                    </li>
                    <li>Pasif yapılan seçenekler müşterilere gösterilmez.</li>
                  </ul>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
