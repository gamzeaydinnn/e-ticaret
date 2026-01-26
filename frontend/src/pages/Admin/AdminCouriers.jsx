import React, { useState, useEffect } from "react";
import { CourierService } from "../../services/courierService";
import { AdminService } from "../../services/adminService";

export default function AdminCouriers() {
  const [couriers, setCouriers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedCourier, setSelectedCourier] = useState(null);
  const [performance, setPerformance] = useState(null);
  const [loadingPerformance, setLoadingPerformance] = useState(false);

  // Kurye Ekleme/Düzenleme Modal State
  const [showEditModal, setShowEditModal] = useState(false);
  const [editingCourier, setEditingCourier] = useState(null);
  const [courierForm, setCourierForm] = useState({
    name: "",
    phone: "",
    email: "",
    password: "",
    vehicle: "motorcycle",
    plateNumber: "",
  });
  const [formError, setFormError] = useState("");
  const [saving, setSaving] = useState(false);

  // Şifre Sıfırlama Modal State
  const [showPasswordModal, setShowPasswordModal] = useState(false);
  const [passwordCourier, setPasswordCourier] = useState(null);
  const [newPassword, setNewPassword] = useState("");
  const [resettingPassword, setResettingPassword] = useState(false);

  useEffect(() => {
    loadCouriers();
  }, []);

  const loadCouriers = async () => {
    try {
      const data = await CourierService.getAll();
      setCouriers(data);
    } catch (error) {
      console.error("Kurye listesi yüklenemedi:", error);
    } finally {
      setLoading(false);
    }
  };

  const loadCourierPerformance = async (courierId) => {
    setLoadingPerformance(true);
    try {
      const data = await CourierService.getCourierPerformance(courierId);
      setPerformance(data);
    } catch (error) {
      console.error("Performans verileri yüklenemedi:", error);
    } finally {
      setLoadingPerformance(false);
    }
  };

  // ============================================================
  // KURYE EKLEME/DÜZENLEME İŞLEMLERİ
  // ============================================================
  const openAddModal = () => {
    setEditingCourier(null);
    setCourierForm({
      name: "",
      phone: "",
      email: "",
      password: "",
      vehicle: "motorcycle",
      plateNumber: "",
    });
    setFormError("");
    setShowEditModal(true);
  };

  const openEditModal = (courier) => {
    setEditingCourier(courier);
    setCourierForm({
      name: courier.name || "",
      phone: courier.phone || "",
      email: courier.email || "",
      password: "",
      vehicle: courier.vehicle || "motorcycle",
      plateNumber: courier.plateNumber || "",
    });
    setFormError("");
    setShowEditModal(true);
  };

  const handleFormChange = (e) => {
    const { name, value } = e.target;
    setCourierForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSaveCourier = async () => {
    // Validasyon
    if (!courierForm.name.trim()) {
      setFormError("İsim zorunludur");
      return;
    }
    if (!courierForm.phone.trim()) {
      setFormError("Telefon zorunludur");
      return;
    }
    if (!editingCourier) {
      if (!courierForm.email.trim()) {
        setFormError("E-posta zorunludur");
        return;
      }
      if (!courierForm.password || courierForm.password.length < 6) {
        setFormError("Şifre en az 6 karakter olmalıdır");
        return;
      }
    }

    setSaving(true);
    setFormError("");

    try {
      if (editingCourier) {
        // Güncelleme
        const updatePayload = {
          name: courierForm.name,
          phone: courierForm.phone,
          email: courierForm.email,
          vehicle: courierForm.vehicle,
          plateNumber: courierForm.plateNumber,
        };
        await CourierService.updateCourier(editingCourier.id, updatePayload);
        console.log("✅ Kurye güncellendi:", editingCourier.id);
      } else {
        // Yeni ekleme
        await CourierService.createCourier(courierForm);
        console.log("✅ Yeni kurye eklendi");
      }

      // Listeyi yenile ve modalı kapat
      await loadCouriers();
      setShowEditModal(false);
    } catch (error) {
      console.error("Kurye kaydetme hatası:", error);
      // Backend'den gelen hata mesajını göster
      const errorMsg =
        error?.raw?.response?.data?.message ||
        error?.message ||
        "Kurye kaydedilemedi";
      setFormError(errorMsg);
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteCourier = async (courier) => {
    if (
      !window.confirm(
        `${courier.name} isimli kuryeyi silmek istediğinize emin misiniz?`,
      )
    ) {
      return;
    }

    try {
      await CourierService.deleteCourier(courier.id);
      console.log("✅ Kurye silindi:", courier.id);
      await loadCouriers();
    } catch (error) {
      console.error("Kurye silme hatası:", error);
      alert("Kurye silinemedi: " + (error.message || "Bilinmeyen hata"));
    }
  };

  // ============================================================
  // ŞİFRE SIFIRLAMA İŞLEMLERİ
  // ============================================================
  const openPasswordModal = (courier) => {
    setPasswordCourier(courier);
    setNewPassword("");
    setShowPasswordModal(true);
  };

  const handleResetPassword = async () => {
    if (!newPassword || newPassword.length < 6) {
      alert("Şifre en az 6 karakter olmalıdır");
      return;
    }

    setResettingPassword(true);
    try {
      await CourierService.resetPassword(passwordCourier.id, newPassword);
      console.log("✅ Şifre sıfırlandı:", passwordCourier.id);
      alert("Şifre başarıyla sıfırlandı");
      setShowPasswordModal(false);
    } catch (error) {
      console.error("Şifre sıfırlama hatası:", error);
      alert("Şifre sıfırlanamadı: " + (error.message || "Bilinmeyen hata"));
    } finally {
      setResettingPassword(false);
    }
  };

  const getStatusColor = (status) => {
    const colorMap = {
      active: "success",
      busy: "warning",
      offline: "secondary",
      break: "info",
    };
    return colorMap[status] || "secondary";
  };

  const getStatusText = (status) => {
    const statusMap = {
      active: "Aktif",
      busy: "Meşgul",
      offline: "Çevrimdışı",
      break: "Mola",
    };
    return statusMap[status] || status;
  };

  if (loading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ minHeight: "60vh" }}
      >
        <div className="spinner-border text-primary"></div>
      </div>
    );
  }

  return (
    <div style={{ overflow: "hidden", maxWidth: "100%" }}>
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-3 gap-2 px-1">
        <div>
          <h5 className="fw-bold text-dark mb-0" style={{ fontSize: "1rem" }}>
            <i
              className="fas fa-motorcycle me-2"
              style={{ color: "#f97316" }}
            ></i>
            Kurye Yönetimi
          </h5>
          <p
            className="text-muted mb-0 d-none d-sm-block"
            style={{ fontSize: "0.75rem" }}
          >
            Durum ve performans takibi
          </p>
        </div>
        <div className="d-flex gap-1">
          <button
            onClick={openAddModal}
            className="btn btn-outline-primary btn-sm px-2 py-1"
            style={{ fontSize: "0.7rem" }}
          >
            <i className="fas fa-plus me-1"></i>Yeni Kurye
          </button>
          <button
            onClick={loadCouriers}
            className="btn btn-outline-secondary btn-sm px-2 py-1"
            style={{ fontSize: "0.7rem" }}
          >
            <i className="fas fa-sync-alt"></i>
          </button>
        </div>
      </div>

      {/* Özet Kartlar - 2x2 mobil grid */}
      <div className="row g-2 mb-3 px-1">
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-success text-white"
            style={{ borderRadius: "8px" }}
          >
            <div className="card-body text-center p-2">
              <h5 className="fw-bold mb-0">
                {couriers.filter((c) => c.status === "active").length}
              </h5>
              <small style={{ fontSize: "0.65rem" }}>Aktif</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-warning text-white"
            style={{ borderRadius: "8px" }}
          >
            <div className="card-body text-center p-2">
              <h5 className="fw-bold mb-0">
                {couriers.filter((c) => c.status === "busy").length}
              </h5>
              <small style={{ fontSize: "0.65rem" }}>Meşgul</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-primary text-white"
            style={{ borderRadius: "8px" }}
          >
            <div className="card-body text-center p-2">
              <h5 className="fw-bold mb-0">
                {couriers.reduce((sum, c) => sum + c.activeOrders, 0)}
              </h5>
              <small style={{ fontSize: "0.65rem" }}>Aktif Sipariş</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div
            className="card border-0 shadow-sm bg-info text-white"
            style={{ borderRadius: "8px" }}
          >
            <div className="card-body text-center p-2">
              <h5 className="fw-bold mb-0">
                {(
                  couriers.reduce((sum, c) => sum + c.rating, 0) /
                    couriers.length || 0
                ).toFixed(1)}
              </h5>
              <small style={{ fontSize: "0.65rem" }}>Ort. Puan</small>
            </div>
          </div>
        </div>
      </div>

      {/* Kurye Listesi */}
      <div
        className="card border-0 shadow-sm mx-1"
        style={{ borderRadius: "10px" }}
      >
        <div className="card-header bg-white border-0 py-2 px-2 px-md-3">
          <h6 className="fw-bold mb-0" style={{ fontSize: "0.85rem" }}>
            <i className="fas fa-users me-2 text-primary"></i>
            Kuryeler ({couriers.length})
          </h6>
        </div>
        <div className="card-body p-0">
          <div className="table-responsive">
            <table
              className="table table-sm mb-0"
              style={{ fontSize: "0.7rem" }}
            >
              <thead className="bg-light">
                <tr>
                  <th className="px-1 py-2">Kurye</th>
                  <th className="px-1 py-2 d-none d-md-table-cell">İletişim</th>
                  <th className="px-1 py-2">Durum</th>
                  <th className="px-1 py-2">Sipariş</th>
                  <th className="px-1 py-2 d-none d-sm-table-cell">Puan</th>
                  <th className="px-1 py-2">İşlem</th>
                </tr>
              </thead>
              <tbody>
                {couriers.map((courier) => (
                  <tr key={courier.id}>
                    <td className="px-1 py-2">
                      <div className="d-flex align-items-center">
                        <div
                          className="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center me-1"
                          style={{
                            width: "24px",
                            height: "24px",
                            minWidth: "24px",
                            fontSize: "0.6rem",
                          }}
                        >
                          <i className="fas fa-user"></i>
                        </div>
                        <div className="overflow-hidden">
                          <div
                            className="fw-semibold text-truncate"
                            style={{ maxWidth: "70px" }}
                          >
                            {courier.name}
                          </div>
                          <small
                            className="text-muted d-none d-sm-block"
                            style={{ fontSize: "0.6rem" }}
                          >
                            {courier.vehicle}
                          </small>
                        </div>
                      </div>
                    </td>
                    <td className="px-1 py-2 d-none d-md-table-cell">
                      <div
                        className="text-muted"
                        style={{ fontSize: "0.65rem" }}
                      >
                        {courier.phone}
                      </div>
                    </td>
                    <td className="px-1 py-2">
                      <span
                        className={`badge bg-${getStatusColor(courier.status)}`}
                        style={{ fontSize: "0.55rem", padding: "0.2em 0.4em" }}
                      >
                        {getStatusText(courier.status).substring(0, 5)}
                      </span>
                    </td>
                    <td className="px-1 py-2">
                      <span
                        className="badge bg-primary"
                        style={{ fontSize: "0.6rem" }}
                      >
                        {courier.activeOrders}
                      </span>
                      <span
                        className="badge bg-success ms-1 d-none d-sm-inline"
                        style={{ fontSize: "0.6rem" }}
                      >
                        {courier.completedToday}
                      </span>
                    </td>
                    <td className="px-1 py-2 d-none d-sm-table-cell">
                      <span style={{ fontSize: "0.7rem" }}>
                        {courier.rating}
                      </span>
                      <i
                        className="fas fa-star text-warning ms-1"
                        style={{ fontSize: "0.6rem" }}
                      ></i>
                    </td>
                    <td className="px-1 py-2">
                      <div className="d-flex gap-1 flex-wrap">
                        {/* Performans Butonu */}
                        <button
                          onClick={() => {
                            setSelectedCourier(courier);
                            loadCourierPerformance(courier.id);
                          }}
                          className="btn btn-outline-primary p-1"
                          style={{ fontSize: "0.6rem", lineHeight: 1 }}
                          title="Performans"
                        >
                          <i className="fas fa-chart-line"></i>
                        </button>
                        {/* Düzenle Butonu */}
                        <button
                          onClick={() => openEditModal(courier)}
                          className="btn btn-outline-secondary p-1"
                          style={{ fontSize: "0.6rem", lineHeight: 1 }}
                          title="Düzenle"
                        >
                          <i className="fas fa-edit"></i>
                        </button>
                        {/* Şifre Sıfırla Butonu */}
                        <button
                          onClick={() => openPasswordModal(courier)}
                          className="btn btn-outline-warning p-1 d-none d-md-inline-block"
                          style={{ fontSize: "0.6rem", lineHeight: 1 }}
                          title="Şifre Sıfırla"
                        >
                          <i className="fas fa-key"></i>
                        </button>
                        {/* Sil Butonu */}
                        <button
                          onClick={() => handleDeleteCourier(courier)}
                          className="btn btn-outline-danger p-1 d-none d-md-inline-block"
                          style={{ fontSize: "0.6rem", lineHeight: 1 }}
                          title="Sil"
                        >
                          <i className="fas fa-trash"></i>
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Kurye Performans Modal */}
      {selectedCourier && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-dialog-centered mx-2">
            <div className="modal-content" style={{ borderRadius: "12px" }}>
              <div className="modal-header py-2 px-3">
                <h6 className="modal-title" style={{ fontSize: "0.9rem" }}>
                  <i className="fas fa-chart-line me-2"></i>
                  {selectedCourier.name} - Performans
                </h6>
                <button
                  onClick={() => {
                    setSelectedCourier(null);
                    setPerformance(null);
                  }}
                  className="btn-close btn-close-sm"
                ></button>
              </div>
              <div
                className="modal-body p-2 p-md-3"
                style={{ maxHeight: "70vh", overflowY: "auto" }}
              >
                {loadingPerformance ? (
                  <div className="text-center py-4">
                    <div className="spinner-border spinner-border-sm text-primary mb-2"></div>
                    <p className="small mb-0">Yükleniyor...</p>
                  </div>
                ) : performance ? (
                  <div>
                    <div className="row g-2 mb-3">
                      <div className="col-4">
                        <div
                          className="card border-0 bg-light"
                          style={{ borderRadius: "8px" }}
                        >
                          <div className="card-body text-center p-2">
                            <h5 className="text-primary mb-0">
                              {performance.deliveries.total}
                            </h5>
                            <small
                              className="text-muted"
                              style={{ fontSize: "0.6rem" }}
                            >
                              Toplam
                            </small>
                          </div>
                        </div>
                      </div>
                      <div className="col-4">
                        <div
                          className="card border-0 bg-light"
                          style={{ borderRadius: "8px" }}
                        >
                          <div className="card-body text-center p-2">
                            <h5 className="text-success mb-0">
                              {performance.deliveries.onTime}
                            </h5>
                            <small
                              className="text-muted"
                              style={{ fontSize: "0.6rem" }}
                            >
                              Zamanında
                            </small>
                          </div>
                        </div>
                      </div>
                      <div className="col-4">
                        <div
                          className="card border-0 bg-light"
                          style={{ borderRadius: "8px" }}
                        >
                          <div className="card-body text-center p-2">
                            <h5 className="text-warning mb-0">
                              {performance.deliveries.delayed}
                            </h5>
                            <small
                              className="text-muted"
                              style={{ fontSize: "0.6rem" }}
                            >
                              Geç
                            </small>
                          </div>
                        </div>
                      </div>
                    </div>

                    <h6 className="fw-bold mb-2" style={{ fontSize: "0.8rem" }}>
                      Bugünkü Aktiviteler
                    </h6>
                    <div>
                      {performance.timeline.map((item, index) => (
                        <div
                          key={index}
                          className="d-flex mb-2 align-items-center"
                        >
                          <div
                            className={`rounded-circle bg-${getStatusColor(
                              item.status,
                            )} text-white d-flex align-items-center justify-content-center`}
                            style={{
                              width: "20px",
                              height: "20px",
                              minWidth: "20px",
                              fontSize: "0.5rem",
                            }}
                          >
                            <i className="fas fa-clock"></i>
                          </div>
                          <div
                            className="flex-grow-1 ms-2 d-flex justify-content-between"
                            style={{ fontSize: "0.7rem" }}
                          >
                            <span>{item.action}</span>
                            <small className="text-muted">{item.time}</small>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-4">
                    <p className="text-muted small mb-0">Veri yüklenemedi.</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ================================================================
          KURYE EKLEME/DÜZENLEME MODAL
          ================================================================ */}
      {showEditModal && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-dialog-centered mx-2">
            <div className="modal-content" style={{ borderRadius: "12px" }}>
              <div className="modal-header py-2 px-3">
                <h6 className="modal-title" style={{ fontSize: "0.9rem" }}>
                  <i
                    className={`fas fa-${editingCourier ? "edit" : "plus"} me-2`}
                  ></i>
                  {editingCourier ? "Kurye Düzenle" : "Yeni Kurye Ekle"}
                </h6>
                <button
                  onClick={() => setShowEditModal(false)}
                  className="btn-close btn-close-sm"
                ></button>
              </div>
              <div className="modal-body p-3">
                {formError && (
                  <div
                    className="alert alert-danger py-2 mb-3"
                    style={{ fontSize: "0.75rem" }}
                  >
                    <i className="fas fa-exclamation-circle me-1"></i>
                    {formError}
                  </div>
                )}

                {/* İsim */}
                <div className="mb-3">
                  <label className="form-label small fw-bold">
                    <i className="fas fa-user me-1"></i>İsim Soyisim *
                  </label>
                  <input
                    type="text"
                    name="name"
                    value={courierForm.name}
                    onChange={handleFormChange}
                    className="form-control form-control-sm"
                    placeholder="Kurye adı soyadı"
                  />
                </div>

                {/* Telefon */}
                <div className="mb-3">
                  <label className="form-label small fw-bold">
                    <i className="fas fa-phone me-1"></i>Telefon *
                  </label>
                  <input
                    type="tel"
                    name="phone"
                    value={courierForm.phone}
                    onChange={handleFormChange}
                    className="form-control form-control-sm"
                    placeholder="05XX XXX XXXX"
                  />
                </div>

                {/* E-posta */}
                <div className="mb-3">
                  <label className="form-label small fw-bold">
                    <i className="fas fa-envelope me-1"></i>E-posta
                  </label>
                  <input
                    type="email"
                    name="email"
                    value={courierForm.email}
                    onChange={handleFormChange}
                    className="form-control form-control-sm"
                    placeholder="kurye@ornek.com"
                  />
                </div>

                {!editingCourier && (
                  <div className="mb-3">
                    <label className="form-label small fw-bold">
                      <i className="fas fa-key me-1"></i>Kurye Şifresi *
                    </label>
                    <input
                      type="password"
                      name="password"
                      value={courierForm.password}
                      onChange={handleFormChange}
                      className="form-control form-control-sm"
                      placeholder="En az 6 karakter"
                    />
                  </div>
                )}

                {/* Araç Tipi */}
                <div className="mb-3">
                  <label className="form-label small fw-bold">
                    <i className="fas fa-motorcycle me-1"></i>Araç Tipi
                  </label>
                  <select
                    name="vehicle"
                    value={courierForm.vehicle}
                    onChange={handleFormChange}
                    className="form-select form-select-sm"
                  >
                    <option value="motorcycle">🏍️ Motosiklet</option>
                    <option value="bicycle">🚴 Bisiklet</option>
                    <option value="car">🚗 Araba</option>
                    <option value="scooter">🛵 Scooter</option>
                    <option value="walk">🚶 Yaya</option>
                  </select>
                </div>

                {/* Plaka */}
                <div className="mb-3">
                  <label className="form-label small fw-bold">
                    <i className="fas fa-id-card me-1"></i>Plaka No
                  </label>
                  <input
                    type="text"
                    name="plateNumber"
                    value={courierForm.plateNumber}
                    onChange={handleFormChange}
                    className="form-control form-control-sm"
                    placeholder="34 ABC 123"
                  />
                </div>
              </div>
              <div className="modal-footer py-2 px-3">
                <button
                  onClick={() => setShowEditModal(false)}
                  className="btn btn-outline-secondary btn-sm"
                  style={{ fontSize: "0.75rem" }}
                >
                  İptal
                </button>
                <button
                  onClick={handleSaveCourier}
                  className="btn btn-primary btn-sm"
                  style={{ fontSize: "0.75rem" }}
                  disabled={saving}
                >
                  {saving ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-1"></span>
                      Kaydediliyor...
                    </>
                  ) : (
                    <>
                      <i className="fas fa-save me-1"></i>
                      {editingCourier ? "Güncelle" : "Kaydet"}
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ================================================================
          ŞİFRE SIFIRLAMA MODAL
          ================================================================ */}
      {showPasswordModal && passwordCourier && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-dialog-centered modal-sm mx-2">
            <div className="modal-content" style={{ borderRadius: "12px" }}>
              <div className="modal-header py-2 px-3">
                <h6 className="modal-title" style={{ fontSize: "0.9rem" }}>
                  <i className="fas fa-key me-2 text-warning"></i>
                  Şifre Sıfırla
                </h6>
                <button
                  onClick={() => setShowPasswordModal(false)}
                  className="btn-close btn-close-sm"
                ></button>
              </div>
              <div className="modal-body p-3">
                <div
                  className="alert alert-info py-2 mb-3"
                  style={{ fontSize: "0.75rem" }}
                >
                  <i className="fas fa-info-circle me-1"></i>
                  <strong>{passwordCourier.name}</strong> için yeni şifre
                  belirleyin.
                </div>

                <div className="mb-3">
                  <label className="form-label small fw-bold">
                    <i className="fas fa-lock me-1"></i>Yeni Şifre
                  </label>
                  <input
                    type="password"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    className="form-control form-control-sm"
                    placeholder="En az 6 karakter"
                    minLength={6}
                  />
                  <small className="text-muted">En az 6 karakter giriniz</small>
                </div>
              </div>
              <div className="modal-footer py-2 px-3">
                <button
                  onClick={() => setShowPasswordModal(false)}
                  className="btn btn-outline-secondary btn-sm"
                  style={{ fontSize: "0.75rem" }}
                >
                  İptal
                </button>
                <button
                  onClick={handleResetPassword}
                  className="btn btn-warning btn-sm"
                  style={{ fontSize: "0.75rem" }}
                  disabled={resettingPassword || newPassword.length < 6}
                >
                  {resettingPassword ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-1"></span>
                      Sıfırlanıyor...
                    </>
                  ) : (
                    <>
                      <i className="fas fa-key me-1"></i>
                      Şifreyi Sıfırla
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
