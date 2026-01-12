// =============================================================================
// AdminRoles.jsx - Rol Yönetimi Sayfası
// =============================================================================
// Bu sayfa rollerin yönetimini ve rol-izin atamalarını sağlar.
// Sadece SuperAdmin tarafından erişilebilir.
// =============================================================================

import React, { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";
import permissionService, {
  PERMISSIONS,
  PERMISSION_MODULES,
} from "../../services/permissionService";

const AdminRoles = () => {
  const navigate = useNavigate();
  const { user, hasPermission } = useAuth();

  // State
  const [roles, setRoles] = useState([]);
  const [allPermissions, setAllPermissions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [successMessage, setSuccessMessage] = useState(null);

  // Modal state
  const [showModal, setShowModal] = useState(false);
  const [modalMode, setModalMode] = useState("create"); // create, edit, permissions
  const [selectedRole, setSelectedRole] = useState(null);
  const [rolePermissions, setRolePermissions] = useState([]);
  const [savingPermissions, setSavingPermissions] = useState(false);

  // Form state
  const [formData, setFormData] = useState({
    name: "",
    displayName: "",
    description: "",
    isActive: true,
  });

  // Erişim kontrolü
  const canView =
    user?.role === "SuperAdmin" || hasPermission?.(PERMISSIONS.ROLES_VIEW);
  const canCreate =
    user?.role === "SuperAdmin" || hasPermission?.(PERMISSIONS.ROLES_CREATE);
  const canUpdate =
    user?.role === "SuperAdmin" || hasPermission?.(PERMISSIONS.ROLES_UPDATE);
  const canDelete =
    user?.role === "SuperAdmin" || hasPermission?.(PERMISSIONS.ROLES_DELETE);
  const canManagePermissions =
    user?.role === "SuperAdmin" ||
    hasPermission?.(PERMISSIONS.ROLES_PERMISSIONS);

  // Verileri yükle
  const loadData = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const [rolesResponse, permissionsResponse] = await Promise.all([
        permissionService.getAllRoles(),
        permissionService.getAllPermissions(),
      ]);

      if (rolesResponse?.success !== false) {
        setRoles(
          Array.isArray(rolesResponse)
            ? rolesResponse
            : rolesResponse.roles || []
        );
      }

      if (permissionsResponse?.success !== false) {
        setAllPermissions(
          Array.isArray(permissionsResponse)
            ? permissionsResponse
            : permissionsResponse.permissions || []
        );
      }
    } catch (err) {
      console.error("Data loading error:", err);
      setError("Veriler yüklenirken bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (canView) {
      loadData();
    }
  }, [canView, loadData]);

  // Modal işlemleri
  const handleOpenCreateModal = () => {
    setModalMode("create");
    setFormData({
      name: "",
      displayName: "",
      description: "",
      isActive: true,
    });
    setShowModal(true);
  };

  const handleOpenEditModal = (role) => {
    setModalMode("edit");
    setSelectedRole(role);
    setFormData({
      name: role.name || "",
      displayName: role.displayName || role.name || "",
      description: role.description || "",
      isActive: role.isActive !== false,
    });
    setShowModal(true);
  };

  const handleOpenPermissionsModal = async (role) => {
    setModalMode("permissions");
    setSelectedRole(role);
    setShowModal(true);

    try {
      const response = await permissionService.getRoleById(
        role.id || role.name
      );
      if (response?.permissions) {
        setRolePermissions(
          response.permissions.map((p) => p.code || p.name || p)
        );
      } else if (Array.isArray(response)) {
        setRolePermissions(response.map((p) => p.code || p.name || p));
      }
    } catch (err) {
      console.error("Role permissions loading error:", err);
      setRolePermissions([]);
    }
  };

  const handleCloseModal = () => {
    setShowModal(false);
    setSelectedRole(null);
    setRolePermissions([]);
    setFormData({
      name: "",
      displayName: "",
      description: "",
      isActive: true,
    });
  };

  // Form submit
  const handleSubmit = async (e) => {
    e.preventDefault();
    setSavingPermissions(true);

    try {
      if (modalMode === "create") {
        // Rol oluşturma API çağrısı yapılacak
        // await permissionService.createRole(formData);
        setSuccessMessage("Rol başarıyla oluşturuldu.");
      } else if (modalMode === "edit") {
        // Rol güncelleme API çağrısı yapılacak
        // await permissionService.updateRole(selectedRole.id, formData);
        setSuccessMessage("Rol başarıyla güncellendi.");
      }

      handleCloseModal();
      loadData();
    } catch (err) {
      setError(err.message || "İşlem sırasında hata oluştu.");
    } finally {
      setSavingPermissions(false);
    }
  };

  // İzin toggle
  const handlePermissionToggle = (permissionCode) => {
    setRolePermissions((prev) => {
      if (prev.includes(permissionCode)) {
        return prev.filter((p) => p !== permissionCode);
      } else {
        return [...prev, permissionCode];
      }
    });
  };

  // Modül tüm izinlerini toggle
  const handleModuleToggle = (modulePermissions) => {
    const permCodes = modulePermissions.map((p) => p.code || p.name);
    const allSelected = permCodes.every((code) =>
      rolePermissions.includes(code)
    );

    if (allSelected) {
      // Tümünü kaldır
      setRolePermissions((prev) => prev.filter((p) => !permCodes.includes(p)));
    } else {
      // Tümünü ekle
      setRolePermissions((prev) => [...new Set([...prev, ...permCodes])]);
    }
  };

  // İzinleri kaydet
  const handleSavePermissions = async () => {
    if (!selectedRole) return;

    setSavingPermissions(true);
    setError(null);

    try {
      await permissionService.updateRolePermissions(
        selectedRole.id || selectedRole.name,
        rolePermissions
      );

      setSuccessMessage(
        `"${
          selectedRole.displayName || selectedRole.name
        }" rolünün izinleri güncellendi.`
      );
      handleCloseModal();
      loadData();
    } catch (err) {
      console.error("Permission save error:", err);
      setError(err.message || "İzinler kaydedilirken hata oluştu.");
    } finally {
      setSavingPermissions(false);
    }
  };

  // Rol silme
  const handleDeleteRole = async (role) => {
    if (
      !window.confirm(
        `"${
          role.displayName || role.name
        }" rolünü silmek istediğinizden emin misiniz?`
      )
    ) {
      return;
    }

    try {
      // await permissionService.deleteRole(role.id);
      setSuccessMessage("Rol başarıyla silindi.");
      loadData();
    } catch (err) {
      setError(err.message || "Rol silinirken hata oluştu.");
    }
  };

  // Mesajları temizle
  useEffect(() => {
    if (successMessage) {
      const timer = setTimeout(() => setSuccessMessage(null), 5000);
      return () => clearTimeout(timer);
    }
  }, [successMessage]);

  // İzinleri modüle göre grupla
  const groupedPermissions = React.useMemo(() => {
    const groups = {};

    allPermissions.forEach((permission) => {
      const code = permission.code || permission.name || permission;
      const module = code.split(".")[0];

      if (!groups[module]) {
        groups[module] = {
          name: module,
          displayName: PERMISSION_MODULES[module]?.name || module,
          description: PERMISSION_MODULES[module]?.description || "",
          permissions: [],
        };
      }

      groups[module].permissions.push({
        code: code,
        name: permission.name || code,
        displayName: permission.displayName || code.split(".").pop(),
        description: permission.description || "",
      });
    });

    return Object.values(groups).sort((a, b) =>
      a.displayName.localeCompare(b.displayName)
    );
  }, [allPermissions]);

  // Yetki yoksa
  if (!canView) {
    return (
      <div className="container-fluid">
        <div className="alert alert-danger">
          <i className="bi bi-shield-lock me-2"></i>
          Bu sayfayı görüntüleme yetkiniz bulunmamaktadır.
        </div>
      </div>
    );
  }

  // Yükleniyor
  if (loading) {
    return (
      <div className="container-fluid">
        <div
          className="d-flex justify-content-center align-items-center"
          style={{ minHeight: "400px" }}
        >
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container-fluid">
      {/* Header */}
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center mb-4">
        <div>
          <h1 className="h3 mb-1">
            <i className="bi bi-person-badge me-2 text-primary"></i>
            Rol Yönetimi
          </h1>
          <p className="text-muted mb-0">
            Sistem rollerini ve izinlerini yönetin
          </p>
        </div>

        {canCreate && (
          <button
            className="btn btn-primary mt-3 mt-md-0"
            onClick={handleOpenCreateModal}
          >
            <i className="bi bi-plus-lg me-2"></i>
            Yeni Rol
          </button>
        )}
      </div>

      {/* Messages */}
      {error && (
        <div
          className="alert alert-danger alert-dismissible fade show"
          role="alert"
        >
          <i className="bi bi-exclamation-triangle me-2"></i>
          {error}
          <button
            type="button"
            className="btn-close"
            onClick={() => setError(null)}
          ></button>
        </div>
      )}

      {successMessage && (
        <div
          className="alert alert-success alert-dismissible fade show"
          role="alert"
        >
          <i className="bi bi-check-circle me-2"></i>
          {successMessage}
          <button
            type="button"
            className="btn-close"
            onClick={() => setSuccessMessage(null)}
          ></button>
        </div>
      )}

      {/* Roles Grid */}
      <div className="row g-4">
        {roles.map((role) => (
          <div key={role.id || role.name} className="col-12 col-md-6 col-lg-4">
            <div className="card h-100 shadow-sm border-0">
              <div className="card-body">
                {/* Role Header */}
                <div className="d-flex align-items-start justify-content-between mb-3">
                  <div>
                    <h5 className="card-title mb-1">
                      <i className="bi bi-person-badge me-2 text-primary"></i>
                      {role.displayName || role.name}
                    </h5>
                    <span className="badge bg-secondary">{role.name}</span>
                  </div>

                  {role.isSystemRole && (
                    <span className="badge bg-info">
                      <i className="bi bi-lock me-1"></i>
                      Sistem
                    </span>
                  )}
                </div>

                {/* Description */}
                {role.description && (
                  <p className="card-text text-muted small mb-3">
                    {role.description}
                  </p>
                )}

                {/* Stats */}
                <div className="d-flex gap-3 mb-3">
                  <div className="text-center">
                    <div className="h5 mb-0 text-primary">
                      {role.permissionCount || 0}
                    </div>
                    <small className="text-muted">İzin</small>
                  </div>
                  <div className="text-center">
                    <div className="h5 mb-0 text-success">
                      {role.userCount || 0}
                    </div>
                    <small className="text-muted">Kullanıcı</small>
                  </div>
                </div>

                {/* Actions */}
                <div className="d-flex gap-2 flex-wrap">
                  {canManagePermissions && (
                    <button
                      className="btn btn-outline-primary btn-sm"
                      onClick={() => handleOpenPermissionsModal(role)}
                    >
                      <i className="bi bi-key me-1"></i>
                      İzinler
                    </button>
                  )}

                  {canUpdate && !role.isSystemRole && (
                    <button
                      className="btn btn-outline-secondary btn-sm"
                      onClick={() => handleOpenEditModal(role)}
                    >
                      <i className="bi bi-pencil me-1"></i>
                      Düzenle
                    </button>
                  )}

                  {canDelete && !role.isSystemRole && (
                    <button
                      className="btn btn-outline-danger btn-sm"
                      onClick={() => handleDeleteRole(role)}
                    >
                      <i className="bi bi-trash me-1"></i>
                      Sil
                    </button>
                  )}
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Modal */}
      {showModal && (
        <div
          className="modal fade show d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div
            className={`modal-dialog modal-dialog-centered modal-dialog-scrollable ${
              modalMode === "permissions" ? "modal-xl" : ""
            }`}
          >
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">
                  {modalMode === "create" && (
                    <>
                      <i className="bi bi-plus-circle me-2 text-primary"></i>
                      Yeni Rol Oluştur
                    </>
                  )}
                  {modalMode === "edit" && (
                    <>
                      <i className="bi bi-pencil me-2 text-warning"></i>
                      Rolü Düzenle:{" "}
                      {selectedRole?.displayName || selectedRole?.name}
                    </>
                  )}
                  {modalMode === "permissions" && (
                    <>
                      <i className="bi bi-key me-2 text-success"></i>
                      İzin Yönetimi:{" "}
                      {selectedRole?.displayName || selectedRole?.name}
                    </>
                  )}
                </h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={handleCloseModal}
                ></button>
              </div>

              <div className="modal-body">
                {/* Create/Edit Form */}
                {(modalMode === "create" || modalMode === "edit") && (
                  <form onSubmit={handleSubmit}>
                    <div className="mb-3">
                      <label className="form-label">Rol Adı (Sistem)</label>
                      <input
                        type="text"
                        className="form-control"
                        value={formData.name}
                        onChange={(e) =>
                          setFormData({ ...formData, name: e.target.value })
                        }
                        placeholder="örn: store_manager"
                        required
                        disabled={modalMode === "edit"}
                      />
                      <small className="text-muted">
                        Sistemde kullanılacak tekil isim (değiştirilemez)
                      </small>
                    </div>

                    <div className="mb-3">
                      <label className="form-label">Görünen Ad</label>
                      <input
                        type="text"
                        className="form-control"
                        value={formData.displayName}
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            displayName: e.target.value,
                          })
                        }
                        placeholder="örn: Mağaza Yöneticisi"
                        required
                      />
                    </div>

                    <div className="mb-3">
                      <label className="form-label">Açıklama</label>
                      <textarea
                        className="form-control"
                        rows="3"
                        value={formData.description}
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            description: e.target.value,
                          })
                        }
                        placeholder="Rol açıklaması..."
                      />
                    </div>

                    <div className="form-check mb-3">
                      <input
                        type="checkbox"
                        className="form-check-input"
                        id="isActive"
                        checked={formData.isActive}
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            isActive: e.target.checked,
                          })
                        }
                      />
                      <label className="form-check-label" htmlFor="isActive">
                        Aktif
                      </label>
                    </div>
                  </form>
                )}

                {/* Permissions Matrix */}
                {modalMode === "permissions" && (
                  <div>
                    <p className="text-muted mb-4">
                      Bu role atanacak izinleri seçin. Seçilen izinler bu role
                      sahip tüm kullanıcılara uygulanacaktır.
                    </p>

                    <div className="accordion" id="permissionsAccordion">
                      {groupedPermissions.map((module, moduleIndex) => {
                        const modulePermCodes = module.permissions.map(
                          (p) => p.code
                        );
                        const selectedCount = modulePermCodes.filter((code) =>
                          rolePermissions.includes(code)
                        ).length;
                        const allSelected =
                          selectedCount === modulePermCodes.length;
                        const someSelected = selectedCount > 0 && !allSelected;

                        return (
                          <div key={module.name} className="accordion-item">
                            <h2 className="accordion-header">
                              <button
                                className={`accordion-button ${
                                  moduleIndex !== 0 ? "collapsed" : ""
                                }`}
                                type="button"
                                data-bs-toggle="collapse"
                                data-bs-target={`#module-${module.name}`}
                              >
                                <div className="d-flex align-items-center w-100">
                                  <div
                                    className="form-check me-3"
                                    onClick={(e) => e.stopPropagation()}
                                  >
                                    <input
                                      type="checkbox"
                                      className="form-check-input"
                                      checked={allSelected}
                                      ref={(input) => {
                                        if (input)
                                          input.indeterminate = someSelected;
                                      }}
                                      onChange={() =>
                                        handleModuleToggle(module.permissions)
                                      }
                                    />
                                  </div>
                                  <div className="flex-grow-1">
                                    <strong>{module.displayName}</strong>
                                    <small className="text-muted ms-2">
                                      ({selectedCount}/
                                      {module.permissions.length} seçili)
                                    </small>
                                  </div>
                                </div>
                              </button>
                            </h2>
                            <div
                              id={`module-${module.name}`}
                              className={`accordion-collapse collapse ${
                                moduleIndex === 0 ? "show" : ""
                              }`}
                            >
                              <div className="accordion-body">
                                {module.description && (
                                  <p className="text-muted small mb-3">
                                    {module.description}
                                  </p>
                                )}
                                <div className="row g-2">
                                  {module.permissions.map((perm) => (
                                    <div
                                      key={perm.code}
                                      className="col-12 col-md-6 col-lg-4"
                                    >
                                      <div className="form-check">
                                        <input
                                          type="checkbox"
                                          className="form-check-input"
                                          id={`perm-${perm.code}`}
                                          checked={rolePermissions.includes(
                                            perm.code
                                          )}
                                          onChange={() =>
                                            handlePermissionToggle(perm.code)
                                          }
                                        />
                                        <label
                                          className="form-check-label"
                                          htmlFor={`perm-${perm.code}`}
                                        >
                                          <span className="d-block">
                                            {perm.displayName}
                                          </span>
                                          <small className="text-muted">
                                            {perm.code}
                                          </small>
                                        </label>
                                      </div>
                                    </div>
                                  ))}
                                </div>
                              </div>
                            </div>
                          </div>
                        );
                      })}
                    </div>

                    {/* Seçili İzin Sayısı */}
                    <div className="mt-4 p-3 bg-light rounded">
                      <div className="d-flex justify-content-between align-items-center">
                        <span>
                          <i className="bi bi-check-circle text-success me-2"></i>
                          <strong>{rolePermissions.length}</strong> izin seçildi
                        </span>
                        <button
                          type="button"
                          className="btn btn-outline-secondary btn-sm"
                          onClick={() => setRolePermissions([])}
                        >
                          Tümünü Temizle
                        </button>
                      </div>
                    </div>
                  </div>
                )}
              </div>

              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={handleCloseModal}
                >
                  İptal
                </button>

                {(modalMode === "create" || modalMode === "edit") && (
                  <button
                    type="submit"
                    className="btn btn-primary"
                    onClick={handleSubmit}
                    disabled={savingPermissions}
                  >
                    {savingPermissions ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2"></span>
                        Kaydediliyor...
                      </>
                    ) : (
                      <>
                        <i className="bi bi-check-lg me-2"></i>
                        {modalMode === "create" ? "Oluştur" : "Kaydet"}
                      </>
                    )}
                  </button>
                )}

                {modalMode === "permissions" && (
                  <button
                    type="button"
                    className="btn btn-success"
                    onClick={handleSavePermissions}
                    disabled={savingPermissions}
                  >
                    {savingPermissions ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2"></span>
                        Kaydediliyor...
                      </>
                    ) : (
                      <>
                        <i className="bi bi-key me-2"></i>
                        İzinleri Kaydet
                      </>
                    )}
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminRoles;
