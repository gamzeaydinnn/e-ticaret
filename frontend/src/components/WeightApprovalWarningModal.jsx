import React from "react";
import "./WeightApprovalWarningModal.css";

const WeightApprovalWarningModal = ({
  isOpen,
  onClose,
  onConfirm,
  orderData,
  weightReport,
}) => {
  if (!isOpen) return null;

  const hasPendingWeight = weightReport?.status === "Pending";

  return (
    <div className="weight-modal-overlay">
      <div className="weight-modal-container">
        <div className="weight-modal-header">
          <div className="weight-modal-icon-wrapper">
            {hasPendingWeight ? (
              <i className="fas fa-exclamation-triangle weight-modal-icon-warning"></i>
            ) : (
              <i className="fas fa-check-circle weight-modal-icon-success"></i>
            )}
          </div>
          <h3 className="weight-modal-title">
            {hasPendingWeight
              ? "⚠️ Ağırlık Onayı Bekleniyor"
              : "✅ Teslimat Onayı"}
          </h3>
        </div>

        <div className="weight-modal-body">
          {hasPendingWeight ? (
            <>
              <div className="weight-alert weight-alert-warning">
                <div className="weight-alert-icon">
                  <i className="fas fa-weight-hanging"></i>
                </div>
                <div className="weight-alert-content">
                  <h4>Bu siparişte onaylanmamış ağırlık farkı var!</h4>
                  <p>
                    Admin onayı bekleyen ağırlık fazlalığı tespit edildi. Teslim
                    etmeden önce admin onayını beklemeniz gerekmektedir.
                  </p>
                </div>
              </div>

              <div className="weight-info-card">
                <div className="weight-info-row">
                  <span className="weight-info-label">
                    <i className="fas fa-balance-scale"></i> Beklenen Ağırlık
                  </span>
                  <span className="weight-info-value">
                    {weightReport.expectedWeightGrams}g
                  </span>
                </div>
                <div className="weight-info-row">
                  <span className="weight-info-label">
                    <i className="fas fa-weight"></i> Tartılan Ağırlık
                  </span>
                  <span className="weight-info-value weight-reported">
                    {weightReport.reportedWeightGrams}g
                  </span>
                </div>
                <div className="weight-info-divider"></div>
                <div className="weight-info-row weight-highlight">
                  <span className="weight-info-label">
                    <i className="fas fa-arrow-up"></i> Fazlalık
                  </span>
                  <span className="weight-info-value weight-overage">
                    +{weightReport.overageGrams}g
                  </span>
                </div>
                <div className="weight-info-row weight-highlight">
                  <span className="weight-info-label">
                    <i className="fas fa-lira-sign"></i> Ek Ücret
                  </span>
                  <span className="weight-info-value weight-amount">
                    +{weightReport.overageAmount} ₺
                  </span>
                </div>
              </div>

              <div className="weight-status-badge weight-status-pending">
                <i className="fas fa-clock"></i>
                <span>Admin Onayı Bekleniyor</span>
              </div>

              <div className="weight-instruction">
                <i className="fas fa-info-circle"></i>
                <p>
                  Lütfen admin onayını bekleyin. Onay geldiğinde bildirim
                  alacaksınız ve teslimat işlemini tamamlayabileceksiniz.
                </p>
              </div>
            </>
          ) : (
            <>
              <div className="weight-alert weight-alert-success">
                <div className="weight-alert-icon">
                  <i className="fas fa-check-double"></i>
                </div>
                <div className="weight-alert-content">
                  <h4>Teslimat için hazır</h4>
                  <p>
                    {weightReport
                      ? "Ağırlık fazlalığı admin tarafından onaylanmıştır. Teslim ettiğinizde ek ücret otomatik olarak tahsil edilecektir."
                      : "Bu sipariş için teslimat yapabilirsiniz."}
                  </p>
                </div>
              </div>

              <div className="weight-order-summary">
                <h4>
                  <i className="fas fa-box"></i> Sipariş Özeti
                </h4>
                <div className="weight-summary-item">
                  <span>Müşteri:</span>
                  <strong>{orderData.customerName}</strong>
                </div>
                <div className="weight-summary-item">
                  <span>Sipariş No:</span>
                  <strong>#{orderData.id}</strong>
                </div>
                <div className="weight-summary-item">
                  <span>Tutar:</span>
                  <strong>{orderData.totalAmount.toFixed(2)} ₺</strong>
                </div>

                {weightReport && (
                  <>
                    <div className="weight-summary-divider"></div>
                    <div className="weight-summary-item weight-summary-extra">
                      <span>
                        <i className="fas fa-weight-hanging"></i> Ağırlık Farkı:
                      </span>
                      <strong className="text-warning">
                        +{weightReport.overageGrams}g
                      </strong>
                    </div>
                    <div className="weight-summary-item weight-summary-extra">
                      <span>
                        <i className="fas fa-plus-circle"></i> Ek Ücret:
                      </span>
                      <strong className="text-warning">
                        +{weightReport.overageAmount} ₺
                      </strong>
                    </div>
                    <div className="weight-summary-divider"></div>
                    <div className="weight-summary-item weight-summary-total">
                      <span>Toplam Tahsilat:</span>
                      <strong className="text-success">
                        {(
                          parseFloat(orderData.totalAmount) +
                          parseFloat(weightReport.overageAmount)
                        ).toFixed(2)}{" "}
                        ₺
                      </strong>
                    </div>
                  </>
                )}
              </div>

              {weightReport && (
                <div className="weight-status-badge weight-status-approved">
                  <i className="fas fa-check-circle"></i>
                  <span>Ağırlık Onaylandı</span>
                </div>
              )}
            </>
          )}
        </div>

        <div className="weight-modal-footer">
          <button className="weight-btn weight-btn-secondary" onClick={onClose}>
            <i className="fas fa-times"></i>
            {hasPendingWeight ? "Kapat" : "İptal"}
          </button>

          {!hasPendingWeight && (
            <button
              className="weight-btn weight-btn-primary"
              onClick={onConfirm}
            >
              <i className="fas fa-check"></i>
              Teslim Et & Tahsil Et
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default WeightApprovalWarningModal;
