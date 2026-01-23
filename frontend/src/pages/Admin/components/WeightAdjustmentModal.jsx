// ==========================================================================
// WeightAdjustmentModal.jsx - Manuel Düzeltme Modal Komponenti
// ==========================================================================
// Admin'in ağırlık fark kayıtlarında manuel düzeltme yapması için
// kullanılan modal. Fiyat düzeltme, not ekleme ve iptal işlemleri içerir.
// ==========================================================================

import React, { useState, useEffect } from "react";
import PropTypes from "prop-types";

/**
 * WeightAdjustmentModal - Manuel Düzeltme Modal
 *
 * @param {Object} adjustment - Düzenlenecek fark kaydı
 * @param {Function} onSave - Kaydetme callback
 * @param {Function} onClose - Modal kapatma callback
 */
export default function WeightAdjustmentModal({ adjustment, onSave, onClose }) {
  // Form state
  const [formData, setFormData] = useState({
    newAmount: 0,
    notes: "",
    action: "adjust", // adjust, cancel, forceApprove
  });
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState({});

  // Modal açıldığında değerleri ayarla
  useEffect(() => {
    if (adjustment) {
      setFormData({
        newAmount: adjustment.differenceAmount || 0,
        notes: "",
        action: "adjust",
      });
    }
  }, [adjustment]);

  /**
   * Form değişikliği
   */
  const handleChange = (field, value) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    // Hata temizle
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: null }));
    }
  };

  /**
   * Form validasyonu
   */
  const validateForm = () => {
    const newErrors = {};

    if (formData.action === "adjust") {
      if (formData.newAmount === undefined || formData.newAmount === "") {
        newErrors.newAmount = "Tutar gerekli";
      }
    }

    if (!formData.notes || formData.notes.trim().length < 5) {
      newErrors.notes = "Lütfen bir açıklama girin (min 5 karakter)";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  /**
   * Formu kaydet
   */
  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!validateForm()) return;

    setSaving(true);
    try {
      await onSave({
        id: adjustment.id,
        newAmount: parseFloat(formData.newAmount),
        notes: formData.notes,
        action: formData.action,
      });
    } catch (err) {
      console.error("Kaydetme hatası:", err);
    } finally {
      setSaving(false);
    }
  };

  /**
   * ESC tuşu ile kapatma
   */
  useEffect(() => {
    const handleEsc = (e) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", handleEsc);
    return () => window.removeEventListener("keydown", handleEsc);
  }, [onClose]);

  if (!adjustment) return null;

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div
        className="weight-adjustment-modal"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Modal Header */}
        <div className="modal-header">
          <h5 className="modal-title">
            <i className="fas fa-edit me-2"></i>
            Fark Kaydı Düzenleme
          </h5>
          <button
            type="button"
            className="btn-close"
            onClick={onClose}
            aria-label="Kapat"
          ></button>
        </div>

        {/* Modal Body */}
        <div className="modal-body">
          {/* Kayıt Bilgisi */}
          <div className="adjustment-info">
            <div className="info-row">
              <span className="info-label">Sipariş</span>
              <span className="info-value">
                #{adjustment.orderNumber || adjustment.orderId}
              </span>
            </div>
            <div className="info-row">
              <span className="info-label">Ürün</span>
              <span className="info-value">{adjustment.productName}</span>
            </div>
            <div className="info-row">
              <span className="info-label">Müşteri</span>
              <span className="info-value">{adjustment.customerName}</span>
            </div>
            <div className="info-row">
              <span className="info-label">Kurye</span>
              <span className="info-value">{adjustment.courierName}</span>
            </div>
          </div>

          {/* Ağırlık Özeti */}
          <div className="weight-summary">
            <div className="weight-box">
              <span className="box-label">Tahmini</span>
              <span className="box-value">
                {(adjustment.estimatedWeightGrams / 1000).toFixed(2)} kg
              </span>
              <span className="box-price">
                {adjustment.estimatedPrice?.toFixed(2)} ₺
              </span>
            </div>
            <div className="weight-arrow">
              <i className="fas fa-arrow-right"></i>
            </div>
            <div className="weight-box highlight">
              <span className="box-label">Gerçek</span>
              <span className="box-value">
                {(adjustment.actualWeightGrams / 1000).toFixed(2)} kg
              </span>
              <span className="box-price">
                {adjustment.actualPrice?.toFixed(2)} ₺
              </span>
            </div>
            <div className="weight-box difference">
              <span className="box-label">Mevcut Fark</span>
              <span
                className={`box-value ${adjustment.differenceAmount >= 0 ? "text-success" : "text-warning"}`}
              >
                {adjustment.differenceAmount >= 0 ? "+" : ""}
                {adjustment.differenceAmount?.toFixed(2)} ₺
              </span>
              <span className="box-percent">
                %{adjustment.differencePercent?.toFixed(1)}
              </span>
            </div>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit}>
            {/* İşlem Tipi */}
            <div className="mb-3">
              <label className="form-label">İşlem Tipi</label>
              <div className="action-buttons">
                <button
                  type="button"
                  className={`btn ${formData.action === "adjust" ? "btn-primary" : "btn-outline-primary"}`}
                  onClick={() => handleChange("action", "adjust")}
                >
                  <i className="fas fa-edit me-2"></i>
                  Tutarı Düzenle
                </button>
                <button
                  type="button"
                  className={`btn ${formData.action === "forceApprove" ? "btn-success" : "btn-outline-success"}`}
                  onClick={() => handleChange("action", "forceApprove")}
                >
                  <i className="fas fa-check me-2"></i>
                  Olduğu Gibi Onayla
                </button>
                <button
                  type="button"
                  className={`btn ${formData.action === "cancel" ? "btn-danger" : "btn-outline-danger"}`}
                  onClick={() => handleChange("action", "cancel")}
                >
                  <i className="fas fa-times me-2"></i>
                  İptal Et
                </button>
              </div>
            </div>

            {/* Yeni Tutar (sadece düzenleme modunda) */}
            {formData.action === "adjust" && (
              <div className="mb-3">
                <label className="form-label">Yeni Fark Tutarı (₺)</label>
                <div className="input-group">
                  <input
                    type="number"
                    step="0.01"
                    className={`form-control form-control-lg ${errors.newAmount ? "is-invalid" : ""}`}
                    value={formData.newAmount}
                    onChange={(e) => handleChange("newAmount", e.target.value)}
                    placeholder="0.00"
                  />
                  <span className="input-group-text">₺</span>
                </div>
                {errors.newAmount && (
                  <div className="invalid-feedback d-block">
                    {errors.newAmount}
                  </div>
                )}
                <small className="text-muted">
                  Pozitif değer: Müşteriden tahsil edilecek, Negatif değer:
                  Müşteriye iade edilecek
                </small>
              </div>
            )}

            {/* Hızlı Tutar Butonları */}
            {formData.action === "adjust" && (
              <div className="quick-amounts mb-3">
                <span className="quick-label">Hızlı Seçim:</span>
                <button
                  type="button"
                  className="btn btn-sm btn-outline-secondary"
                  onClick={() => handleChange("newAmount", 0)}
                >
                  0 ₺
                </button>
                <button
                  type="button"
                  className="btn btn-sm btn-outline-secondary"
                  onClick={() =>
                    handleChange(
                      "newAmount",
                      (adjustment.differenceAmount / 2).toFixed(2),
                    )
                  }
                >
                  Yarısı
                </button>
                <button
                  type="button"
                  className="btn btn-sm btn-outline-secondary"
                  onClick={() =>
                    handleChange(
                      "newAmount",
                      adjustment.differenceAmount?.toFixed(2),
                    )
                  }
                >
                  Tamamı
                </button>
              </div>
            )}

            {/* Not/Açıklama */}
            <div className="mb-3">
              <label className="form-label">
                Açıklama <span className="text-danger">*</span>
              </label>
              <textarea
                className={`form-control ${errors.notes ? "is-invalid" : ""}`}
                rows="3"
                value={formData.notes}
                onChange={(e) => handleChange("notes", e.target.value)}
                placeholder="İşlem nedenini açıklayın..."
              ></textarea>
              {errors.notes && (
                <div className="invalid-feedback">{errors.notes}</div>
              )}
            </div>

            {/* Uyarı Mesajları */}
            {formData.action === "cancel" && (
              <div className="alert alert-warning">
                <i className="fas fa-exclamation-triangle me-2"></i>
                <strong>Dikkat!</strong> Bu işlemi iptal ettiğinizde, ağırlık
                farkı dikkate alınmayacak ve müşteriden/müşteriye herhangi bir
                ödeme alınmayacak/yapılmayacak.
              </div>
            )}

            {formData.action === "forceApprove" && (
              <div className="alert alert-info">
                <i className="fas fa-info-circle me-2"></i>
                Mevcut fark tutarı ({adjustment.differenceAmount?.toFixed(2)} ₺)
                olduğu gibi işleme alınacak.
              </div>
            )}
          </form>
        </div>

        {/* Modal Footer */}
        <div className="modal-footer">
          <button
            type="button"
            className="btn btn-secondary"
            onClick={onClose}
            disabled={saving}
          >
            İptal
          </button>
          <button
            type="button"
            className={`btn ${
              formData.action === "cancel"
                ? "btn-danger"
                : formData.action === "forceApprove"
                  ? "btn-success"
                  : "btn-primary"
            }`}
            onClick={handleSubmit}
            disabled={saving}
          >
            {saving ? (
              <>
                <span className="spinner-border spinner-border-sm me-2"></span>
                Kaydediliyor...
              </>
            ) : (
              <>
                <i
                  className={`fas ${
                    formData.action === "cancel"
                      ? "fa-times"
                      : formData.action === "forceApprove"
                        ? "fa-check"
                        : "fa-save"
                  } me-2`}
                ></i>
                {formData.action === "cancel"
                  ? "İptal Et"
                  : formData.action === "forceApprove"
                    ? "Onayla"
                    : "Kaydet"}
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
}

WeightAdjustmentModal.propTypes = {
  adjustment: PropTypes.shape({
    id: PropTypes.number.isRequired,
    orderId: PropTypes.number,
    orderNumber: PropTypes.string,
    productName: PropTypes.string,
    customerName: PropTypes.string,
    courierName: PropTypes.string,
    estimatedWeightGrams: PropTypes.number,
    actualWeightGrams: PropTypes.number,
    differenceGrams: PropTypes.number,
    differencePercent: PropTypes.number,
    estimatedPrice: PropTypes.number,
    actualPrice: PropTypes.number,
    differenceAmount: PropTypes.number,
  }),
  onSave: PropTypes.func.isRequired,
  onClose: PropTypes.func.isRequired,
};
