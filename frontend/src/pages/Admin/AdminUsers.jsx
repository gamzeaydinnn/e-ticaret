import React, { useState, useEffect, useCallback } from "react";
import { AdminService } from "../../services/adminService";
import { useAuth } from "../../contexts/AuthContext";

// ============================================================================
// 5 TEMEL ROL VE AÇIKLAMALARI
// RBAC (Role-Based Access Control) sistemi için tanımlanmış roller
// ============================================================================
const ROLE_DESCRIPTIONS = {
  SuperAdmin: {
    name: "Süper Yönetici",
    description:
      "Sistemin tam yetkili sahibidir. Tüm site ayarlarını değiştirme, diğer yöneticileri atama/silme, ödeme yöntemlerini yapılandırma ve tam veri dışa aktarma yetkisine sahiptir.",
    color: "danger",
    icon: "👑",
  },
  StoreManager: {
    name: "Mağaza Yöneticisi",
    description:
      "Günlük iş akışını yöneten kişidir. Ürün ekleme/güncelleme, stok yönetimi, kampanya ve kupon oluşturma, satış raporlarını görüntüleme yetkilerine sahiptir. Sistem ayarlarına erişemez.",
    color: "warning",
    icon: "🏪",
  },
  CustomerSupport: {
    name: "Müşteri Hizmetleri",
    description:
      "Müşteri memnuniyetini sağlamak ve sipariş sorunlarını çözmekle görevlidir. Sipariş durumlarını güncelleme, iade süreçlerini yönetme, müşteri yorumlarını onaylama yetkilerine sahiptir.",
    color: "info",
    icon: "🎧",
  },
  Logistics: {
    name: "Lojistik Görevlisi",
    description:
      "Depo ve kargo operasyonlarından sorumludur. Sadece gönderilmeyi bekleyen sipariş listesini görme ve kargo takip numarası girme yetkisine sahiptir. Müşteri bilgilerine erişemez.",
    color: "secondary",
    icon: "🚚",
  },
  Admin: {
    name: "Admin (Eski)",
    description:
      "[Deprecated] Eski uyumluluk için korunmuş rol. Yeni kullanıcılar için StoreManager tercih edilmeli.",
    color: "dark",
    icon: "⚙️",
  },
  User: {
    name: "Müşteri",
    description:
      "Sitenin son kullanıcısıdır. Ürün satın alma, kendi profilini düzenleme, sipariş geçmişini görüntüleme ve favori listesi oluşturma yetkilerine sahiptir.",
    color: "light",
    icon: "👤",
  },
  Customer: {
    name: "Müşteri",
    description:
      "Sitenin son kullanıcısıdır. Ürün satın alma, kendi profilini düzenleme, sipariş geçmişini görüntüleme yetkilerine sahiptir.",
    color: "light",
    icon: "👤",
  },
};

// Rol seçenekleri - Admin panelinden atanabilecek roller
const ASSIGNABLE_ROLES = [
  { value: "SuperAdmin", label: "Süper Yönetici", requiresSuperAdmin: true },
  {
    value: "StoreManager",
    label: "Mağaza Yöneticisi",
    requiresSuperAdmin: false,
  },
  {
    value: "CustomerSupport",
    label: "Müşteri Hizmetleri",
    requiresSuperAdmin: false,
  },
  {
    value: "Logistics",
    label: "Lojistik Görevlisi",
    requiresSuperAdmin: false,
  },
  { value: "User", label: "Müşteri", requiresSuperAdmin: false },
];

const AdminUsers = () => {
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedUser, setSelectedUser] = useState(null);
  const [selectedRole, setSelectedRole] = useState("User");
  const [saving, setSaving] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState("");
  const initialCreateForm = {
    firstName: "",
    lastName: "",
    email: "",
    password: "",
    address: "",
    city: "",
    role: "User",
  };
  const [createForm, setCreateForm] = useState(initialCreateForm);

  const isAdminLike =
    currentUser?.role === "Admin" || currentUser?.role === "SuperAdmin";

  const loadUsers = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await AdminService.getUsers();
      const payload = response?.data || response;
      const list = Array.isArray(payload?.data)
        ? payload.data
        : Array.isArray(payload)
        ? payload
        : [];
      setUsers(list);
    } catch (err) {
      console.error("Kullanıcılar yükleme hatası:", err);
      const status = err?.status || err?.response?.status;
      if (status === 401 || status === 403) {
        setError("Bu sayfayı görüntülemek için admin girişi yapmalısınız.");
      } else {
        setError("Kullanıcılar yüklenirken hata oluştu");
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  const canEditUserRole = (u) => {
    if (!isAdminLike) return false;
    if (u?.role === "SuperAdmin" && currentUser?.role !== "SuperAdmin") {
      return false;
    }
    return true;
  };

  const openRoleModal = (u) => {
    setSelectedUser(u);
    setSelectedRole(u?.role || "User");
  };

  const closeRoleModal = () => {
    setSelectedUser(null);
    setSelectedRole("User");
  };

  const openCreateModal = () => {
    if (!isAdminLike) return;
    setCreateForm({
      ...initialCreateForm,
      role: "User",
    });
    setCreateError("");
    setShowCreateModal(true);
  };

  const closeCreateModal = () => {
    setShowCreateModal(false);
    setCreateForm(initialCreateForm);
    setCreateError("");
  };

  const handleCreateInputChange = (e) => {
    const { name, value } = e.target;
    setCreateForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleCreateSubmit = async (e) => {
    e.preventDefault();
    if (!isAdminLike) return;
    if (
      !createForm.firstName.trim() ||
      !createForm.lastName.trim() ||
      !createForm.email.trim() ||
      !createForm.password.trim()
    ) {
      setCreateError("Ad, Soyad, Email ve Şifre alanları zorunludur.");
      return;
    }

    const desiredRole = createForm.role || "User";
    if (desiredRole === "SuperAdmin" && currentUser?.role !== "SuperAdmin") {
      setCreateError(
        "SuperAdmin rolü atamak için SuperAdmin yetkisine sahip olmalısınız."
      );
      return;
    }

    try {
      setCreating(true);
      setCreateError("");
      const payload = {
        firstName: createForm.firstName.trim(),
        lastName: createForm.lastName.trim(),
        email: createForm.email.trim(),
        password: createForm.password,
        address: createForm.address?.trim() || null,
        city: createForm.city?.trim() || null,
        role: desiredRole,
      };
      await AdminService.createUser(payload);
      await loadUsers();
      closeCreateModal();
    } catch (err) {
      console.error("Kullanıcı oluşturma hatası:", err);
      const status = err?.status || err?.response?.status;
      if (status === 403 && desiredRole === "SuperAdmin") {
        setCreateError(
          "SuperAdmin rolü atamak için SuperAdmin yetkisine sahip olmalısınız."
        );
      } else if (status === 401 || status === 403) {
        setCreateError(
          "Bu işlemi gerçekleştirmek için yetkiniz yok. Lütfen tekrar giriş yapın."
        );
      } else {
        setCreateError(
          "Kullanıcı eklenirken bir hata oluştu. Lütfen tekrar deneyin."
        );
      }
    } finally {
      setCreating(false);
    }
  };

  const handleSaveRole = async () => {
    if (!selectedUser) return;
    try {
      setSaving(true);
      await AdminService.updateUserRole(selectedUser.id, selectedRole);
      setUsers((prev) =>
        prev.map((u) =>
          u.id === selectedUser.id ? { ...u, role: selectedRole } : u
        )
      );
      closeRoleModal();
    } catch (err) {
      console.error("Rol güncelleme hatası:", err);
      const status = err?.status || err?.response?.status;
      if (status === 403 && selectedRole === "SuperAdmin") {
        alert(
          "SuperAdmin rolü atamak için SuperAdmin yetkisine sahip olmalısınız."
        );
      } else if (status === 401 || status === 403) {
        alert(
          "Bu işlemi gerçekleştirmek için yetkiniz yok. Lütfen tekrar giriş yapın."
        );
      } else {
        alert("Rol güncellenirken bir hata oluştu. Lütfen tekrar deneyin.");
      }
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ height: "400px" }}
      >
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="alert alert-danger" role="alert">
        {error}
      </div>
    );
  }

  return (
    <div className="admin-users-page">
      <div className="admin-users-header d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-2 mb-4">
        <h2>Kullanıcı Yönetimi</h2>
        {isAdminLike && (
          <div className="admin-users-actions">
            <button className="btn btn-primary" onClick={openCreateModal}>
              Yeni Kullanıcı Ekle
            </button>
          </div>
        )}
      </div>

      <div className="card">
        <div className="card-header">
          <h5 className="card-title mb-0">Kullanıcılar</h5>
        </div>
        <div className="card-body">
          <div className="table-responsive">
            <table className="table table-striped align-middle admin-users-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Ad Soyad</th>
                  <th>Email</th>
                  <th>Rol</th>
                  <th>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                {users.map((u) => {
                  const roleInfo =
                    ROLE_DESCRIPTIONS[u.role] || ROLE_DESCRIPTIONS.User;
                  return (
                    <tr key={u.id}>
                      <td data-label="ID">{u.id}</td>
                      <td data-label="Ad Soyad">
                        {u.fullName ||
                          `${u.firstName ?? ""} ${u.lastName ?? ""}`.trim()}
                      </td>
                      <td data-label="Email">{u.email}</td>
                      <td data-label="Rol">
                        <span
                          className={`badge bg-${roleInfo.color} ${
                            roleInfo.color === "warning" ||
                            roleInfo.color === "light"
                              ? "text-dark"
                              : ""
                          }`}
                          title={roleInfo.description}
                        >
                          {roleInfo.icon} {roleInfo.name}
                        </span>
                      </td>
                      <td data-label="İşlemler">
                        {canEditUserRole(u) && (
                          <button
                            className="btn btn-sm btn-outline-primary admin-users-action-btn"
                            onClick={() => openRoleModal(u)}
                          >
                            Rolü Düzenle
                          </button>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {users.length === 0 && !error && (
            <div className="text-center py-4">
              <i className="fas fa-users fa-3x text-muted mb-3"></i>
              <p className="text-muted">Henüz kullanıcı bulunmuyor.</p>
            </div>
          )}
        </div>
      </div>

      {/* Rol Açıklamaları - 5 Temel Rol */}
      <div className="card mb-4 mt-4">
        <div className="card-header bg-dark text-white">
          <h5 className="card-title mb-0">
            <i className="fas fa-user-tag me-2"></i>
            Rol Açıklamaları
          </h5>
        </div>
        <div className="card-body">
          <div className="row">
            {/* Süper Yönetici */}
            <div className="col-md-6 col-lg-4 mb-3">
              <div className="card h-100 border-danger">
                <div className="card-header bg-danger text-white">
                  <strong>
                    {ROLE_DESCRIPTIONS.SuperAdmin.icon}{" "}
                    {ROLE_DESCRIPTIONS.SuperAdmin.name}
                  </strong>
                </div>
                <div className="card-body">
                  <small>{ROLE_DESCRIPTIONS.SuperAdmin.description}</small>
                </div>
              </div>
            </div>

            {/* Mağaza Yöneticisi */}
            <div className="col-md-6 col-lg-4 mb-3">
              <div className="card h-100 border-warning">
                <div className="card-header bg-warning text-dark">
                  <strong>
                    {ROLE_DESCRIPTIONS.StoreManager.icon}{" "}
                    {ROLE_DESCRIPTIONS.StoreManager.name}
                  </strong>
                </div>
                <div className="card-body">
                  <small>{ROLE_DESCRIPTIONS.StoreManager.description}</small>
                </div>
              </div>
            </div>

            {/* Müşteri Hizmetleri */}
            <div className="col-md-6 col-lg-4 mb-3">
              <div className="card h-100 border-info">
                <div className="card-header bg-info text-white">
                  <strong>
                    {ROLE_DESCRIPTIONS.CustomerSupport.icon}{" "}
                    {ROLE_DESCRIPTIONS.CustomerSupport.name}
                  </strong>
                </div>
                <div className="card-body">
                  <small>{ROLE_DESCRIPTIONS.CustomerSupport.description}</small>
                </div>
              </div>
            </div>

            {/* Lojistik Görevlisi */}
            <div className="col-md-6 col-lg-4 mb-3">
              <div className="card h-100 border-secondary">
                <div className="card-header bg-secondary text-white">
                  <strong>
                    {ROLE_DESCRIPTIONS.Logistics.icon}{" "}
                    {ROLE_DESCRIPTIONS.Logistics.name}
                  </strong>
                </div>
                <div className="card-body">
                  <small>{ROLE_DESCRIPTIONS.Logistics.description}</small>
                </div>
              </div>
            </div>

            {/* Müşteri */}
            <div className="col-md-6 col-lg-4 mb-3">
              <div className="card h-100 border-light">
                <div className="card-header bg-light text-dark">
                  <strong>
                    {ROLE_DESCRIPTIONS.User.icon} {ROLE_DESCRIPTIONS.User.name}
                  </strong>
                </div>
                <div className="card-body">
                  <small>{ROLE_DESCRIPTIONS.User.description}</small>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* İzin Matrisi Tablosu - 5 Rol */}
      <div className="card mb-4">
        <div className="card-header bg-primary text-white">
          <h5 className="card-title mb-0">
            <i className="fas fa-shield-alt me-2"></i>
            Rol Bazlı Erişim Kontrol (RBAC) Matrisi
          </h5>
        </div>
        <div className="card-body">
          <p className="text-muted mb-3">
            Her rol için hangi modüllere erişim izni olduğunu gösteren tablo
            ("En Az Yetki" prensibi uygulanmıştır):
          </p>
          <div className="table-responsive">
            <table className="table table-bordered table-hover permission-matrix">
              <thead className="table-dark">
                <tr>
                  <th>Modül / İşlem</th>
                  <th className="text-center">
                    <span className="badge bg-danger">👑 Süper Yönetici</span>
                  </th>
                  <th className="text-center">
                    <span className="badge bg-warning text-dark">
                      🏪 Mağaza Yön.
                    </span>
                  </th>
                  <th className="text-center">
                    <span className="badge bg-info">🎧 Müşt. Hizm.</span>
                  </th>
                  <th className="text-center">
                    <span className="badge bg-secondary">🚚 Lojistik</span>
                  </th>
                  <th className="text-center">
                    <span className="badge bg-light text-dark">👤 Müşteri</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {/* Kullanıcı Yönetimi */}
                <tr className="table-light">
                  <td colSpan="6">
                    <strong>👥 Kullanıcı Yönetimi</strong>
                  </td>
                </tr>
                <tr>
                  <td className="ps-4">Kullanıcıları görüntüleme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>
                <tr>
                  <td className="ps-4">Kullanıcı rolü değiştirme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>

                {/* Ödeme Ayarları */}
                <tr className="table-light">
                  <td colSpan="6">
                    <strong>💳 Ödeme Ayarları</strong>
                  </td>
                </tr>
                <tr>
                  <td className="ps-4">Ödeme yöntemlerini yapılandırma</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>

                {/* Ürün/Fiyat Yönetimi */}
                <tr className="table-light">
                  <td colSpan="6">
                    <strong>📦 Ürün/Fiyat Düzenleme</strong>
                  </td>
                </tr>
                <tr>
                  <td className="ps-4">Ürünleri görüntüleme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>
                <tr>
                  <td className="ps-4">Ürün ekleme/düzenleme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>
                <tr>
                  <td className="ps-4">Fiyat değiştirme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>
                <tr>
                  <td className="ps-4">Stok yönetimi</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>

                {/* Sipariş Yönetimi */}
                <tr className="table-light">
                  <td colSpan="6">
                    <strong>🛒 Sipariş Durumu Güncelleme</strong>
                  </td>
                </tr>
                <tr>
                  <td className="ps-4">Siparişleri görüntüleme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-warning">⚠️</td>
                  <td className="text-center text-danger">❌</td>
                </tr>
                <tr>
                  <td className="ps-4">Sipariş durumu güncelleme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                </tr>
                <tr>
                  <td className="ps-4">Kargo takip no girme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                </tr>

                {/* İade/İptal Yönetimi */}
                <tr className="table-light">
                  <td colSpan="6">
                    <strong>↩️ İade/İptal Onayı</strong>
                  </td>
                </tr>
                <tr>
                  <td className="ps-4">İade talebi görüntüleme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>
                <tr>
                  <td className="ps-4">İade/İptal onaylama</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>

                {/* Satış Raporları */}
                <tr className="table-light">
                  <td colSpan="6">
                    <strong>📈 Satış Raporları</strong>
                  </td>
                </tr>
                <tr>
                  <td className="ps-4">Satış istatistikleri</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>
                <tr>
                  <td className="ps-4">Finansal raporlar</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-warning">⚠️</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>

                {/* Kampanya/Kupon */}
                <tr className="table-light">
                  <td colSpan="6">
                    <strong>🏷️ Kampanya ve Kupon</strong>
                  </td>
                </tr>
                <tr>
                  <td className="ps-4">Kampanya oluşturma</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>
                <tr>
                  <td className="ps-4">Kupon yönetimi</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>

                {/* Müşteri İletişimi */}
                <tr className="table-light">
                  <td colSpan="6">
                    <strong>💬 Müşteri İletişimi</strong>
                  </td>
                </tr>
                <tr>
                  <td className="ps-4">Müşteri yorumlarını görme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>
                <tr>
                  <td className="ps-4">Yorumları onaylama/silme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>

                {/* Sistem Ayarları */}
                <tr className="table-light">
                  <td colSpan="6">
                    <strong>⚙️ Sistem Ayarları</strong>
                  </td>
                </tr>
                <tr>
                  <td className="ps-4">Site ayarlarını değiştirme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>
                <tr>
                  <td className="ps-4">ERP/Mikro entegrasyonu</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>
                <tr>
                  <td className="ps-4">Veri dışa aktarma</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                  <td className="text-center text-danger">❌</td>
                </tr>

                {/* Müşteri Yetkileri */}
                <tr className="table-light">
                  <td colSpan="6">
                    <strong>🛍️ Müşteri İşlemleri</strong>
                  </td>
                </tr>
                <tr>
                  <td className="ps-4">Alışveriş yapma</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                </tr>
                <tr>
                  <td className="ps-4">Kendi siparişlerini görme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                </tr>
                <tr>
                  <td className="ps-4">Profil düzenleme</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                  <td className="text-center text-success">✅</td>
                </tr>
              </tbody>
            </table>
          </div>
          <div className="mt-3">
            <small className="text-muted">
              <strong>Açıklama:</strong>✅ Tam erişim | ⚠️ Kısıtlı erişim
              (sadece belirli koşullarda) | ❌ Erişim yok
            </small>
          </div>
        </div>
      </div>

      {selectedUser && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">Rolü Düzenle</h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={closeRoleModal}
                ></button>
              </div>
              <div className="modal-body">
                <p className="mb-2">
                  <strong>Kullanıcı:</strong>{" "}
                  {selectedUser.fullName ||
                    `${selectedUser.firstName ?? ""} ${
                      selectedUser.lastName ?? ""
                    }`.trim()}
                </p>
                <p className="mb-3">
                  <strong>Email:</strong> {selectedUser.email}
                </p>
                <div className="mb-3">
                  <label className="form-label">Rol Seçin</label>
                  <select
                    className="form-select"
                    value={selectedRole}
                    onChange={(e) => setSelectedRole(e.target.value)}
                  >
                    {ASSIGNABLE_ROLES.map((role) => (
                      <option
                        key={role.value}
                        value={role.value}
                        disabled={
                          role.requiresSuperAdmin &&
                          currentUser?.role !== "SuperAdmin"
                        }
                      >
                        {ROLE_DESCRIPTIONS[role.value]?.icon} {role.label}
                      </option>
                    ))}
                  </select>
                  {ROLE_DESCRIPTIONS[selectedRole] && (
                    <small className="form-text text-muted d-block mt-2">
                      <strong>{ROLE_DESCRIPTIONS[selectedRole].name}:</strong>{" "}
                      {ROLE_DESCRIPTIONS[selectedRole].description}
                    </small>
                  )}
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={closeRoleModal}
                  disabled={saving}
                >
                  İptal
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={handleSaveRole}
                  disabled={saving}
                >
                  {saving ? "Kaydediliyor..." : "Kaydet"}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {showCreateModal && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog">
            <div className="modal-content">
              <form onSubmit={handleCreateSubmit}>
                <div className="modal-header">
                  <h5 className="modal-title">Yeni Kullanıcı</h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={closeCreateModal}
                    disabled={creating}
                  ></button>
                </div>
                <div className="modal-body">
                  {createError && (
                    <div className="alert alert-danger">{createError}</div>
                  )}
                  <div className="row g-3">
                    <div className="col-md-6">
                      <label className="form-label">Ad</label>
                      <input
                        type="text"
                        className="form-control"
                        name="firstName"
                        value={createForm.firstName}
                        onChange={handleCreateInputChange}
                        required
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">Soyad</label>
                      <input
                        type="text"
                        className="form-control"
                        name="lastName"
                        value={createForm.lastName}
                        onChange={handleCreateInputChange}
                        required
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">Email</label>
                      <input
                        type="email"
                        className="form-control"
                        name="email"
                        value={createForm.email}
                        onChange={handleCreateInputChange}
                        required
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">Şifre</label>
                      <input
                        type="password"
                        className="form-control"
                        name="password"
                        value={createForm.password}
                        onChange={handleCreateInputChange}
                        required
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">Adres</label>
                      <input
                        type="text"
                        className="form-control"
                        name="address"
                        value={createForm.address}
                        onChange={handleCreateInputChange}
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">Şehir</label>
                      <input
                        type="text"
                        className="form-control"
                        name="city"
                        value={createForm.city}
                        onChange={handleCreateInputChange}
                      />
                    </div>
                    <div className="col-12">
                      <label className="form-label">Rol Seçin</label>
                      <select
                        className="form-select"
                        name="role"
                        value={createForm.role}
                        onChange={handleCreateInputChange}
                      >
                        {ASSIGNABLE_ROLES.map((role) => (
                          <option
                            key={role.value}
                            value={role.value}
                            disabled={
                              role.requiresSuperAdmin &&
                              currentUser?.role !== "SuperAdmin"
                            }
                          >
                            {ROLE_DESCRIPTIONS[role.value]?.icon} {role.label}
                          </option>
                        ))}
                      </select>
                      {ROLE_DESCRIPTIONS[createForm.role] && (
                        <small className="form-text text-muted d-block mt-2">
                          <strong>
                            {ROLE_DESCRIPTIONS[createForm.role].name}:
                          </strong>{" "}
                          {ROLE_DESCRIPTIONS[createForm.role].description}
                        </small>
                      )}
                    </div>
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={closeCreateModal}
                    disabled={creating}
                  >
                    İptal
                  </button>
                  <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={creating}
                  >
                    {creating ? "Kaydediliyor..." : "Kullanıcı Ekle"}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminUsers;
