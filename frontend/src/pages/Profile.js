import React, { useState, useEffect } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import profileService from "../services/profileService";

export default function Profile() {
  const { user, updateUser } = useAuth();
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

  useEffect(() => {
    const loadProfile = async () => {
      try {
        const response = await profileService.getProfile();
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
      const response = await profileService.updateProfile({
        firstName: form.firstName,
        lastName: form.lastName,
        phoneNumber: form.phone,
      });
      if (response.success) {
        setMessage("Profil başarıyla güncellendi!");
        updateUser({
          firstName: form.firstName,
          lastName: form.lastName,
          phone: form.phone,
          name: `${form.firstName} ${form.lastName}`.trim(),
        });
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
      const response = await profileService.changePassword({
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
    return <Navigate to="/account" replace />;
  }

  const isPasswordless =
    user.passwordless ||
    user.loginProvider === "google" ||
    user.loginProvider === "facebook";

  return (
    <div className="container my-4 my-md-5 profile-settings-page">
      <div className="row">
        <div className="col-12 mb-4">
          <button
            type="button"
            className="btn btn-link text-decoration-none ps-0 mb-2 d-md-none"
            onClick={() => navigate("/account")}
          >
            <i className="fas fa-arrow-left me-2" />
            Hesabım
          </button>
          <div className="d-flex align-items-center justify-content-between gap-3 flex-wrap">
            <h2 className="fw-bold mb-0">
              <i className="fas fa-user-circle me-2 text-warning" />
              Profil ve Şifre
            </h2>
            <button
              type="button"
              className="btn btn-outline-secondary d-none d-md-inline-flex"
              onClick={() => navigate("/account")}
              style={{ borderRadius: "12px", fontWeight: "600" }}
            >
              <i className="fas fa-arrow-left me-2" />
              Hesabıma Dön
            </button>
          </div>
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
                <i className="fas fa-user text-primary me-2" />
                Profil Bilgileri
              </h5>
            </div>
            <div className="card-body px-4 pb-4">
              <form onSubmit={handleProfileUpdate}>
                <div className="mb-3">
                  <label className="form-label fw-semibold">
                    <i className="fas fa-user me-1" />
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
                    <i className="fas fa-user me-1" />
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
                    <i className="fas fa-envelope me-1" />
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
                    <i className="fas fa-info-circle me-1" />
                    E-posta adresi değiştirilemez
                  </small>
                </div>
                <div className="mb-4">
                  <label className="form-label fw-semibold">
                    <i className="fas fa-phone me-1" />
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
                  <i className="fas fa-check-circle me-2" />
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
                <i className="fas fa-shield-alt text-warning me-2" />
                Şifre Değiştir
              </h5>
            </div>
            <div className="card-body px-4 pb-4">
              {isPasswordless ? (
                <div className="alert alert-info mb-0">
                  <i className="fas fa-info-circle me-2" />
                  {user.loginProvider === "google" ? "Google" : "Sosyal"} ile
                  giriş yaptınız. Şifre belirlemek için çıkış yapıp giriş
                  ekranından &quot;Şifremi Unuttum&quot; akışını
                  kullanabilirsiniz.
                </div>
              ) : (
              <form onSubmit={handlePasswordChange}>
                <div className="mb-3">
                  <label className="form-label fw-semibold">
                    <i className="fas fa-key me-1" />
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
                    <i className="fas fa-key me-1" />
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
                    <i className="fas fa-key me-1" />
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
                  <i className="fas fa-shield-alt me-2" />
                  Şifreyi Değiştir
                </button>
              </form>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
