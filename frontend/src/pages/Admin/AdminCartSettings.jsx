// ==========================================================================
// AdminCartSettings.jsx - Sepet Ayarları Yönetim Sayfası
// ==========================================================================
// Admin panelinde minimum sepet tutarını yönetmek için kullanılır.
// Tek kart tasarımı (singleton ayar), görüntüleme/düzenleme modları.
// Mobil uyumlu, modern ve profesyonel tasarım.
// ==========================================================================

import React, { useState, useEffect, useCallback } from "react";
import {
  getCartSettingsAdmin,
  updateCartSettings,
  formatCurrency,
} from "../../services/cartSettingsService";

// ============================================
// SABİTLER
// ============================================

/**
 * Tema renkleri - turuncu gradient (proje renk paletine uyumlu)
 */
const THEME = {
  primaryColor: "#f57c00",
  secondaryColor: "#ff9800",
  bgColor: "#fff3e0",
  gradientHeader: "linear-gradient(135deg, #f57c00, #ff9800)",
};

/**
 * Form başlangıç değerleri
 */
const initialEditForm = {
  minimumCartAmount: "",
  minimumCartAmountMessage: "",
  isMinimumCartAmountActive: false,
};

// ============================================
// ANA COMPONENT
// ============================================

export default function AdminCartSettings() {
  // State tanımlamaları
  const [settings, setSettings] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);

  // Feedback mesajları
  const [message, setMessage] = useState("");
  const [messageType, setMessageType] = useState("success");

  // Düzenleme modu
  const [isEditing, setIsEditing] = useState(false);
  const [editForm, setEditForm] = useState(initialEditForm);

  // ============================================
  // VERİ YÜKLEME
  // ============================================

  const loadSettings = useCallback(async () => {
    try {
      setLoading(true);
      setError("");
      const data = await getCartSettingsAdmin();
      setSettings(data);
    } catch (err) {
      console.error("Sepet ayarları yüklenemedi:", err);
      setError(err.message || "Sepet ayarları yüklenirken bir hata oluştu");
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
    setTimeout(() => setMessage(""), 4000);
  };

  // ============================================
  // DÜZENLEME İŞLEMLERİ
  // ============================================

  /**
   * Düzenleme moduna geçiş - mevcut değerleri forma yükle
   */
  const handleStartEdit = () => {
    setIsEditing(true);
    setEditForm({
      minimumCartAmount: settings?.minimumCartAmount?.toString() || "0",
      minimumCartAmountMessage: settings?.minimumCartAmountMessage || "",
      isMinimumCartAmountActive: settings?.isMinimumCartAmountActive || false,
    });
  };

  /**
   * Düzenleme iptal
   */
  const handleCancelEdit = () => {
    setIsEditing(false);
    setEditForm(initialEditForm);
  };

  /**
   * Form değişikliği (text/number inputlar)
   */
  const handleFormChange = (e) => {
    const { name, value, type, checked } = e.target;
    setEditForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  /**
   * Kaydetme işlemi
   */
  const handleSave = async () => {
    // Validasyon
    const amount = parseFloat(editForm.minimumCartAmount);
    if (isNaN(amount) || amount < 0) {
      showMessage("Geçerli bir tutar giriniz (0 veya üzeri)", "danger");
      return;
    }

    try {
      setSaving(true);

      await updateCartSettings({
        minimumCartAmount: amount,
        isMinimumCartAmountActive: editForm.isMinimumCartAmountActive,
        minimumCartAmountMessage:
          editForm.minimumCartAmountMessage.trim() || undefined,
      });

      showMessage("Sepet ayarları başarıyla güncellendi!", "success");
      handleCancelEdit();
      await loadSettings();
    } catch (err) {
      console.error("Güncelleme hatası:", err);
      showMessage(
        err.response?.data?.message ||
          err.message ||
          "Güncelleme sırasında bir hata oluştu",
        "danger",
      );
    } finally {
      setSaving(false);
    }
  };

  /**
   * Aktif/Pasif hızlı toggle (kart header'ından)
   */
  const handleQuickToggle = async () => {
    try {
      setSaving(true);
      await updateCartSettings({
        isMinimumCartAmountActive: !settings?.isMinimumCartAmountActive,
      });
      showMessage(
        settings?.isMinimumCartAmountActive
          ? "Minimum sepet tutarı kuralı pasif yapıldı"
          : "Minimum sepet tutarı kuralı aktif yapıldı",
        "success",
      );
      await loadSettings();
    } catch (err) {
      showMessage(err.message || "İşlem başarısız oldu", "danger");
    } finally {
      setSaving(false);
    }
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
              className="spinner-border mb-3"
              style={{
                width: "3rem",
                height: "3rem",
                color: THEME.primaryColor,
              }}
            >
              <span className="visually-hidden">Yükleniyor...</span>
            </div>
            <p className="text-muted">Sepet ayarları yükleniyor...</p>
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
                <i
                  className="fas fa-shopping-cart"
                  style={{ color: THEME.primaryColor }}
                ></i>
                <span>Sepet Ayarları</span>
              </h2>
              <p className="text-muted mb-0 small">
                Minimum sepet tutarı ve sepet kurallarını yönetin
              </p>
            </div>

            {/* Yenile Butonu */}
            <button
              className="btn btn-outline-secondary d-flex align-items-center gap-2"
              onClick={loadSettings}
              disabled={loading}
              style={{
                borderColor: THEME.primaryColor,
                color: THEME.primaryColor,
              }}
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
          MİNİMUM SEPET TUTARI KARTI
          ═══════════════════════════════════════════════════════════════════════════════ */}
      <div className="row justify-content-center">
        <div className="col-12 col-md-8 col-lg-6">
          <div
            className={`card border-0 shadow-sm overflow-hidden ${
              !settings?.isMinimumCartAmountActive ? "opacity-75" : ""
            }`}
            style={{
              borderRadius: "16px",
              transition: "all 0.3s ease",
            }}
          >
            {/* Kart Üst Kısım - Turuncu Gradient */}
            <div
              className="card-header border-0 py-3 py-md-4"
              style={{ background: THEME.gradientHeader }}
            >
              <div className="d-flex justify-content-between align-items-center">
                <div className="d-flex align-items-center gap-3">
                  {/* Sepet İkonu */}
                  <div
                    className="d-flex align-items-center justify-content-center rounded-circle"
                    style={{
                      width: "50px",
                      height: "50px",
                      backgroundColor: "rgba(255,255,255,0.2)",
                    }}
                  >
                    <i className="fas fa-shopping-cart fa-lg text-white"></i>
                  </div>

                  {/* Başlık */}
                  <div>
                    <h5 className="mb-0 text-white fw-bold">
                      Minimum Sepet Tutarı
                    </h5>
                    <small className="text-white" style={{ opacity: 0.7 }}>
                      Sipariş için gereken minimum tutar
                    </small>
                  </div>
                </div>

                {/* Aktif/Pasif Toggle */}
                <div className="form-check form-switch">
                  <input
                    type="checkbox"
                    className="form-check-input"
                    id="minCartActive"
                    checked={settings?.isMinimumCartAmountActive || false}
                    onChange={handleQuickToggle}
                    disabled={saving}
                    style={{
                      width: "3rem",
                      height: "1.5rem",
                      cursor: "pointer",
                    }}
                  />
                  <label
                    className="form-check-label text-white small"
                    htmlFor="minCartActive"
                    style={{ cursor: "pointer" }}
                  >
                    {settings?.isMinimumCartAmountActive ? "Aktif" : "Pasif"}
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
                  {/* Aktif/Pasif toggle (form içinde) */}
                  <div className="mb-3">
                    <div className="form-check form-switch d-flex align-items-center gap-2">
                      <input
                        type="checkbox"
                        className="form-check-input"
                        id="editMinCartActive"
                        name="isMinimumCartAmountActive"
                        checked={editForm.isMinimumCartAmountActive}
                        onChange={handleFormChange}
                        style={{
                          width: "2.5rem",
                          height: "1.25rem",
                          cursor: "pointer",
                        }}
                      />
                      <label
                        className="form-check-label fw-semibold"
                        htmlFor="editMinCartActive"
                        style={{ cursor: "pointer" }}
                      >
                        Minimum Sepet Tutarı Kuralı{" "}
                        <span
                          className={`badge ${editForm.isMinimumCartAmountActive ? "bg-success" : "bg-secondary"}`}
                        >
                          {editForm.isMinimumCartAmountActive
                            ? "Aktif"
                            : "Pasif"}
                        </span>
                      </label>
                    </div>
                  </div>

                  {/* Tutar Input */}
                  <div className="mb-3">
                    <label className="form-label fw-semibold">
                      <i className="fas fa-lira-sign me-1 text-muted"></i>
                      Minimum Tutar (TL)
                    </label>
                    <div className="input-group input-group-lg">
                      <input
                        type="number"
                        name="minimumCartAmount"
                        className="form-control"
                        value={editForm.minimumCartAmount}
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
                    <small className="text-muted mt-1 d-block">
                      0 girilirse minimum tutar zorunluluğu olmaz
                    </small>
                  </div>

                  {/* Mesaj Textarea */}
                  <div className="mb-4">
                    <label className="form-label fw-semibold">
                      <i className="fas fa-comment-alt me-1 text-muted"></i>
                      Uyarı Mesajı
                    </label>
                    <textarea
                      name="minimumCartAmountMessage"
                      className="form-control"
                      value={editForm.minimumCartAmountMessage}
                      onChange={handleFormChange}
                      rows="3"
                      placeholder="Sipariş verebilmek için sepet tutarınız en az {amount} TL olmalıdır."
                      style={{ borderRadius: "10px", resize: "none" }}
                    />
                    <small className="text-muted mt-1 d-block">
                      <code>{"{amount}"}</code> yazdığınız yere minimum tutar
                      otomatik yerleştirilir
                    </small>
                  </div>

                  {/* Butonlar */}
                  <div className="d-flex gap-2">
                    <button
                      className="btn btn-success flex-grow-1 d-flex align-items-center justify-content-center gap-2"
                      onClick={handleSave}
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
                  {/* Durum Badge */}
                  <div className="text-center mb-3">
                    <span
                      className={`badge rounded-pill px-3 py-2 ${
                        settings?.isMinimumCartAmountActive
                          ? "bg-success"
                          : "bg-secondary"
                      }`}
                      style={{ fontSize: "0.85rem" }}
                    >
                      <i
                        className={`fas ${settings?.isMinimumCartAmountActive ? "fa-check-circle" : "fa-pause-circle"} me-1`}
                      ></i>
                      {settings?.isMinimumCartAmountActive
                        ? "Kural Aktif"
                        : "Kural Pasif"}
                    </span>
                  </div>

                  {/* Ana Tutar Gösterimi */}
                  <div className="text-center mb-4">
                    <div
                      className="display-4 fw-bold mb-1"
                      style={{ color: THEME.primaryColor }}
                    >
                      {formatCurrency(settings?.minimumCartAmount || 0)}
                    </div>
                    <small className="text-muted">Minimum Sipariş Tutarı</small>
                  </div>

                  {/* Detaylar */}
                  <div className="bg-light rounded-3 p-3 mb-4">
                    {/* Mesaj */}
                    <div className="d-flex align-items-start mb-2">
                      <div
                        className="d-flex align-items-center justify-content-center me-3 flex-shrink-0"
                        style={{
                          width: "36px",
                          height: "36px",
                          backgroundColor: THEME.bgColor,
                          borderRadius: "10px",
                        }}
                      >
                        <i
                          className="fas fa-comment-alt"
                          style={{ color: THEME.primaryColor }}
                        ></i>
                      </div>
                      <div>
                        <small className="text-muted d-block">
                          Müşteriye Gösterilen Mesaj
                        </small>
                        <span className="text-secondary small">
                          {settings?.minimumCartAmountMessage
                            ? settings.minimumCartAmountMessage.replace(
                                "{amount}",
                                (
                                  settings?.minimumCartAmount || 0
                                ).toLocaleString("tr-TR", {
                                  minimumFractionDigits: 2,
                                }),
                              )
                            : "Belirtilmemiş"}
                        </span>
                      </div>
                    </div>

                    {/* Son Güncelleme */}
                    {settings?.updatedAt && (
                      <div className="d-flex align-items-center">
                        <div
                          className="d-flex align-items-center justify-content-center me-3 flex-shrink-0"
                          style={{
                            width: "36px",
                            height: "36px",
                            backgroundColor: THEME.bgColor,
                            borderRadius: "10px",
                          }}
                        >
                          <i
                            className="fas fa-history"
                            style={{ color: THEME.primaryColor }}
                          ></i>
                        </div>
                        <div>
                          <small className="text-muted d-block">
                            Son Güncelleme
                          </small>
                          <span className="fw-semibold small">
                            {new Date(settings.updatedAt).toLocaleDateString(
                              "tr-TR",
                              {
                                day: "numeric",
                                month: "long",
                                year: "numeric",
                                hour: "2-digit",
                                minute: "2-digit",
                              },
                            )}
                            {settings.updatedByUserName && (
                              <span className="text-muted ms-1">
                                ({settings.updatedByUserName})
                              </span>
                            )}
                          </span>
                        </div>
                      </div>
                    )}
                  </div>

                  {/* Düzenle Butonu */}
                  <button
                    className="btn w-100 d-flex align-items-center justify-content-center gap-2"
                    onClick={handleStartEdit}
                    style={{
                      backgroundColor: THEME.bgColor,
                      color: THEME.primaryColor,
                      border: `1px solid ${THEME.primaryColor}40`,
                      borderRadius: "12px",
                      padding: "12px",
                      fontWeight: "600",
                      transition: "all 0.2s ease",
                    }}
                    onMouseOver={(e) => {
                      e.currentTarget.style.backgroundColor =
                        THEME.primaryColor;
                      e.currentTarget.style.color = "#fff";
                    }}
                    onMouseOut={(e) => {
                      e.currentTarget.style.backgroundColor = THEME.bgColor;
                      e.currentTarget.style.color = THEME.primaryColor;
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
      </div>

      {/* ═══════════════════════════════════════════════════════════════════════════════
          BİLGİ KARTI
          ═══════════════════════════════════════════════════════════════════════════════ */}
      <div className="row justify-content-center mt-4">
        <div className="col-12 col-md-8 col-lg-6">
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
                  <h6 className="fw-bold mb-2">
                    Minimum Sepet Tutarı Nasıl Çalışır?
                  </h6>
                  <ul className="mb-0 ps-3 small text-muted">
                    <li className="mb-1">
                      Müşterinin sepet toplamı belirlenen minimum tutarın
                      altında olduğunda sipariş verememesi sağlanır.
                    </li>
                    <li className="mb-1">
                      Sepet sayfasında kalan tutar ve ilerleme çubuğu ile
                      müşteri bilgilendirilir.
                    </li>
                    <li className="mb-1">
                      Ödeme sayfasında da aynı kontrol yapılır ve sipariş butonu
                      devre dışı bırakılır.
                    </li>
                    <li>
                      Kural pasif yapıldığında tüm müşteriler tutar sınırı
                      olmadan sipariş verebilir.
                    </li>
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
