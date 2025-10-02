import React from "react";
import { useCartCount } from "../hooks/useCartCount";
import { Link, useNavigate } from "react-router-dom";

const CartPage = () => {
  const { count: cartCount } = useCartCount();
  const navigate = useNavigate();

  return (
    <div
      style={{
        minHeight: "100vh",
        background:
          "linear-gradient(135deg, #fff3e0 0%, #ffe0b2 50%, #ffcc80 100%)",
        paddingTop: "2rem",
        paddingBottom: "2rem",
      }}
    >
      <div className="container">
        <div className="row">
          <div className="col-md-10 mx-auto">
            <div
              className="card shadow-lg border-0"
              style={{ borderRadius: "20px" }}
            >
              <div
                className="card-header text-white d-flex justify-content-between align-items-center border-0"
                style={{
                  background:
                    "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)",
                  borderTopLeftRadius: "20px",
                  borderTopRightRadius: "20px",
                  padding: "1.5rem 2rem",
                }}
              >
                <h3 className="mb-0 fw-bold">
                  <i className="fas fa-shopping-cart me-3"></i>Sepetim
                </h3>
                <span
                  className="badge fs-6 fw-bold px-3 py-2"
                  style={{
                    backgroundColor: "rgba(255,255,255,0.2)",
                    borderRadius: "50px",
                  }}
                >
                  {cartCount} Ürün
                </span>
              </div>
              <div className="card-body" style={{ padding: "2rem" }}>
                {cartCount > 0 ? (
                  <>
                    <div
                      className="row pb-3 mb-4 fw-bold text-warning"
                      style={{ borderBottom: "2px solid #ffe0b2" }}
                    >
                      <div className="col-md-8">
                        <h6>
                          <i className="fas fa-box me-2"></i>Ürün
                        </h6>
                      </div>
                      <div className="col-md-2 text-center">
                        <h6>
                          <i className="fas fa-sort-numeric-up me-2"></i>Adet
                        </h6>
                      </div>
                      <div className="col-md-2 text-end">
                        <h6>
                          <i className="fas fa-tag me-2"></i>Fiyat
                        </h6>
                      </div>
                    </div>

                    <div className="text-center py-5">
                      <div
                        className="spinner-border mb-3"
                        role="status"
                        style={{
                          color: "#ff8f00",
                          width: "3rem",
                          height: "3rem",
                        }}
                      >
                        <span className="visually-hidden">Loading...</span>
                      </div>
                      <p className="text-muted fw-bold">
                        Sepet detayları yükleniyor...
                      </p>
                    </div>

                    <div className="row mt-4">
                      <div className="col-md-8"></div>
                      <div className="col-md-4">
                        <div
                          className="card border-0 shadow-lg"
                          style={{
                            borderRadius: "20px",
                            backgroundColor: "#fff8f0",
                          }}
                        >
                          <div
                            className="card-body"
                            style={{ padding: "1.5rem" }}
                          >
                            <h6 className="text-warning fw-bold mb-3">
                              <i className="fas fa-calculator me-2"></i>Sipariş
                              Özeti
                            </h6>
                            <hr style={{ borderColor: "#ffe0b2" }} />
                            <div className="d-flex justify-content-between mb-2">
                              <span>Ara Toplam:</span>
                              <span className="fw-bold">₺0.00</span>
                            </div>
                            <div className="d-flex justify-content-between mb-2">
                              <span>Kargo:</span>
                              <span className="text-success fw-bold">
                                Ücretsiz
                              </span>
                            </div>
                            <hr style={{ borderColor: "#ffe0b2" }} />
                            <div className="d-flex justify-content-between fw-bold fs-5 mb-3">
                              <span>Toplam:</span>
                              <span className="text-warning">₺0.00</span>
                            </div>
                            <button
                              className="btn btn-lg w-100 text-white fw-bold shadow-lg border-0"
                              style={{
                                background:
                                  "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)",
                                borderRadius: "50px",
                                padding: "1rem",
                              }}
                              onClick={() => navigate("/payment")}
                            >
                              <i className="fas fa-credit-card me-2"></i>
                              Ödemeye Geç
                            </button>
                          </div>
                        </div>
                      </div>
                    </div>
                  </>
                ) : (
                  <div className="row justify-content-center">
                    <div className="col-md-8">
                      <div className="text-center mb-4">
                        <div
                          className="p-4 rounded-circle mx-auto mb-4 shadow-lg"
                          style={{
                            backgroundColor: "#fff8f0",
                            width: "100px",
                            height: "100px",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                          }}
                        >
                          <i
                            className="fas fa-user-circle text-warning"
                            style={{ fontSize: "2.5rem" }}
                          ></i>
                        </div>
                        <h4 className="text-warning fw-bold mb-2">
                          Giriş Yapın
                        </h4>
                        <p className="text-muted mb-4">
                          Sepetinize ürün eklemek için hesabınıza giriş yapmanız
                          gerekiyor
                        </p>
                      </div>

                      <form className="login-form">
                        <div className="mb-4">
                          <label className="form-label fw-semibold text-muted">
                            <i className="fas fa-envelope me-2"></i>E-posta
                            Adresi
                          </label>
                          <input
                            type="email"
                            className="form-control form-control-lg"
                            placeholder="ornek@email.com"
                            style={{
                              borderRadius: "15px",
                              border: "2px solid #ffe0b2",
                              padding: "12px 20px",
                            }}
                          />
                        </div>

                        <div className="mb-4">
                          <label className="form-label fw-semibold text-muted">
                            <i className="fas fa-lock me-2"></i>Şifre
                          </label>
                          <input
                            type="password"
                            className="form-control form-control-lg"
                            placeholder="••••••••"
                            style={{
                              borderRadius: "15px",
                              border: "2px solid #ffe0b2",
                              padding: "12px 20px",
                            }}
                          />
                        </div>

                        <div className="d-flex justify-content-between align-items-center mb-4">
                          <div className="form-check">
                            <input
                              className="form-check-input"
                              type="checkbox"
                              id="rememberMe"
                            />
                            <label
                              className="form-check-label text-muted"
                              htmlFor="rememberMe"
                            >
                              Beni hatırla
                            </label>
                          </div>
                          <button
                            type="button"
                            className="btn btn-link text-warning text-decoration-none fw-semibold p-0"
                            onClick={() => alert("Şifre sıfırlama özelliği yakında...")}
                          >
                            Şifremi unuttum
                          </button>
                        </div>

                        <button
                          type="button"
                          className="btn btn-lg w-100 text-white fw-bold shadow-lg border-0 mb-3"
                          style={{
                            background:
                              "linear-gradient(135deg, #ff6b35, #ff8c00)",
                            borderRadius: "25px",
                            padding: "15px",
                            fontSize: "1.1rem",
                          }}
                          onClick={() => {
                            // Basit demo login - gerçek uygulamada API çağrısı yapılır
                            alert(
                              "Giriş başarılı! Şimdi ürünleri sepetinize ekleyebilirsiniz."
                            );
                            navigate("/");
                          }}
                        >
                          <i className="fas fa-sign-in-alt me-2"></i>
                          Giriş Yap
                        </button>

                        <div className="text-center">
                          <span className="text-muted">Hesabınız yok mu? </span>
                          <button
                            type="button"
                            className="btn btn-link text-warning text-decoration-none fw-semibold p-0"
                            onClick={() => alert("Kayıt olma özelliği yakında...")}
                          >
                            Kayıt olun
                          </button>
                        </div>
                      </form>

                      <div className="text-center mt-4">
                        <Link
                          to="/"
                          className="btn btn-outline-warning btn-lg border-2 fw-semibold"
                          style={{
                            borderRadius: "25px",
                            padding: "12px 30px",
                          }}
                        >
                          <i className="fas fa-arrow-left me-2"></i>
                          Alışverişe Devam Et
                        </Link>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CartPage;
