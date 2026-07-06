import React, { useState } from "react";
import {
  getOrderActions,
  CANCEL_MODE,
  canDownloadInvoice,
  getOrderRefundChip,
  isCashOnDelivery,
} from "../../utils/orderCancelPolicy";
import { openWhatsAppSupportAsync } from "../../utils/customerSupport";
import { OrderService } from "../../services/orderService";
import "./OrderActions.css";

/**
 * Sipariş iptal / WhatsApp / fatura aksiyon barı — tüm sipariş ekranlarında ortak.
 */
export default function OrderActions({
  order,
  onCancel,
  isCancelling = false,
  isDownloadingInvoice = false,
  layout = "inline",
  refundRequests = [],
  isAuthenticated = true,
}) {
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [localDownloading, setLocalDownloading] = useState(false);
  const actions = getOrderActions(order, { isAuthenticated });
  const showInvoice = canDownloadInvoice(order, { isAuthenticated });
  const refundChip = getOrderRefundChip(order, refundRequests);
  const isCod = isCashOnDelivery(order);

  const handleCancelClick = () => {
    setConfirmOpen(true);
  };

  const handleConfirmCancel = async () => {
    setConfirmOpen(false);
    if (onCancel) {
      await onCancel(order?.id || order?.orderId, actions.orderNumber);
    }
  };

  const handleWhatsApp = () => {
    openWhatsAppSupportAsync(
      actions.orderNumber,
      actions.cancelMode === CANCEL_MODE.WHATSAPP || refundChip?.type === "failed"
        ? "cancel"
        : "destek",
    );
  };

  const handleDownloadInvoice = async () => {
    const orderId = order?.id || order?.orderId;
    if (!orderId) return;

    setLocalDownloading(true);
    try {
      await OrderService.downloadInvoice(
        orderId,
        order?.trackingNumber || order?.orderNumber,
      );
    } catch (err) {
      console.error("[OrderActions] Fatura indirme hatası:", err);
      alert(
        err?.message ||
          "Fatura indirilemedi. Ödeme tamamlandıysa bir süre sonra tekrar deneyin.",
      );
    } finally {
      setLocalDownloading(false);
    }
  };

  const downloading = isDownloadingInvoice || localDownloading;

  if (
    actions.cancelMode === CANCEL_MODE.NONE &&
    !refundChip &&
    !showInvoice
  ) {
    return null;
  }

  const cancelConfirmText = isCod
    ? " numaralı siparişiniz iptal edilecektir. Kapıda ödeme seçtiğiniz için tahsilat yapılmayacaktır. Devam etmek istiyor musunuz?"
    : " numaralı siparişiniz iptal edilecek ve ödemeniz iade sürecine alınacaktır. Devam etmek istiyor musunuz?";

  return (
    <div className={`order-actions order-actions--${layout}`}>
      {refundChip && (
        <div
          className={`order-actions__refund-chip order-actions__refund-chip--${refundChip.tone}`}
        >
          <i className={`fas fa-${refundChip.icon} me-1`} />
          {refundChip.label}
          {refundChip.detail && (
            <span className="order-actions__refund-chip-detail">
              {refundChip.detail}
            </span>
          )}
        </div>
      )}

      {actions.disabledReason && !actions.showCancel && (
        <p className="order-actions__hint">{actions.disabledReason}</p>
      )}

      <div className="order-actions__buttons">
        {showInvoice && (
          <button
            type="button"
            className="order-actions__btn order-actions__btn--invoice"
            onClick={handleDownloadInvoice}
            disabled={downloading}
          >
            {downloading ? (
              <>
                <span className="spinner-border spinner-border-sm me-2" />
                İndiriliyor...
              </>
            ) : (
              <>
                <i className="fas fa-file-pdf me-2" />
                Faturayı İndir
              </>
            )}
          </button>
        )}

        {actions.showCancel && onCancel && (
          <button
            type="button"
            className="order-actions__btn order-actions__btn--cancel"
            onClick={handleCancelClick}
            disabled={isCancelling}
          >
            {isCancelling ? (
              <>
                <span className="spinner-border spinner-border-sm me-2" />
                İptal Ediliyor...
              </>
            ) : (
              <>
                <i className="fas fa-times me-2" />
                {actions.cancelLabel}
              </>
            )}
          </button>
        )}

        {actions.showWhatsApp && (
          <button
            type="button"
            className={`order-actions__btn order-actions__btn--whatsapp ${
              actions.whatsAppPrimary || refundChip?.type === "failed"
                ? "order-actions__btn--whatsapp-primary"
                : ""
            }`}
            onClick={handleWhatsApp}
          >
            <i className="fab fa-whatsapp me-2" />
            {refundChip?.type === "failed"
              ? "Destek Al (İade)"
              : actions.whatsAppPrimary
                ? "WhatsApp ile İptal/İade"
                : "Destek Al"}
          </button>
        )}
      </div>

      {confirmOpen && (
        <div className="order-actions__confirm-overlay" role="dialog" aria-modal="true">
          <div className="order-actions__confirm">
            <h6>Siparişi İptal Et</h6>
            <p>
              <strong>{actions.orderNumber}</strong>
              {cancelConfirmText}
            </p>
            <div className="order-actions__confirm-buttons">
              <button
                type="button"
                className="btn btn-outline-secondary btn-sm"
                onClick={() => setConfirmOpen(false)}
              >
                Vazgeç
              </button>
              <button
                type="button"
                className="btn btn-danger btn-sm"
                onClick={handleConfirmCancel}
              >
                Evet, İptal Et
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
