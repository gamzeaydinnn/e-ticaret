import React, { useState, useEffect, useCallback } from "react";
import { AdminService } from "../../services/adminService";
import { useAuth } from "../../contexts/AuthContext";

const ROLE_DESCRIPTIONS = {
  SuperAdmin:
    "Tüm sistemi yönetir. Diğer adminleri ve rolleri yönetebilir, kritik ayarları değiştirebilir.",
  Admin:
    "Ürün, kategori, kampanya, kupon, sipariş ve kullanıcı yönetimi yapabilir. Sistem ayarlarını değiştiremez.",
  User: "Normal müşteri hesabıdır. Sadece alışveriş ve kendi hesap işlemlerini yapabilir, admin paneline erişemez.",
};

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
                {users.map((u) => (
                  <tr key={u.id}>
                    <td data-label="ID">{u.id}</td>
                    <td data-label="Ad Soyad">
                      {u.fullName ||
                        `${u.firstName ?? ""} ${u.lastName ?? ""}`.trim()}
                    </td>
                    <td data-label="Email">{u.email}</td>
                    <td data-label="Rol">
                      <span
                        className={`badge ${
                          u.role === "SuperAdmin"
                            ? "bg-danger"
                            : u.role === "Admin"
                            ? "bg-warning text-dark"
                            : "bg-secondary"
                        }`}
                      >
                        {u.role === "User" ? "Kullanıcı" : u.role}
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
                ))}
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

      <div className="card mb-4 mt-4">
        <div className="card-body">
          <h5 className="card-title">Rol Açıklamaları</h5>
          <ul className="mb-0">
            <li>
              <strong>SuperAdmin:</strong> {ROLE_DESCRIPTIONS.SuperAdmin}
            </li>
            <li>
              <strong>Admin:</strong> {ROLE_DESCRIPTIONS.Admin}
            </li>
            <li>
              <strong>Kullanıcı:</strong> {ROLE_DESCRIPTIONS.User}
            </li>
          </ul>
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
                  <label className="form-label">Rol</label>
                  <select
                    className="form-select"
                    value={selectedRole}
                    onChange={(e) => setSelectedRole(e.target.value)}
                  >
                    <option
                      value="SuperAdmin"
                      disabled={currentUser?.role !== "SuperAdmin"}
                    >
                      SuperAdmin
                    </option>
                    <option value="Admin">Admin</option>
                    <option value="User">Kullanıcı</option>
                  </select>
                  <small className="form-text text-muted">
                    {ROLE_DESCRIPTIONS[selectedRole] || ""}
                  </small>
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
                      <label className="form-label">Rol</label>
                      <select
                        className="form-select"
                        name="role"
                        value={createForm.role}
                        onChange={handleCreateInputChange}
                      >
                        <option value="User">Kullanıcı</option>
                        <option value="Admin">Admin</option>
                        <option
                          value="SuperAdmin"
                          disabled={currentUser?.role !== "SuperAdmin"}
                        >
                          SuperAdmin
                        </option>
                      </select>
                      <small className="form-text text-muted">
                        {ROLE_DESCRIPTIONS[createForm.role] || ""}
                      </small>
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
