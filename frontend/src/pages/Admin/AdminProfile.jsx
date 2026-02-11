// src/pages/Admin/AdminProfile.jsx
// =============================================================================
// Admin Profil Yönetimi - Backend Entegre
// =============================================================================
// Admin kullanıcısının kendi profil bilgilerini görüntüleme ve güncelleme sayfası.
// Backend: AuthController.GetCurrentUser ve AccountController.UpdateProfile
// =============================================================================

import React, { useState, useEffect } from "react";
import { useAuth } from "../../contexts/AuthContext";
import AdminService from "../../services/adminService";
import "./AdminProfile.css";

const AdminProfile = () => {
  const { user: currentUser, setUser } = useAuth();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);

  // Profil bilgileri state
  const [profileData, setProfileData] = useState({
    firstName: "",
    lastName: "",
    email: "",
    phoneNumber: "",
    address: "",
    city: "",
  });

  // Şifre değiştirme state
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  });
  const [passwordError, setPasswordError] = useState(null);
  const [passwordSuccess, setPasswordSuccess] = useState(null);
  const [changingPassword, setChangingPassword] = useState(false);

  useEffect(() => {
    loadProfile();
  }, []);

  /**
   * Backend'den kullanıcı profil bilgilerini yükle
   */
  const loadProfile = async () => {
    try {
      setLoading(true);
      setError(null);

      // Önce context'teki user'dan doldur
      if (currentUser) {
        setProfileData({
          firstName: currentUser.firstName || "",
          lastName: currentUser.lastName || "",
          email: currentUser.email || "",
          phoneNumber: currentUser.phoneNumber || "",
          address: currentUser.address || "",
          city: currentUser.city || "",
        });
      }

      // Sonra backend'den güncel bilgileri çek
      const response = await AdminService.getCurrentUser();
      const userData = response?.data || response;

      if (userData) {
        setProfileData({
          firstName: userData.firstName || "",
          lastName: userData.lastName || "",
          email: userData.email || "",
          phoneNumber: userData.phoneNumber || "",
          address: userData.address || "",
          city: userData.city || "",
        });
      }
    } catch (err) {
      console.error("Profil yükleme hatası:", err);
      setError("Profil bilgileri yüklenirken hata oluştu.");
    } finally {
      setLoading(false);
    }
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setProfileData((prev) => ({ ...prev, [name]: value }));
    setError(null);
    setSuccess(null);
  };

  const handlePasswordChange = (e) => {
    const { name, value } = e.target;
    setPasswordForm((prev) => ({ ...prev, [name]: value }));
    setPasswordError(null);
    setPasswordSuccess(null);
  };

  /**
   * Profil bilgilerini güncelle
   */
  const handleProfileUpdate = async (e) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    // Validasyon
    if (!profileData.firstName?.trim() || !profileData.lastName?.trim()) {
      setError("Ad ve Soyad alanları zorunludur.");
      return;
    }

    if (!profileData.email?.trim()) {
      setError("Email alanı zorunludur.");
      return;
    }

    try {
      setSaving(true);

      const payload = {
        firstName: profileData.firstName.trim(),
        lastName: profileData.lastName.trim(),
        email: profileData.email.trim(),
        phoneNumber: profileData.phoneNumber?.trim() || null,
        address: profileData.address?.trim() || null,
        city: profileData.city?.trim() || null,
      };

      await AdminService.updateProfile(payload);

      // Context'teki user'ı güncelle
      if (setUser && currentUser) {
        setUser({
          ...currentUser,
          ...payload,
          fullName: `${payload.firstName} ${payload.lastName}`,
        });
      }

      setSuccess("Profil bilgileriniz başarıyla güncellendi.");

      // 3 saniye sonra success mesajını temizle
      setTimeout(() => setSuccess(null), 3000);
    } catch (err) {
      console.error("Profil güncelleme hatası:", err);
      const errorMsg =
        err?.response?.data?.message ||
        err?.message ||
        "Profil güncellenirken hata oluştu.";
      setError(errorMsg);
    } finally {
      setSaving(false);
    }
  };

  /**
   * Şifre değiştir
   */
  const handlePasswordUpdate = async (e) => {
    e.preventDefault();
    setPasswordError(null);
    setPasswordSuccess(null);

    // Validasyon
    if (
      !passwordForm.currentPassword ||
      !passwordForm.newPassword ||
      !passwordForm.confirmPassword
    ) {
      setPasswordError("Tüm alanları doldurunuz.");
      return;
    }

    if (passwordForm.newPassword.length < 6) {
      setPasswordError("Yeni şifre en az 6 karakter olmalıdır.");
      return;
    }

    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setPasswordError("Yeni şifre ve onay şifresi eşleşmiyor.");
      return;
    }

    try {
      setChangingPassword(true);

      await AdminService.changePassword({
        oldPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword,
        confirmPassword: passwordForm.confirmPassword,
      });

      setPasswordSuccess("Şifreniz başarıyla değiştirildi.");
      setPasswordForm({
        currentPassword: "",
        newPassword: "",
        confirmPassword: "",
      });

      // 3 saniye sonra success mesajını temizle
      setTimeout(() => setPasswordSuccess(null), 3000);
    } catch (err) {
      console.error("Şifre değiştirme hatası:", err);
      const errorMsg =
        err?.response?.data?.message ||
        err?.message ||
        "Şifre değiştirilirken hata oluştu.";
      setPasswordError(errorMsg);
    } finally {
      setChangingPassword(false);
    }
  };

  if (loading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ minHeight: "400px" }}
      >
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  return (
    <div className="admin-profile-container">
      <div className="row">
        <div className="col-12 mb-4">
          <h2 className="admin-profile-title">
            <i className="fas fa-user-circle me-2"></i>
            Profil Yönetimi
          </h2>
          <p className="text-muted">
            Kendi bilgilerinizi görüntüleyin ve güncelleyin
          </p>
        </div>
      </div>

      <div className="row g-4">
        {/* Profil Bilgileri Card */}
        <div className="col-lg-6">
          <div className="card shadow-sm">
            <div className="card-header bg-primary text-white">
              <h5 className="mb-0">
                <i className="fas fa-id-card me-2"></i>
                Profil Bilgileri
              </h5>
            </div>
            <div className="card-body">
              {error && <div className="alert alert-danger">{error}</div>}
              {success && <div className="alert alert-success">{success}</div>}

              <form onSubmit={handleProfileUpdate}>
                <div className="row g-3">
                  <div className="col-md-6">
                    <label className="form-label">Ad *</label>
                    <input
                      type="text"
                      className="form-control"
                      name="firstName"
                      value={profileData.firstName}
                      onChange={handleInputChange}
                      required
                      disabled={saving}
                    />
                  </div>
                  <div className="col-md-6">
                    <label className="form-label">Soyad *</label>
                    <input
                      type="text"
                      className="form-control"
                      name="lastName"
                      value={profileData.lastName}
                      onChange={handleInputChange}
                      required
                      disabled={saving}
                    />
                  </div>
                  <div className="col-12">
                    <label className="form-label">Email *</label>
                    <input
                      type="email"
                      className="form-control"
                      name="email"
                      value={profileData.email}
                      onChange={handleInputChange}
                      required
                      disabled={saving}
                    />
                  </div>
                  <div className="col-12">
                    <label className="form-label">
                      Telefon <small className="text-muted">(opsiyonel)</small>
                    </label>
                    <input
                      type="tel"
                      className="form-control"
                      name="phoneNumber"
                      value={profileData.phoneNumber}
                      onChange={handleInputChange}
                      placeholder="05XX XXX XX XX"
                      disabled={saving}
                    />
                  </div>
                  <div className="col-md-6">
                    <label className="form-label">Adres</label>
                    <input
                      type="text"
                      className="form-control"
                      name="address"
                      value={profileData.address}
                      onChange={handleInputChange}
                      disabled={saving}
                    />
                  </div>
                  <div className="col-md-6">
                    <label className="form-label">Şehir</label>
                    <input
                      type="text"
                      className="form-control"
                      name="city"
                      value={profileData.city}
                      onChange={handleInputChange}
                      disabled={saving}
                    />
                  </div>
                </div>

                <div className="mt-4">
                  <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={saving}
                  >
                    {saving ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2"></span>
                        Kaydediliyor...
                      </>
                    ) : (
                      <>
                        <i className="fas fa-save me-2"></i>
                        Değişiklikleri Kaydet
                      </>
                    )}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>

        {/* Şifre Değiştirme Card */}
        <div className="col-lg-6">
          <div className="card shadow-sm">
            <div className="card-header bg-warning text-dark">
              <h5 className="mb-0">
                <i className="fas fa-lock me-2"></i>
                Şifre Değiştir
              </h5>
            </div>
            <div className="card-body">
              {passwordError && (
                <div className="alert alert-danger">{passwordError}</div>
              )}
              {passwordSuccess && (
                <div className="alert alert-success">{passwordSuccess}</div>
              )}

              <form onSubmit={handlePasswordUpdate}>
                <div className="mb-3">
                  <label className="form-label">Mevcut Şifre *</label>
                  <input
                    type="password"
                    className="form-control"
                    name="currentPassword"
                    value={passwordForm.currentPassword}
                    onChange={handlePasswordChange}
                    required
                    disabled={changingPassword}
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label">Yeni Şifre *</label>
                  <input
                    type="password"
                    className="form-control"
                    name="newPassword"
                    value={passwordForm.newPassword}
                    onChange={handlePasswordChange}
                    required
                    minLength="6"
                    disabled={changingPassword}
                  />
                  <small className="text-muted">En az 6 karakter</small>
                </div>
                <div className="mb-3">
                  <label className="form-label">Yeni Şifre Tekrar *</label>
                  <input
                    type="password"
                    className="form-control"
                    name="confirmPassword"
                    value={passwordForm.confirmPassword}
                    onChange={handlePasswordChange}
                    required
                    disabled={changingPassword}
                  />
                </div>

                <button
                  type="submit"
                  className="btn btn-warning"
                  disabled={changingPassword}
                >
                  {changingPassword ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-2"></span>
                      Değiştiriliyor...
                    </>
                  ) : (
                    <>
                      <i className="fas fa-key me-2"></i>
                      Şifreyi Değiştir
                    </>
                  )}
                </button>
              </form>
            </div>
          </div>

          {/* Kullanıcı Bilgi Kartı */}
          <div className="card shadow-sm mt-4">
            <div className="card-header bg-info text-white">
              <h5 className="mb-0">
                <i className="fas fa-info-circle me-2"></i>
                Hesap Bilgileri
              </h5>
            </div>
            <div className="card-body">
              <div className="mb-2">
                <strong>Rol:</strong>
                <span className="badge bg-primary ms-2">
                  {currentUser?.role || "N/A"}
                </span>
              </div>
              <div className="mb-2">
                <strong>Durum:</strong>
                <span
                  className={`badge ms-2 ${currentUser?.isActive ? "bg-success" : "bg-danger"}`}
                >
                  {currentUser?.isActive ? "Aktif" : "Pasif"}
                </span>
              </div>
              {currentUser?.createdAt && (
                <div className="mb-2">
                  <strong>Kayıt Tarihi:</strong>
                  <span className="ms-2">
                    {new Date(currentUser.createdAt).toLocaleDateString(
                      "tr-TR",
                    )}
                  </span>
                </div>
              )}
              {currentUser?.lastLoginAt && (
                <div className="mb-2">
                  <strong>Son Giriş:</strong>
                  <span className="ms-2">
                    {new Date(currentUser.lastLoginAt).toLocaleString("tr-TR")}
                  </span>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminProfile;
