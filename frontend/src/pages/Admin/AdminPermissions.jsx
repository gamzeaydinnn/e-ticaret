// =============================================================================
// AdminPermissions.jsx - İzin Yönetimi Sayfası
// =============================================================================
// Bu sayfa sistemdeki tüm izinlerin görüntülenmesini ve yönetimini sağlar.
// Sadece SuperAdmin tarafından erişilebilir.
// =============================================================================

import React, { useState, useEffect, useCallback } from "react";
import { useAuth } from "../../contexts/AuthContext";
import permissionService, {
  PERMISSIONS,
  PERMISSION_MODULES,
} from "../../services/permissionService";

const AdminPermissions = () => {
  const { user, hasPermission } = useAuth();

  // State
  const [permissions, setPermissions] = useState([]);
  const [roles, setRoles] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [successMessage, setSuccessMessage] = useState(null);

  // Filters
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedModule, setSelectedModule] = useState("all");

  // Role Permission Matrix State
  const [rolePermissionMatrix, setRolePermissionMatrix] = useState({});
  const [savingMatrix, setSavingMatrix] = useState(false);
  const [matrixModified, setMatrixModified] = useState(false);

  // Erişim kontrolü
  const canView =
    user?.role === "SuperAdmin" ||
    hasPermission?.(PERMISSIONS.ROLES_PERMISSIONS);
  const canManage = user?.role === "SuperAdmin";

  // Verileri yükle
  const loadData = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const [permissionsResponse, rolesResponse] = await Promise.all([
        permissionService.getAllPermissions(),
        permissionService.getAllRoles(),
      ]);

      // İzinleri işle
      if (permissionsResponse?.success !== false) {
        const permList = Array.isArray(permissionsResponse)
          ? permissionsResponse
          : permissionsResponse.permissions || [];
        setPermissions(permList);
      }

      // Rolleri ve izinlerini işle
      if (rolesResponse?.success !== false) {
        const roleList = Array.isArray(rolesResponse)
          ? rolesResponse
          : rolesResponse.roles || [];
        setRoles(roleList);

        // Role-permission matrix oluştur
        const matrix = {};
        for (const role of roleList) {
          try {
            const roleData = await permissionService.getRoleById(
              role.id || role.name
            );
            const perms = roleData?.permissions || [];
            matrix[role.name] = perms.map((p) => p.code || p.name || p);
          } catch (e) {
            matrix[role.name] = [];
          }
        }
        setRolePermissionMatrix(matrix);
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

  // İzinleri modüle göre grupla
  const groupedPermissions = React.useMemo(() => {
    const groups = {};

    permissions.forEach((permission) => {
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
        displayName:
          permission.displayName || code.split(".").slice(1).join("."),
        description: permission.description || "",
        isActive: permission.isActive !== false,
      });
    });

    return Object.values(groups).sort((a, b) =>
      a.displayName.localeCompare(b.displayName)
    );
  }, [permissions]);

  // Filtrelenmiş izinler
  const filteredGroups = React.useMemo(() => {
    return groupedPermissions
      .map((group) => {
        // Modül filtresi
        if (selectedModule !== "all" && group.name !== selectedModule) {
          return null;
        }

        // Arama filtresi
        if (searchTerm) {
          const filtered = group.permissions.filter(
            (p) =>
              p.code.toLowerCase().includes(searchTerm.toLowerCase()) ||
              p.displayName.toLowerCase().includes(searchTerm.toLowerCase()) ||
              p.description?.toLowerCase().includes(searchTerm.toLowerCase())
          );

          if (filtered.length === 0) return null;

          return { ...group, permissions: filtered };
        }

        return group;
      })
      .filter(Boolean);
  }, [groupedPermissions, selectedModule, searchTerm]);

  // Matrix permission toggle
  const handleMatrixToggle = (roleName, permissionCode) => {
    setRolePermissionMatrix((prev) => {
      const rolePerms = prev[roleName] || [];
      const newPerms = rolePerms.includes(permissionCode)
        ? rolePerms.filter((p) => p !== permissionCode)
        : [...rolePerms, permissionCode];

      return { ...prev, [roleName]: newPerms };
    });
    setMatrixModified(true);
  };

  // Tüm değişiklikleri kaydet
  const handleSaveMatrix = async () => {
    setSavingMatrix(true);
    setError(null);

    try {
      // Her rol için izinleri güncelle
      for (const role of roles) {
        const newPermissions = rolePermissionMatrix[role.name] || [];
        await permissionService.updateRolePermissions(
          role.id || role.name,
          newPermissions
        );
      }

      setSuccessMessage("Tüm izin değişiklikleri kaydedildi.");
      setMatrixModified(false);
    } catch (err) {
      console.error("Matrix save error:", err);
      setError(err.message || "İzinler kaydedilirken hata oluştu.");
    } finally {
      setSavingMatrix(false);
    }
  };

  // Mesajları temizle
  useEffect(() => {
    if (successMessage) {
      const timer = setTimeout(() => setSuccessMessage(null), 5000);
      return () => clearTimeout(timer);
    }
  }, [successMessage]);

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
            <i className="bi bi-key me-2 text-primary"></i>
            İzin Yönetimi
          </h1>
          <p className="text-muted mb-0">
            Sistemdeki tüm izinleri görüntüleyin ve yönetin
          </p>
        </div>

        {matrixModified && canManage && (
          <button
            className="btn btn-success mt-3 mt-md-0"
            onClick={handleSaveMatrix}
            disabled={savingMatrix}
          >
            {savingMatrix ? (
              <>
                <span className="spinner-border spinner-border-sm me-2"></span>
                Kaydediliyor...
              </>
            ) : (
              <>
                <i className="bi bi-check-lg me-2"></i>
                Değişiklikleri Kaydet
              </>
            )}
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

      {/* Filters */}
      <div className="card shadow-sm border-0 mb-4">
        <div className="card-body">
          <div className="row g-3">
            <div className="col-12 col-md-6">
              <label className="form-label">Arama</label>
              <div className="input-group">
                <span className="input-group-text">
                  <i className="bi bi-search"></i>
                </span>
                <input
                  type="text"
                  className="form-control"
                  placeholder="İzin ara..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
              </div>
            </div>

            <div className="col-12 col-md-6">
              <label className="form-label">Modül</label>
              <select
                className="form-select"
                value={selectedModule}
                onChange={(e) => setSelectedModule(e.target.value)}
              >
                <option value="all">Tüm Modüller</option>
                {groupedPermissions.map((group) => (
                  <option key={group.name} value={group.name}>
                    {group.displayName} ({group.permissions.length})
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>
      </div>

      {/* Stats */}
      <div className="row g-3 mb-4">
        <div className="col-6 col-md-3">
          <div className="card border-0 bg-primary bg-opacity-10">
            <div className="card-body text-center py-3">
              <div className="h3 mb-0 text-primary">{permissions.length}</div>
              <small className="text-muted">Toplam İzin</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div className="card border-0 bg-success bg-opacity-10">
            <div className="card-body text-center py-3">
              <div className="h3 mb-0 text-success">
                {groupedPermissions.length}
              </div>
              <small className="text-muted">Modül</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div className="card border-0 bg-info bg-opacity-10">
            <div className="card-body text-center py-3">
              <div className="h3 mb-0 text-info">{roles.length}</div>
              <small className="text-muted">Rol</small>
            </div>
          </div>
        </div>
        <div className="col-6 col-md-3">
          <div className="card border-0 bg-warning bg-opacity-10">
            <div className="card-body text-center py-3">
              <div className="h3 mb-0 text-warning">
                {Object.values(rolePermissionMatrix).reduce(
                  (sum, perms) => sum + perms.length,
                  0
                )}
              </div>
              <small className="text-muted">Aktif Atama</small>
            </div>
          </div>
        </div>
      </div>

      {/* Permission Matrix Table */}
      {canManage && roles.length > 0 && (
        <div className="card shadow-sm border-0 mb-4">
          <div className="card-header bg-white py-3">
            <h5 className="mb-0">
              <i className="bi bi-table me-2 text-primary"></i>
              İzin Matrisi
            </h5>
          </div>
          <div className="card-body p-0">
            <div className="table-responsive" style={{ maxHeight: "600px" }}>
              <table className="table table-bordered table-hover mb-0">
                <thead className="table-light sticky-top">
                  <tr>
                    <th className="text-nowrap" style={{ minWidth: "200px" }}>
                      İzin
                    </th>
                    {roles.map((role) => (
                      <th
                        key={role.name}
                        className="text-center text-nowrap"
                        style={{ minWidth: "100px" }}
                      >
                        <div className="small">
                          {role.displayName || role.name}
                        </div>
                        <span className="badge bg-secondary small">
                          {role.name}
                        </span>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {filteredGroups.map((group) => (
                    <React.Fragment key={group.name}>
                      {/* Module Header */}
                      <tr className="table-secondary">
                        <td colSpan={roles.length + 1} className="fw-bold">
                          <i className="bi bi-folder me-2"></i>
                          {group.displayName}
                        </td>
                      </tr>

                      {/* Permissions */}
                      {group.permissions.map((perm) => (
                        <tr key={perm.code}>
                          <td>
                            <div className="d-flex flex-column">
                              <span className="text-nowrap">
                                {perm.displayName}
                              </span>
                              <small className="text-muted font-monospace">
                                {perm.code}
                              </small>
                            </div>
                          </td>
                          {roles.map((role) => {
                            const hasPermission = rolePermissionMatrix[
                              role.name
                            ]?.includes(perm.code);
                            const isSuperAdmin = role.name === "SuperAdmin";

                            return (
                              <td
                                key={`${role.name}-${perm.code}`}
                                className="text-center align-middle"
                              >
                                {isSuperAdmin ? (
                                  <span className="badge bg-success">
                                    <i className="bi bi-check-lg"></i>
                                  </span>
                                ) : (
                                  <div className="form-check d-flex justify-content-center mb-0">
                                    <input
                                      type="checkbox"
                                      className="form-check-input"
                                      checked={hasPermission}
                                      onChange={() =>
                                        handleMatrixToggle(role.name, perm.code)
                                      }
                                      style={{
                                        width: "1.25rem",
                                        height: "1.25rem",
                                      }}
                                    />
                                  </div>
                                )}
                              </td>
                            );
                          })}
                        </tr>
                      ))}
                    </React.Fragment>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Permission List (Alternative View) */}
      <div className="card shadow-sm border-0">
        <div className="card-header bg-white py-3">
          <h5 className="mb-0">
            <i className="bi bi-list-ul me-2 text-primary"></i>
            İzin Listesi
          </h5>
        </div>
        <div className="card-body">
          <div className="accordion" id="permissionsListAccordion">
            {filteredGroups.map((group, groupIndex) => (
              <div key={group.name} className="accordion-item">
                <h2 className="accordion-header">
                  <button
                    className={`accordion-button ${
                      groupIndex !== 0 ? "collapsed" : ""
                    }`}
                    type="button"
                    data-bs-toggle="collapse"
                    data-bs-target={`#list-${group.name}`}
                  >
                    <div className="d-flex align-items-center w-100">
                      <span className="badge bg-primary me-3">
                        {group.permissions.length}
                      </span>
                      <div className="flex-grow-1">
                        <strong>{group.displayName}</strong>
                        {group.description && (
                          <small className="text-muted d-block">
                            {group.description}
                          </small>
                        )}
                      </div>
                    </div>
                  </button>
                </h2>
                <div
                  id={`list-${group.name}`}
                  className={`accordion-collapse collapse ${
                    groupIndex === 0 ? "show" : ""
                  }`}
                >
                  <div className="accordion-body">
                    <div className="table-responsive">
                      <table className="table table-sm table-hover mb-0">
                        <thead className="table-light">
                          <tr>
                            <th>Kod</th>
                            <th>Açıklama</th>
                            <th className="text-center">Durum</th>
                          </tr>
                        </thead>
                        <tbody>
                          {group.permissions.map((perm) => (
                            <tr key={perm.code}>
                              <td>
                                <code className="bg-light px-2 py-1 rounded">
                                  {perm.code}
                                </code>
                              </td>
                              <td>{perm.description || perm.displayName}</td>
                              <td className="text-center">
                                {perm.isActive ? (
                                  <span className="badge bg-success">
                                    Aktif
                                  </span>
                                ) : (
                                  <span className="badge bg-secondary">
                                    Pasif
                                  </span>
                                )}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* No Results */}
      {filteredGroups.length === 0 && (
        <div className="text-center py-5">
          <i className="bi bi-search display-1 text-muted"></i>
          <h5 className="mt-3">Sonuç Bulunamadı</h5>
          <p className="text-muted">
            Arama kriterlerinize uygun izin bulunamadı.
          </p>
          <button
            className="btn btn-outline-primary"
            onClick={() => {
              setSearchTerm("");
              setSelectedModule("all");
            }}
          >
            Filtreleri Temizle
          </button>
        </div>
      )}
    </div>
  );
};

export default AdminPermissions;
