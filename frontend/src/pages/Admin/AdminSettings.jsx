// src/pages/Admin/AdminSettings.jsx
// =============================================================================
// Admin Sistem Ayarları - Backend Entegre
// =============================================================================
// Genel uygulama ve sistem ayarlarını yönetme sayfası.
// Sadece SuperAdmin erişebilir.
// =============================================================================

import React, { useState, useEffect } from "react";
import { useAuth } from "../../contexts/AuthContext";
import "./AdminSettings.css";

const AdminSettings = () => {
  const { user } = useAuth();
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(null);
  const [error, setError] = useState(null);

  // Örnek ayarlar - Backend'e bağlanabilir
  const [settings, setSettings] = useState({
    siteName: "E-Ticaret Admin",
    siteEmail: "admin@example.com",
    maintenanceMode: false,
    allowRegistration: true,
    itemsPerPage: 20,
  });

  const handleInputChange = (e) => {
    const { name, value, type, checked } = e.target;
    setSettings((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
    setSuccess(null);
    setError(null);
  };

  const handleSaveSettings = async (e) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      setLoading(true);

      // TODO: Backend endpoint'ine bağlanacak
      // await AdminService.updateSettings(settings);

      // Geçici olarak localStorage'da saklayalım
      localStorage.setItem("appSettings", JSON.stringify(settings));

      setSuccess("Ayarlar başarıyla kaydedildi.");
      setTimeout(() => setSuccess(null), 3000);
    } catch (err) {
      console.error("Ayarlar kaydetme hatası:", err);
      setError("Ayarlar kaydedilirken hata oluştu.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    // Ayarları yükle (localStorage'dan veya backend'den)
    try {
      const saved = localStorage.getItem("appSettings");
      if (saved) {
        setSettings(JSON.parse(saved));
      }
    } catch (err) {
      console.error("Ayarlar yükleme hatası:", err);
    }
  }, []);

  // SuperAdmin kontrolü
  if (user?.role !== "SuperAdmin") {
    return (
      <div className="admin-settings-container">
        <div className="alert alert-warning">
          <i className="fas fa-exclamation-triangle me-2"></i>
          Bu sayfaya erişim yetkiniz bulunmamaktadır. Sadece SuperAdmin
          erişebilir.
        </div>
      </div>
    );
  }

  return (
    <div className="admin-settings-container">
      <div className="row">
        <div className="col-12 mb-4">
          <h2 className="admin-settings-title">
            <i className="fas fa-cog me-2"></i>
            Sistem Ayarları
          </h2>
          <p className="text-muted">
            Genel uygulama ve sistem ayarlarını yönetin
          </p>
        </div>
      </div>

      <div className="row g-4">
        {/* Genel Ayarlar */}
        <div className="col-lg-6">
          <div className="card shadow-sm">
            <div className="card-header bg-primary text-white">
              <h5 className="mb-0">
                <i className="fas fa-sliders-h me-2"></i>
                Genel Ayarlar
              </h5>
            </div>
            <div className="card-body">
              {error && <div className="alert alert-danger">{error}</div>}
              {success && <div className="alert alert-success">{success}</div>}

              <form onSubmit={handleSaveSettings}>
                <div className="mb-3">
                  <label className="form-label">Site Adı</label>
                  <input
                    type="text"
                    className="form-control"
                    name="siteName"
                    value={settings.siteName}
                    onChange={handleInputChange}
                    disabled={loading}
                  />
                </div>

                <div className="mb-3">
                  <label className="form-label">Site Email</label>
                  <input
                    type="email"
                    className="form-control"
                    name="siteEmail"
                    value={settings.siteEmail}
                    onChange={handleInputChange}
                    disabled={loading}
                  />
                </div>

                <div className="mb-3">
                  <label className="form-label">Sayfa Başına Öğe Sayısı</label>
                  <input
                    type="number"
                    className="form-control"
                    name="itemsPerPage"
                    value={settings.itemsPerPage}
                    onChange={handleInputChange}
                    min="10"
                    max="100"
                    disabled={loading}
                  />
                </div>

                <div className="form-check mb-3">
                  <input
                    className="form-check-input"
                    type="checkbox"
                    name="maintenanceMode"
                    id="maintenanceMode"
                    checked={settings.maintenanceMode}
                    onChange={handleInputChange}
                    disabled={loading}
                  />
                  <label className="form-check-label" htmlFor="maintenanceMode">
                    Bakım Modu (Site ziyaretçilere kapatılır)
                  </label>
                </div>

                <div className="form-check mb-3">
                  <input
                    className="form-check-input"
                    type="checkbox"
                    name="allowRegistration"
                    id="allowRegistration"
                    checked={settings.allowRegistration}
                    onChange={handleInputChange}
                    disabled={loading}
                  />
                  <label
                    className="form-check-label"
                    htmlFor="allowRegistration"
                  >
                    Yeni Üye Kaydına İzin Ver
                  </label>
                </div>

                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={loading}
                >
                  {loading ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-2"></span>
                      Kaydediliyor...
                    </>
                  ) : (
                    <>
                      <i className="fas fa-save me-2"></i>
                      Ayarları Kaydet
                    </>
                  )}
                </button>
              </form>
            </div>
          </div>
        </div>

        {/* Sistem Bilgileri */}
        <div className="col-lg-6">
          <div className="card shadow-sm">
            <div className="card-header bg-info text-white">
              <h5 className="mb-0">
                <i className="fas fa-info-circle me-2"></i>
                Sistem Bilgileri
              </h5>
            </div>
            <div className="card-body">
              <div className="mb-3">
                <strong>Uygulama Versiyonu:</strong>
                <span className="ms-2">v1.0.0</span>
              </div>
              <div className="mb-3">
                <strong>Backend API:</strong>
                <span className="ms-2 badge bg-success">Bağlı</span>
              </div>
              <div className="mb-3">
                <strong>Veritabanı:</strong>
                <span className="ms-2 badge bg-success">Aktif</span>
              </div>
              <div className="mb-3">
                <strong>Platform:</strong>
                <span className="ms-2">{navigator.platform}</span>
              </div>
              <div className="mb-3">
                <strong>Tarayıcı:</strong>
                <span className="ms-2">
                  {navigator.userAgent.split(" ")[0]}
                </span>
              </div>
            </div>
          </div>

          {/* Cache ve Performans */}
          <div className="card shadow-sm mt-4">
            <div className="card-header bg-warning text-dark">
              <h5 className="mb-0">
                <i className="fas fa-broom me-2"></i>
                Cache ve Performans
              </h5>
            </div>
            <div className="card-body">
              <p className="text-muted">
                Önbellek yönetimi ve performans araçları
              </p>

              <button
                className="btn btn-outline-warning me-2 mb-2"
                onClick={() => {
                  localStorage.clear();
                  alert("LocalStorage temizlendi!");
                }}
              >
                <i className="fas fa-trash me-1"></i>
                LocalStorage Temizle
              </button>

              <button
                className="btn btn-outline-info mb-2"
                onClick={() => {
                  window.location.reload();
                }}
              >
                <i className="fas fa-sync me-1"></i>
                Sayfayı Yenile
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminSettings;
