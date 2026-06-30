/**
 * CartActionToast - Sepet / favori işlemleri için sağ üst bildirim
 *
 * Backend sepete ekleme başarılı olduğunda gösterilir.
 * AddToCartModal (ortadaki popup) yerine tek kaynak olarak kullanılır.
 */
import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";

const AUTO_DISMISS_MS = {
  success: 5000,
  favorite: 3000,
  error: 4000,
};

export function useCartActionToast() {
  const [notification, setNotification] = useState(null);
  const timerRef = useRef(null);

  const clearTimer = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
  }, []);

  const dismiss = useCallback(() => {
    clearTimer();
    setNotification(null);
  }, [clearTimer]);

  const show = useCallback(
    (payload, durationMs) => {
      clearTimer();
      setNotification(payload);
      if (durationMs > 0) {
        timerRef.current = setTimeout(() => {
          setNotification(null);
          timerRef.current = null;
        }, durationMs);
      }
    },
    [clearTimer],
  );

  const showCartSuccess = useCallback(
    (product, userType = "registered", extra = {}) => {
      show(
        { type: "success", product, userType, ...extra },
        AUTO_DISMISS_MS.success,
      );
    },
    [show],
  );

  const showCartError = useCallback(
    (message) => {
      show({ type: "error", message }, AUTO_DISMISS_MS.error);
    },
    [show],
  );

  const showFavoriteSuccess = useCallback(
    (product) => {
      show(
        { type: "favorite", product, message: "Favorilere eklendi!" },
        AUTO_DISMISS_MS.favorite,
      );
    },
    [show],
  );

  useEffect(() => () => clearTimer(), [clearTimer]);

  return {
    notification,
    showCartSuccess,
    showCartError,
    showFavoriteSuccess,
    dismiss,
  };
}

export default function CartActionToast({
  notification,
  onDismiss,
  guestFirstOrderShippingMessage = "",
}) {
  const navigate = useNavigate();

  if (!notification) return null;

  const isSuccess = notification.type === "success";
  const isFavorite = notification.type === "favorite";
  const isError = notification.type === "error";

  return (
    <div
      className="cart-action-toast-wrapper position-fixed d-flex justify-content-center"
      style={{ 
        zIndex: 13000,
        top: "12px",
        left: "12px",
        right: "12px",
      }}
    >
      <div
        className="toast show border-0 shadow-lg"
        style={{
          borderRadius: "16px",
          width: "100%",
          maxWidth: "380px",
          background: isSuccess
            ? "linear-gradient(145deg, #ffffff, #f8f9fa)"
            : isFavorite
              ? "linear-gradient(145deg, #fdf2f8, #fce7f3)"
              : "linear-gradient(145deg, #fff5f5, #fed7d7)",
          animation: "slideInDown 0.4s cubic-bezier(0.34, 1.56, 0.64, 1)",
          border: isSuccess
            ? "2px solid #10b981"
            : isFavorite
              ? "2px solid #e91e63"
              : "2px solid #ef4444",
          boxShadow: "0 10px 30px rgba(0,0,0,0.15)",
        }}
      >
        <div
          className="toast-header border-0"
          style={{
            background: isSuccess
              ? "linear-gradient(135deg, #10b981, #059669)"
              : isFavorite
                ? "linear-gradient(135deg, #e91e63, #ad1457)"
                : "linear-gradient(135deg, #ef4444, #dc2626)",
            borderRadius: "14px 14px 0 0",
            color: "white",
            padding: "10px 14px",
          }}
        >
          <div className="d-flex align-items-center">
            <div
              className="rounded-circle me-2 d-flex align-items-center justify-content-center"
              style={{
                width: "28px",
                height: "28px",
                minWidth: "28px",
                backgroundColor: "rgba(255,255,255,0.2)",
              }}
            >
              <i
                className={`fas ${
                  isSuccess ? "fa-check" : isFavorite ? "fa-heart" : "fa-exclamation"
                }`}
                style={{ fontSize: "0.85rem" }}
              ></i>
            </div>
            <strong style={{ fontSize: "0.9rem" }}>
              {isSuccess
                ? "Sepete Eklendi!"
                : isFavorite
                  ? "Favorilere Eklendi!"
                  : "Hata Oluştu!"}
            </strong>
          </div>
          <button
            type="button"
            className="btn-close btn-close-white ms-auto"
            onClick={onDismiss}
            style={{ opacity: 0.8, fontSize: "0.7rem" }}
            aria-label="Kapat"
          ></button>
        </div>

        {isSuccess && notification.product && (
          <div className="toast-body" style={{ padding: "12px 14px" }}>
            <div className="d-flex align-items-center">
              <div className="position-relative me-3 flex-shrink-0">
                <img
                  src={
                    notification.product.imageUrl ||
                    notification.product.image ||
                    "/images/placeholder.png"
                  }
                  alt={notification.product.name}
                  style={{
                    width: "50px",
                    height: "50px",
                    objectFit: "contain",
                    borderRadius: "10px",
                    border: "2px solid #10b981",
                    background: "#f8f9fa",
                    padding: "3px",
                  }}
                  onError={(e) => {
                    e.target.src = "/images/placeholder.png";
                  }}
                />
              </div>

              <div className="flex-grow-1 min-w-0" style={{ overflow: "hidden" }}>
                <h6
                  className="mb-1 fw-bold text-dark"
                  style={{ 
                    fontSize: "0.8rem",
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                  }}
                >
                  {notification.product.name}
                </h6>
                <div className="d-flex align-items-center">
                  <span className="text-success fw-bold" style={{ fontSize: "0.85rem" }}>
                    ₺
                    {Number(
                      notification.product.specialPrice ||
                        notification.product.discountedPrice ||
                        notification.product.price ||
                        0,
                    ).toFixed(2)}
                  </span>
                  {(notification.product.specialPrice ||
                    notification.product.discountedPrice) &&
                    notification.product.price && (
                      <span className="text-muted text-decoration-line-through ms-2" style={{ fontSize: "0.7rem" }}>
                        ₺{Number(notification.product.price).toFixed(2)}
                      </span>
                    )}
                </div>
              </div>
            </div>

            {notification.userType === "guest" &&
              guestFirstOrderShippingMessage.trim() && (
                <div
                  className="alert border-0 p-2 mt-2 mb-0"
                  style={{
                    borderRadius: "8px",
                    background: "#fff3cd",
                    fontSize: "0.7rem",
                    textAlign: "center",
                  }}
                >
                  {guestFirstOrderShippingMessage}
                </div>
              )}

            <button
              className="btn w-100 text-white fw-bold mt-2"
              onClick={() => {
                onDismiss();
                navigate("/cart");
              }}
              style={{
                borderRadius: "10px",
                fontSize: "0.8rem",
                background: "linear-gradient(135deg, #10b981, #059669)",
                border: "none",
                padding: "8px 12px",
              }}
            >
              <i className="fas fa-shopping-cart me-1"></i>
              Sepete Git
            </button>
          </div>
        )}

        {isError && (
          <div className="toast-body">
            <p className="mb-0 text-danger fw-bold">{notification.message}</p>
          </div>
        )}
      </div>

      <style>{`
        @keyframes slideInDown {
          from { opacity: 0; transform: translateY(-30px); }
          to { opacity: 1; transform: translateY(0); }
        }
      `}</style>
    </div>
  );
}
