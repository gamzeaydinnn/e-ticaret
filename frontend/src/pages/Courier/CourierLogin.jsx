import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { CourierService } from "../../services/courierService";

export default function CourierLogin() {
  const [formData, setFormData] = useState({ email: "", password: "" });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      const result = await CourierService.login(
        formData.email,
        formData.password
      );
      if (result.success) {
        localStorage.setItem("courierToken", result.courier.token);
        localStorage.setItem("courierData", JSON.stringify(result.courier));
        navigate("/courier/dashboard");
      }
    } catch (err) {
      setError(
        err.message || "Giriş yapılamadı. Lütfen bilgilerinizi kontrol edin."
      );
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-md-6 col-lg-4">
            <div className="card shadow-lg border-0">
              <div className="card-body p-5">
                <div className="text-center mb-4">
                  <div
                    className="mx-auto mb-3 d-flex align-items-center justify-content-center"
                    style={{
                      width: "60px",
                      height: "60px",
                      background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                      borderRadius: "15px",
                    }}
                  >
                    <i className="fas fa-motorcycle text-white fs-4"></i>
                  </div>
                  <h3 className="fw-bold text-dark">Kurye Paneli</h3>
                  <p className="text-muted">Hesabınızla giriş yapın</p>
                </div>

                {error && (
                  <div
                    className="alert alert-danger alert-dismissible fade show"
                    role="alert"
                  >
                    <i className="fas fa-exclamation-triangle me-2"></i>
                    {error}
                  </div>
                )}

                <form onSubmit={handleSubmit}>
                  <div className="mb-3">
                    <label className="form-label fw-semibold">
                      <i className="fas fa-envelope me-2 text-muted"></i>
                      E-posta
                    </label>
                    <input
                      type="email"
                      name="email"
                      className="form-control form-control-lg"
                      value={formData.email}
                      onChange={handleChange}
                      placeholder="ornek@courier.com"
                      required
                      style={{ borderRadius: "10px" }}
                    />
                  </div>

                  <div className="mb-4">
                    <label className="form-label fw-semibold">
                      <i className="fas fa-lock me-2 text-muted"></i>
                      Şifre
                    </label>
                    <input
                      type="password"
                      name="password"
                      className="form-control form-control-lg"
                      value={formData.password}
                      onChange={handleChange}
                      placeholder="••••••••"
                      required
                      style={{ borderRadius: "10px" }}
                    />
                  </div>

                  <button
                    type="submit"
                    disabled={loading}
                    className="btn btn-lg w-100 text-white fw-bold"
                    style={{
                      background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                      border: "none",
                      borderRadius: "10px",
                      boxShadow: "0 4px 15px rgba(255, 107, 53, 0.3)",
                    }}
                  >
                    {loading ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2"></span>
                        Giriş yapılıyor...
                      </>
                    ) : (
                      <>
                        <i className="fas fa-sign-in-alt me-2"></i>
                        Giriş Yap
                      </>
                    )}
                  </button>
                </form>

                <div className="text-center mt-4">
                  <small className="text-muted">
                    Demo için: ahmet@courier.com / 123456
                  </small>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
