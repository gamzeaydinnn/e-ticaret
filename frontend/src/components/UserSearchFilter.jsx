// ============================================================================
// UserSearchFilter.jsx - Kullanıcı Arama ve Filtreleme Bileşeni
// ============================================================================
// AdminUsers sayfası için arama, rol ve durum filtreleme özellikleri sağlar.
// Debounced search ile performanslı arama yapar.
// ============================================================================

import { useState, useEffect, useCallback } from "react";

// Debounce hook - arama performansı için
const useDebounce = (value, delay) => {
  const [debouncedValue, setDebouncedValue] = useState(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => clearTimeout(handler);
  }, [value, delay]);

  return debouncedValue;
};

/**
 * Kullanıcı arama ve filtreleme bileşeni
 * @param {Object} props
 * @param {Function} props.onFilterChange - Filtre değişikliğinde çağrılacak callback
 * @param {Array} props.roles - Mevcut roller listesi
 * @param {number} props.totalCount - Toplam kullanıcı sayısı
 * @param {number} props.filteredCount - Filtrelenmiş kullanıcı sayısı
 */
const UserSearchFilter = ({
  onFilterChange,
  roles = [],
  totalCount = 0,
  filteredCount = 0,
}) => {
  const [search, setSearch] = useState("");
  const [roleFilter, setRoleFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");

  // Debounced search - 300ms gecikme
  const debouncedSearch = useDebounce(search, 300);

  // Filtre değişikliğini parent'a bildir
  useEffect(() => {
    onFilterChange?.({
      search: debouncedSearch,
      role: roleFilter,
      status: statusFilter,
    });
  }, [debouncedSearch, roleFilter, statusFilter, onFilterChange]);

  // Filtreleri temizle
  const clearFilters = useCallback(() => {
    setSearch("");
    setRoleFilter("all");
    setStatusFilter("all");
  }, []);

  // Aktif filtre var mı?
  const hasActiveFilters =
    search || roleFilter !== "all" || statusFilter !== "all";

  return (
    <div
      className="card border-0 shadow-sm mb-4"
      style={{ borderRadius: "12px" }}
    >
      <div className="card-body p-3">
        <div className="row g-3 align-items-end">
          {/* Arama kutusu */}
          <div className="col-12 col-md-4">
            <label className="form-label small text-muted fw-medium mb-1">
              <i className="fas fa-search me-1"></i>
              Ara
            </label>
            <div className="input-group">
              <input
                type="text"
                className="form-control"
                placeholder="Ad, soyad veya email..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                style={{
                  borderRadius: "8px 0 0 8px",
                  border: "1px solid #e2e8f0",
                  padding: "0.6rem 1rem",
                }}
              />
              {search && (
                <button
                  className="btn btn-outline-secondary"
                  type="button"
                  onClick={() => setSearch("")}
                  style={{ borderRadius: "0 8px 8px 0" }}
                >
                  <i className="fas fa-times"></i>
                </button>
              )}
            </div>
          </div>

          {/* Rol filtresi */}
          <div className="col-6 col-md-3">
            <label className="form-label small text-muted fw-medium mb-1">
              <i className="fas fa-user-tag me-1"></i>
              Rol
            </label>
            <select
              className="form-select"
              value={roleFilter}
              onChange={(e) => setRoleFilter(e.target.value)}
              style={{
                borderRadius: "8px",
                border: "1px solid #e2e8f0",
                padding: "0.6rem 1rem",
              }}
            >
              <option value="all">Tüm Roller</option>
              {roles.map((role) => (
                <option key={role} value={role}>
                  {getRoleDisplayName(role)}
                </option>
              ))}
            </select>
          </div>

          {/* Durum filtresi */}
          <div className="col-6 col-md-3">
            <label className="form-label small text-muted fw-medium mb-1">
              <i className="fas fa-toggle-on me-1"></i>
              Durum
            </label>
            <select
              className="form-select"
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              style={{
                borderRadius: "8px",
                border: "1px solid #e2e8f0",
                padding: "0.6rem 1rem",
              }}
            >
              <option value="all">Tüm Durumlar</option>
              <option value="active">Aktif</option>
              <option value="inactive">Pasif</option>
            </select>
          </div>

          {/* Temizle butonu */}
          <div className="col-12 col-md-2">
            {hasActiveFilters && (
              <button
                className="btn btn-outline-secondary w-100"
                onClick={clearFilters}
                style={{ borderRadius: "8px", padding: "0.6rem" }}
              >
                <i className="fas fa-eraser me-1"></i>
                Temizle
              </button>
            )}
          </div>
        </div>

        {/* Sonuç sayısı */}
        <div className="mt-3 pt-2 border-top">
          <small className="text-muted">
            {hasActiveFilters ? (
              <>
                <span className="fw-medium text-primary">{filteredCount}</span>{" "}
                sonuç bulundu
                <span className="text-muted">
                  {" "}
                  (toplam {totalCount} kullanıcı)
                </span>
              </>
            ) : (
              <>
                Toplam <span className="fw-medium">{totalCount}</span> kullanıcı
              </>
            )}
          </small>
        </div>
      </div>
    </div>
  );
};

/**
 * Rol adını Türkçe görüntü adına çevirir
 */
const getRoleDisplayName = (role) => {
  const roleNames = {
    SuperAdmin: "Süper Admin",
    Admin: "Admin",
    StoreManager: "Mağaza Yöneticisi",
    CustomerSupport: "Müşteri Destek",
    Logistics: "Lojistik",
    User: "Kullanıcı",
    Customer: "Müşteri",
  };
  return roleNames[role] || role;
};

/**
 * Kullanıcı listesini filtreler
 * @param {Array} users - Kullanıcı listesi
 * @param {Object} filters - Filtre parametreleri
 * @returns {Array} Filtrelenmiş kullanıcı listesi
 */
export const filterUsers = (users, filters) => {
  if (!users || !Array.isArray(users)) return [];

  let filtered = [...users];

  // Arama filtresi
  if (filters.search) {
    const searchLower = filters.search.toLowerCase();
    filtered = filtered.filter((user) => {
      const fullName = `${user.firstName || ""} ${
        user.lastName || ""
      }`.toLowerCase();
      const email = (user.email || "").toLowerCase();
      return fullName.includes(searchLower) || email.includes(searchLower);
    });
  }

  // Rol filtresi
  if (filters.role && filters.role !== "all") {
    filtered = filtered.filter((user) => user.role === filters.role);
  }

  // Durum filtresi
  if (filters.status && filters.status !== "all") {
    const isActive = filters.status === "active";
    filtered = filtered.filter((user) => user.isActive === isActive);
  }

  return filtered;
};

export default UserSearchFilter;
