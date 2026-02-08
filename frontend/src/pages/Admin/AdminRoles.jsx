// =============================================================================
// AdminRoles.jsx - Rol Yönetimi Sayfası
// =============================================================================
// Bu sayfa rollerin yönetimini ve rol-izin atamalarını sağlar.
// Sadece SuperAdmin tarafından erişilebilir.
// =============================================================================

import { useState, useEffect, useCallback, useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";
import permissionService, {
  PERMISSIONS,
  PERMISSION_MODULES,
} from "../../services/permissionService";

const getValue = (obj, ...keys) => {
  for (const key of keys) {
    if (obj?.[key] !== undefined && obj?.[key] !== null) {
      return obj[key];
    }
  }
  return undefined;
};

const normalizeRole = (role) => {
  const id = getValue(role, "id", "Id", "roleId", "RoleId");
  const name = getValue(role, "name", "Name", "roleName", "RoleName") || "";
  return {
    id,
    name,
    displayName:
      getValue(role, "displayName", "DisplayName", "roleDisplayName", "RoleDisplayName") ||
      name,
    description: getValue(role, "description", "Description") || "",
    isSystemRole: getValue(role, "isSystemRole", "IsSystemRole") === true,
    canEdit: getValue(role, "canEdit", "CanEdit") !== false,
    permissionCount:
      getValue(role, "permissionCount", "PermissionCount") || 0,
    userCount: getValue(role, "userCount", "UserCount") || 0,
  };
};

const normalizePermission = (permission) => {
  const code =
    getValue(
      permission,
      "code",
      "Code",
      "name",
      "Name",
      "permissionName",
      "PermissionName"
    ) || "";
  return {
    id: getValue(permission, "id", "Id", "permissionId", "PermissionId"),
    code,
    name: getValue(permission, "name", "Name") || code,
    displayName:
      getValue(permission, "displayName", "DisplayName") ||
      code.split(".").pop() ||
      code,
    description: getValue(permission, "description", "Description") || "",
  };
};

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

      const roleListRaw = Array.isArray(rolesResponse)
        ? rolesResponse
        : rolesResponse?.roles || rolesResponse?.Roles || [];

      const permissionListRaw = Array.isArray(permissionsResponse)
        ? permissionsResponse
        : permissionsResponse?.permissions ||
          permissionsResponse?.Permissions ||
          [];

      // Tüm rollerin görünmesi için matrix rol listesini de birleştir.
      let matrixRolesRaw = [];
      try {
        const matrix = await permissionService.getRolePermissionMatrix();
        matrixRolesRaw =
          matrix?.roleMatrix || matrix?.RoleMatrix || [];
      } catch {
        matrixRolesRaw = [];
      }

      const mergedRoleMap = new Map();
      [...roleListRaw, ...matrixRolesRaw]
        .map(normalizeRole)
        .filter((r) => r.name)
        .forEach((role) => {
          const key = (role.name || "").toLowerCase();
          if (!key) return;
          if (!mergedRoleMap.has(key)) {
            mergedRoleMap.set(key, role);
          } else {
            mergedRoleMap.set(key, { ...mergedRoleMap.get(key), ...role });
          }
        });

      setRoles(
        Array.from(mergedRoleMap.values()).sort((a, b) =>
          (a.displayName || a.name).localeCompare(b.displayName || b.name)
        )
      );
      setAllPermissions(permissionListRaw.map(normalizePermission).filter((p) => p.code));
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
      const roleId = role.id;
      if (!roleId) {
        setRolePermissions([]);
        return;
      }

      const response = await permissionService.getRoleById(roleId);

      const directPermissions = getValue(response, "permissions", "Permissions") || [];
      const permissionsByModule = getValue(response, "permissionsByModule", "PermissionsByModule") || [];
      const groupedPermissions = permissionsByModule.flatMap(
        (m) => getValue(m, "permissions", "Permissions") || []
      );

      const permissionCodes = [...directPermissions, ...groupedPermissions]
        .map((p) => getValue(p, "code", "Code", "name", "Name"))
        .filter(Boolean);

      setRolePermissions(Array.from(new Set(permissionCodes)));
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
      if (!selectedRole.id) {
        throw new Error("Rol ID bulunamadı.");
      }

      const permissionCodeToId = {};
      allPermissions.forEach((perm) => {
        if (perm.code && perm.id !== undefined && perm.id !== null) {
          permissionCodeToId[perm.code] = perm.id;
        }
      });

      const permissionIds = rolePermissions
        .map((code) => permissionCodeToId[code])
        .filter((id) => id !== undefined);

      await permissionService.updateRolePermissions(
        selectedRole.id,
        permissionIds
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
  const groupedPermissions = useMemo(() => {
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
      <div className="container-fluid p-2 p-md-4">
        <div className="alert alert-danger">
          <i className="fas fa-shield-alt me-2"></i>
          Bu sayfayı görüntüleme yetkiniz bulunmamaktadır.
        </div>
      </div>
    );
  }

  // Yükleniyor
  if (loading) {
    return (
      <div className="container-fluid p-2 p-md-4">
        <div
          className="d-flex justify-content-center align-items-center"
          style={{ minHeight: "300px" }}
        >
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container-fluid p-2 p-md-4">
      {/* Header - Responsive */}
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center mb-3 mb-md-4 gap-2">
        <div className="mb-2 mb-md-0">
          <h1 className="h4 h3-md fw-bold mb-1" style={{ color: "#2d3748" }}>
            <i
              className="fas fa-user-shield me-2"
              style={{ color: "#f57c00" }}
            ></i>
            Rol Yönetimi
          </h1>
          <p className="text-muted mb-0" style={{ fontSize: "0.8rem" }}>
            Sistem rollerini ve izinlerini yönetin
          </p>
        </div>

        {canCreate && (
          <button
            className="btn text-white fw-semibold"
            style={{
              background: "linear-gradient(135deg, #f57c00, #ff9800)",
              minHeight: "44px",
            }}
            onClick={handleOpenCreateModal}
          >
            <i className="fas fa-plus me-2"></i>
            Yeni Rol
          </button>
        )}
      </div>

      {/* Messages */}
      {error && (
        <div
          className="alert alert-danger alert-dismissible fade show py-2"
          role="alert"
        >
          <i className="fas fa-exclamation-triangle me-2"></i>
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
          className="alert alert-success alert-dismissible fade show py-2"
          role="alert"
        >
          <i className="fas fa-check-circle me-2"></i>
          {successMessage}
          <button
            type="button"
            className="btn-close"
            onClick={() => setSuccessMessage(null)}
          ></button>
        </div>
      )}

      {/* Roles Grid - Responsive */}
      <div className="row g-2 g-md-3">
        {roles.map((role) => (
          <div key={role.id || role.name} className="col-12 col-md-6 col-lg-4">
            <div className="card h-100 shadow-sm border-0">
              <div className="card-body p-3">
                {/* Role Header */}
                <div className="d-flex align-items-start justify-content-between mb-2">
                  <div className="flex-grow-1 overflow-hidden">
                    <h6
                      className="card-title mb-1 text-truncate"
                      style={{ fontSize: "0.95rem" }}
                    >
                      <i className="fas fa-user-tag me-2 text-primary"></i>
                      {role.displayName || role.name}
                    </h6>
                    <span
                      className="badge bg-secondary"
                      style={{ fontSize: "0.65rem" }}
                    >
                      {role.name}
                    </span>
                  </div>

                  {role.isSystemRole && (
                    <span
                      className="badge bg-info ms-2"
                      style={{ fontSize: "0.65rem" }}
                    >
                      <i className="fas fa-lock me-1"></i>
                      Sistem
                    </span>
                  )}
                </div>

                {/* Description */}
                {role.description && (
                  <p
                    className="card-text text-muted small mb-2"
                    style={{ fontSize: "0.75rem" }}
                  >
                    {role.description}
                  </p>
                )}

                {/* Stats */}
                <div className="d-flex gap-3 mb-3">
                  <div className="text-center">
                    <div className="h6 mb-0 text-primary">
                      {role.permissionCount || 0}
                    </div>
                    <small
                      className="text-muted"
                      style={{ fontSize: "0.7rem" }}
                    >
                      İzin
                    </small>
                  </div>
                  <div className="text-center">
                    <div className="h6 mb-0 text-success">
                      {role.userCount || 0}
                    </div>
                    <small
                      className="text-muted"
                      style={{ fontSize: "0.7rem" }}
                    >
                      Kullanıcı
                    </small>
                  </div>
                </div>

                {/* Actions - Touch-friendly */}
                <div className="d-flex gap-2 flex-wrap">
                  {canManagePermissions && (
                    <button
                      className="btn btn-outline-primary btn-sm"
                      onClick={() => handleOpenPermissionsModal(role)}
                      style={{ minHeight: "40px", minWidth: "80px" }}
                    >
                      <i className="fas fa-key me-1"></i>
                      İzinler
                    </button>
                  )}

                  {canUpdate && !role.isSystemRole && (
                    <button
                      className="btn btn-outline-secondary btn-sm"
                      onClick={() => handleOpenEditModal(role)}
                      style={{ minHeight: "40px", minWidth: "80px" }}
                    >
                      <i className="fas fa-edit me-1"></i>
                      Düzenle
                    </button>
                  )}

                  {canDelete && !role.isSystemRole && (
                    <button
                      className="btn btn-outline-danger btn-sm"
                      onClick={() => handleDeleteRole(role)}
                      style={{ minHeight: "40px", minWidth: "60px" }}
                    >
                      <i className="fas fa-trash me-1"></i>
                      Sil
                    </button>
                  )}
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Modal - Mobilde full-width */}
      {showModal && (
        <div
          className="modal fade show d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div
            className={`modal-dialog modal-dialog-centered modal-dialog-scrollable modal-fullscreen-sm-down ${
              modalMode === "permissions" ? "modal-xl" : ""
            }`}
          >
            <div
              className="modal-content border-0"
              style={{ borderRadius: "16px" }}
            >
              <div className="modal-header border-0 p-3">
                <h5
                  className="modal-title fw-bold"
                  style={{ fontSize: "1rem" }}
                >
                  {modalMode === "create" && (
                    <>
                      <i className="fas fa-plus-circle me-2 text-primary"></i>
                      Yeni Rol Oluştur
                    </>
                  )}
                  {modalMode === "edit" && (
                    <>
                      <i className="fas fa-edit me-2 text-warning"></i>
                      Rolü Düzenle:{" "}
                      {selectedRole?.displayName || selectedRole?.name}
                    </>
                  )}
                  {modalMode === "permissions" && (
                    <>
                      <i className="fas fa-key me-2 text-success"></i>
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

              <div className="modal-body p-3">
                {/* Create/Edit Form */}
                {(modalMode === "create" || modalMode === "edit") && (
                  <form onSubmit={handleSubmit} className="admin-mobile-form">
                    <div className="mb-3">
                      <label
                        className="form-label fw-semibold"
                        style={{ fontSize: "0.85rem" }}
                      >
                        Rol Adı (Sistem)
                      </label>
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
                        style={{ minHeight: "44px" }}
                      />
                      <small
                        className="text-muted"
                        style={{ fontSize: "0.75rem" }}
                      >
                        Sistemde kullanılacak tekil isim (değiştirilemez)
                      </small>
                    </div>

                    <div className="mb-3">
                      <label
                        className="form-label fw-semibold"
                        style={{ fontSize: "0.85rem" }}
                      >
                        Görünen Ad
                      </label>
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
                        style={{ minHeight: "44px" }}
                      />
                    </div>

                    <div className="mb-3">
                      <label
                        className="form-label fw-semibold"
                        style={{ fontSize: "0.85rem" }}
                      >
                        Açıklama
                      </label>
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

                    <div
                      className="form-check mb-3"
                      style={{
                        minHeight: "44px",
                        display: "flex",
                        alignItems: "center",
                      }}
                    >
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
                        style={{ width: "1.25rem", height: "1.25rem" }}
                      />
                      <label
                        className="form-check-label fw-semibold ms-2"
                        htmlFor="isActive"
                      >
                        Aktif
                      </label>
                    </div>
                  </form>
                )}

                {/* Permissions Matrix - Responsive */}
                {modalMode === "permissions" && (
                  <div>
                    <p
                      className="text-muted mb-3"
                      style={{ fontSize: "0.8rem" }}
                    >
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
                                className={`accordion-button py-2 ${
                                  moduleIndex !== 0 ? "collapsed" : ""
                                }`}
                                type="button"
                                data-bs-toggle="collapse"
                                data-bs-target={`#module-${module.name}`}
                                style={{ fontSize: "0.85rem" }}
                              >
                                <div className="d-flex align-items-center w-100">
                                  <div
                                    className="form-check me-2"
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
                                      style={{
                                        width: "1.25rem",
                                        height: "1.25rem",
                                      }}
                                    />
                                  </div>
                                  <div className="flex-grow-1">
                                    <strong>{module.displayName}</strong>
                                    <small
                                      className="text-muted ms-2"
                                      style={{ fontSize: "0.7rem" }}
                                    >
                                      ({selectedCount}/
                                      {module.permissions.length})
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
                              <div className="accordion-body py-2 px-3">
                                {module.description && (
                                  <p
                                    className="text-muted small mb-2"
                                    style={{ fontSize: "0.75rem" }}
                                  >
                                    {module.description}
                                  </p>
                                )}
                                <div className="row g-2">
                                  {module.permissions.map((perm) => (
                                    <div
                                      key={perm.code}
                                      className="col-12 col-md-6 col-lg-4"
                                    >
                                      <div
                                        className="form-check"
                                        style={{
                                          minHeight: "40px",
                                          display: "flex",
                                          alignItems: "center",
                                        }}
                                      >
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
                                          style={{
                                            width: "1.25rem",
                                            height: "1.25rem",
                                          }}
                                        />
                                        <label
                                          className="form-check-label ms-2"
                                          htmlFor={`perm-${perm.code}`}
                                        >
                                          <span
                                            className="d-block"
                                            style={{ fontSize: "0.8rem" }}
                                          >
                                            {perm.displayName}
                                          </span>
                                          <small
                                            className="text-muted"
                                            style={{ fontSize: "0.65rem" }}
                                          >
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
                    <div className="mt-3 p-2 bg-light rounded">
                      <div className="d-flex justify-content-between align-items-center flex-wrap gap-2">
                        <span style={{ fontSize: "0.85rem" }}>
                          <i className="fas fa-check-circle text-success me-2"></i>
                          <strong>{rolePermissions.length}</strong> izin seçildi
                        </span>
                        <button
                          type="button"
                          className="btn btn-outline-secondary btn-sm"
                          onClick={() => setRolePermissions([])}
                          style={{ minHeight: "36px" }}
                        >
                          Tümünü Temizle
                        </button>
                      </div>
                    </div>
                  </div>
                )}
              </div>

              <div className="modal-footer border-0 p-3">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={handleCloseModal}
                  style={{ minHeight: "44px" }}
                >
                  İptal
                </button>

                {(modalMode === "create" || modalMode === "edit") && (
                  <button
                    type="submit"
                    className="btn btn-primary"
                    onClick={handleSubmit}
                    disabled={savingPermissions}
                    style={{ minHeight: "44px" }}
                  >
                    {savingPermissions ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2"></span>
                        Kaydediliyor...
                      </>
                    ) : (
                      <>
                        <i className="fas fa-check me-2"></i>
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
                    style={{ minHeight: "44px" }}
                  >
                    {savingPermissions ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2"></span>
                        Kaydediliyor...
                      </>
                    ) : (
                      <>
                        <i className="fas fa-key me-2"></i>
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
