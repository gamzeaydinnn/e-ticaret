// src/components/LoginRequiredModal.js
import React from "react";

const LoginRequiredModal = ({
  show,
  onHide,
  action,
  onGuestContinue,
  onLogin,
}) => {
  if (!show) return null;

  const getActionText = () => {
    switch (action) {
      case "cart":
        return "sepete eklemek";
      case "favorite":
        return "favorilere eklemek";
      default:
        return "bu işlemi yapmak";
    }
  };

  const getIcon = () => {
    switch (action) {
      case "cart":
        return "fas fa-shopping-cart";
      case "favorite":
        return "fas fa-heart";
      default:
        return "fas fa-user";
    }
  };

  return (
    <div
      className="modal fade show d-block"
      style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
      onClick={onHide}
    >
      <div
        className="modal-dialog modal-dialog-centered"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-content border-0 shadow-lg">
          <div className="modal-header border-0 text-center pb-0">
            <div className="w-100">
              <div className="mb-3">
                <div
                  className="mx-auto rounded-circle d-flex align-items-center justify-content-center"
                  style={{
                    width: "80px",
                    height: "80px",
                    background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                  }}
                >
                  <i
                    className={`${getIcon()} text-white`}
                    style={{ fontSize: "2rem" }}
                  ></i>
                </div>
              </div>
              <h5 className="modal-title fw-bold text-dark">Hesap Gerekli</h5>
            </div>
            <button
              type="button"
              className="btn-close"
              onClick={onHide}
            ></button>
          </div>

          <div className="modal-body text-center px-4">
            <p className="text-muted mb-4">
              Ürünü <strong>{getActionText()}</strong> için hesap oluşturabilir
              veya misafir olarak devam edebilirsiniz.
            </p>

            <div className="d-grid gap-2">
              <button
                className="btn btn-lg text-white fw-bold"
                style={{
                  background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                  border: "none",
                }}
                onClick={onLogin}
              >
                <i className="fas fa-user-plus me-2"></i>
                Hesap Oluştur / Giriş Yap
              </button>

              <button
                className="btn btn-outline-secondary btn-lg"
                onClick={onGuestContinue}
              >
                <i className="fas fa-user-secret me-2"></i>
                Misafir Olarak Devam Et
              </button>
            </div>

            <div className="mt-3">
              <small className="text-muted">
                <i className="fas fa-info-circle me-1"></i>
                Hesap oluşturarak siparişlerinizi takip edebilir ve daha hızlı
                alışveriş yapabilirsiniz.
              </small>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginRequiredModal;
