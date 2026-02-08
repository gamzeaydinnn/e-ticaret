import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import api from "../services/api";

export default function Profile() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({
    firstName: "",
    lastName: "",
    email: "",
    phone: "",
    address: "",
  });
  const [passwordForm, setPasswordForm] = useState({
    oldPassword: "",
    newPassword: "",
    confirmPassword: "",
  });
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [logoutLoading, setLogoutLoading] = useState(false);

  useEffect(() => {
    const loadProfile = async () => {
      try {
        const response = await api.get("/profile");
        if (response.success && response.data) {
          setForm({
            firstName: response.data.firstName || "",
            lastName: response.data.lastName || "",
            email: response.data.email || "",
            phone: response.data.phoneNumber || "",
            address: "",
          });
        }
      } catch (err) {
        console.error("Profil yüklenemedi:", err);
        if (user) {
          setForm({
            firstName: user.firstName || "",
            lastName: user.lastName || "",
            email: user.email || "",
            phone: user.phone || "",
            address: user.address || "",
          });
        }
      } finally {
        setLoading(false);
      }
    };

    if (user) {
      loadProfile();
      return;
    }

    setLoading(false);
  }, [user]);

  const handleProfileUpdate = async (e) => {
    e.preventDefault();
    setMessage("");
    setError("");

    try {
      const response = await api.put("/profile", {
        firstName: form.firstName,
        lastName: form.lastName,
        phoneNumber: form.phone,
      });
      if (response.success) {
        setMessage("Profil başarıyla güncellendi!");
      }
    } catch (err) {
      setError(err.message || "Profil güncellenemedi");
    }
  };

  const handlePasswordChange = async (e) => {
    e.preventDefault();
    setMessage("");
    setError("");

    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setError("Yeni şifreler eşleşmiyor!");
      return;
    }

    try {
      const response = await api.post("/profile/change-password", {
        oldPassword: passwordForm.oldPassword,
        newPassword: passwordForm.newPassword,
        confirmPassword: passwordForm.confirmPassword,
      });
      if (response.success) {
        setMessage("Şifre başarıyla değiştirildi!");
        setPasswordForm({
          oldPassword: "",
          newPassword: "",
          confirmPassword: "",
        });
      }
    } catch (err) {
      setError(err.message || "Şifre değiştirilemedi");
    }
  };

  const handleLogout = async () => {
    setLogoutLoading(true);
    setMessage("");
    setError("");

    try {
      await logout();
      navigate("/account");
    } catch (err) {
      setError("Çıkış yapılırken bir hata oluştu");
    } finally {
      setLogoutLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8 max-w-4xl text-center">
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="container my-5" style={{ maxWidth: "640px" }}>
        <div className="alert alert-warning">
          Profil bilgilerini görüntülemek için giriş yapmanız gerekiyor.
        </div>
        <button
          type="button"
          className="btn btn-warning"
          onClick={() => navigate("/account")}
        >
          Hesabıma Git
        </button>
      </div>
    );
  }

  return (
    <div className="container my-5">
      <div className="row">
        <div className="col-12 mb-4 d-flex align-items-center justify-content-between gap-3">
          <h2 className="fw-bold">
            <i className="bi bi-person-circle me-2"></i>
            Profilim
          </h2>
          <button
            type="button"
            className="btn btn-outline-danger"
            onClick={handleLogout}
            disabled={logoutLoading}
            style={{ borderRadius: "12px", fontWeight: "600" }}
          >
            <i className="bi bi-box-arrow-right me-2"></i>
            {logoutLoading ? "Çıkış yapılıyor..." : "Çıkış Yap"}
          </button>
        </div>
      </div>

      {message && (
        <div
          className={`alert ${
            message.includes("✅") ? "alert-success" : "alert-danger"
          } alert-dismissible fade show`}
          role="alert"
        >
          {message}
          <button
            type="button"
            className="btn-close"
            onClick={() => setMessage("")}
          ></button>
        </div>
      )}
      {error && (
        <div
          className="alert alert-danger alert-dismissible fade show"
          role="alert"
        >
          {error}
          <button
            type="button"
            className="btn-close"
            onClick={() => setError("")}
          ></button>
        </div>
      )}

      <div className="row g-4">
        {/* Profil Bilgileri */}
        <div className="col-lg-6">
          <div
            className="card shadow-sm border-0"
            style={{ borderRadius: "16px" }}
          >
            <div className="card-header bg-white border-0 pt-4 pb-3">
              <h5 className="mb-0 fw-bold">
                <i className="bi bi-person-fill text-primary me-2"></i>
                Profil Bilgileri
              </h5>
            </div>
            <div className="card-body px-4 pb-4">
              <form onSubmit={handleProfileUpdate}>
                <div className="mb-3">
                  <label className="form-label fw-semibold">
                    <i className="bi bi-person me-1"></i>
                    Ad
                  </label>
                  <input
                    type="text"
                    className="form-control form-control-lg"
                    value={form.firstName}
                    onChange={(e) =>
                      setForm({ ...form, firstName: e.target.value })
                    }
                    placeholder="Adınızı girin"
                    required
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label fw-semibold">
                    <i className="bi bi-person me-1"></i>
                    Soyad
                  </label>
                  <input
                    type="text"
                    className="form-control form-control-lg"
                    value={form.lastName}
                    onChange={(e) =>
                      setForm({ ...form, lastName: e.target.value })
                    }
                    placeholder="Soyadınızı girin"
                    required
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label fw-semibold">
                    <i className="bi bi-envelope me-1"></i>
                    E-posta
                  </label>
                  <input
                    type="email"
                    className="form-control form-control-lg"
                    value={form.email}
                    disabled
                    style={{ backgroundColor: "#f8f9fa" }}
                  />
                  <small className="text-muted">
                    <i className="bi bi-info-circle me-1"></i>
                    E-posta adresi değiştirilemez
                  </small>
                </div>
                <div className="mb-4">
                  <label className="form-label fw-semibold">
                    <i className="bi bi-telephone me-1"></i>
                    Telefon
                  </label>
                  <input
                    type="tel"
                    className="form-control form-control-lg"
                    value={form.phone}
                    onChange={(e) =>
                      setForm({ ...form, phone: e.target.value })
                    }
                    placeholder="Telefon numaranız"
                  />
                </div>
                <button
                  type="submit"
                  className="btn btn-primary btn-lg w-100"
                  style={{
                    backgroundColor: "#ff8c00",
                    border: "none",
                    borderRadius: "12px",
                    fontWeight: "600",
                  }}
                >
                  <i className="bi bi-check-circle me-2"></i>
                  Profili Güncelle
                </button>
              </form>
            </div>
          </div>
        </div>

        {/* Şifre Değiştirme */}
        <div className="col-lg-6">
          <div
            className="card shadow-sm border-0"
            style={{ borderRadius: "16px" }}
          >
            <div className="card-header bg-white border-0 pt-4 pb-3">
              <h5 className="mb-0 fw-bold">
                <i className="bi bi-shield-lock text-warning me-2"></i>
                Şifre Değiştir
              </h5>
            </div>
            <div className="card-body px-4 pb-4">
              <form onSubmit={handlePasswordChange}>
                <div className="mb-3">
                  <label className="form-label fw-semibold">
                    <i className="bi bi-key me-1"></i>
                    Mevcut Şifre
                  </label>
                  <input
                    type="password"
                    className="form-control form-control-lg"
                    value={passwordForm.oldPassword}
                    onChange={(e) =>
                      setPasswordForm({
                        ...passwordForm,
                        oldPassword: e.target.value,
                      })
                    }
                    placeholder="Mevcut şifreniz"
                    required
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label fw-semibold">
                    <i className="bi bi-key-fill me-1"></i>
                    Yeni Şifre
                  </label>
                  <input
                    type="password"
                    className="form-control form-control-lg"
                    value={passwordForm.newPassword}
                    onChange={(e) =>
                      setPasswordForm({
                        ...passwordForm,
                        newPassword: e.target.value,
                      })
                    }
                    placeholder="Yeni şifreniz"
                    required
                  />
                </div>
                <div className="mb-4">
                  <label className="form-label fw-semibold">
                    <i className="bi bi-key-fill me-1"></i>
                    Yeni Şifre (Tekrar)
                  </label>
                  <input
                    type="password"
                    className="form-control form-control-lg"
                    value={passwordForm.confirmPassword}
                    onChange={(e) =>
                      setPasswordForm({
                        ...passwordForm,
                        confirmPassword: e.target.value,
                      })
                    }
                    placeholder="Yeni şifrenizi tekrar girin"
                    required
                  />
                </div>
                <button
                  type="submit"
                  className="btn btn-warning btn-lg w-100"
                  style={{
                    borderRadius: "12px",
                    fontWeight: "600",
                    color: "#fff",
                  }}
                >
                  <i className="bi bi-shield-check me-2"></i>
                  Şifreyi Değiştir
                </button>
              </form>
            </div>
          </div>
        </div>
      </div>

      {/* Hızlı Erişim Kartları */}
      <div className="row g-4 mt-3">
        <div className="col-md-4">
          <div
            className="card shadow-sm border-0 h-100"
            style={{ borderRadius: "16px", cursor: "pointer" }}
            onClick={() => (window.location.href = "/addresses")}
          >
            <div className="card-body text-center p-4">
              <i
                className="bi bi-geo-alt-fill text-danger"
                style={{ fontSize: "3rem" }}
              ></i>
              <h5 className="mt-3 mb-2 fw-bold">Adreslerim</h5>
              <p className="text-muted mb-0">Teslimat adreslerini yönet</p>
            </div>
          </div>
        </div>
        <div className="col-md-4">
          <div
            className="card shadow-sm border-0 h-100"
            style={{ borderRadius: "16px", cursor: "pointer" }}
            onClick={() => (window.location.href = "/orders")}
          >
            <div className="card-body text-center p-4">
              <i
                className="bi bi-box-seam text-success"
                style={{ fontSize: "3rem" }}
              ></i>
              <h5 className="mt-3 mb-2 fw-bold">Siparişlerim</h5>
              <p className="text-muted mb-0">Sipariş geçmişini görüntüle</p>
            </div>
          </div>
        </div>
        <div className="col-md-4">
          <div
            className="card shadow-sm border-0 h-100"
            style={{ borderRadius: "16px", cursor: "pointer" }}
            onClick={() => (window.location.href = "/favorites")}
          >
            <div className="card-body text-center p-4">
              <i
                className="bi bi-heart-fill text-danger"
                style={{ fontSize: "3rem" }}
              ></i>
              <h5 className="mt-3 mb-2 fw-bold">Favorilerim</h5>
              <p className="text-muted mb-0">Beğendiğin ürünler</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
