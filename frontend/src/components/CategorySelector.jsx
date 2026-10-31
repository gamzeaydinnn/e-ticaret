import React, { useState, useEffect } from "react";
import PropTypes from "prop-types";
import api from "../services/api";
import categoryServiceReal from "../services/categoryServiceReal";

/**
 * CategorySelector Bileşeni
 * 
 * Ana Sayfa Ürün Blokları ve Admin Panel için hiyerarşik kategori seçici.
 * Hem ana kategorileri hem de alt kategorileri seçmeyi destekler.
 */
export default function CategorySelector({ value, onChange, name = "targetCategoryId" }) {
  const [categories, setCategories] = useState([]);
  const [expandedParents, setExpandedParents] = useState(new Set());
  const [searchTerm, setSearchTerm] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadCategories();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadCategories = async () => {
    try {
      setLoading(true);
      let catsWithSubs = [];

      try {
        const rootResponse = await api.get("/api/categories/root");
        const rootCats = rootResponse?.data || rootResponse?.$values || rootResponse || [];
        if (Array.isArray(rootCats) && rootCats.length > 0) {
          catsWithSubs = await Promise.all(
            rootCats.map(async (root) => {
              try {
                const subResponse = await api.get(`/api/categories/${root.id}/subcategories`);
                const subCats = subResponse?.data || subResponse?.$values || subResponse || [];
                return {
                  ...root,
                  subCategories: Array.isArray(subCats) ? subCats.filter((sub) => sub.isActive !== false) : [],
                };
              } catch {
                return { ...root, subCategories: [] };
              }
            })
          );
        }
      } catch (e) {
        // Fallback below
      }

      // API root/subcategories boş dönerse kategori ağacı servisini fallback kullan
      if (!catsWithSubs || catsWithSubs.length === 0) {
        const tree = await categoryServiceReal.getCategoryTree();
        catsWithSubs = (tree || []).filter((cat) => cat.isActive !== false);
      }

      setCategories(catsWithSubs);

      // Eğer mevcut seçili değer varsa o kategorinin parent'ını otomatik aç
      if (value) {
        const numericVal = Number(value);
        for (const parent of catsWithSubs) {
          const hasChild = parent.subCategories?.some((s) => Number(s.id) === numericVal);
          if (hasChild) {
            setExpandedParents(new Set([parent.id]));
            break;
          }
        }
      }
    } catch (err) {
      console.error("Failed to load categories:", err);
    } finally {
      setLoading(false);
    }
  };

  const toggleParent = (parentId) => {
    const newExpanded = new Set(expandedParents);
    if (newExpanded.has(parentId)) {
      newExpanded.delete(parentId);
    } else {
      newExpanded.add(parentId);
    }
    setExpandedParents(newExpanded);
  };

  const getSelectedCategoryInfo = () => {
    if (!value) return null;
    const numericVal = Number(value);

    for (const parent of categories) {
      if (Number(parent.id) === numericVal) {
        return { category: parent, breadcrumb: parent.name, isSub: false };
      }

      const sub = parent.subCategories?.find((s) => Number(s.id) === numericVal);
      if (sub) {
        return { category: sub, breadcrumb: `${parent.name} > ${sub.name}`, isSub: true };
      }
    }
    return null;
  };

  const selectedInfo = getSelectedCategoryInfo();

  const filteredCategories = categories.filter((cat) => {
    if (!searchTerm) return true;
    const term = searchTerm.toLowerCase();
    const parentMatches = (cat.name || "").toLowerCase().includes(term);
    const subMatches = cat.subCategories?.some((sub) =>
      (sub.name || "").toLowerCase().includes(term)
    );
    return parentMatches || subMatches;
  });

  return (
    <div className="category-selector">
      <div className="d-flex align-items-center justify-content-between mb-2">
        <label className="form-label fw-semibold mb-0">
          Hedef Kategori *
          {selectedInfo && (
            <span
              className={`badge ms-2 ${selectedInfo.isSub ? "bg-info" : "bg-primary"}`}
              style={{ fontWeight: "normal", fontSize: "0.75rem" }}
            >
              {selectedInfo.isSub ? "Alt Kategori" : "Ana Kategori"}
            </span>
          )}
        </label>
        {value && (
          <button
            type="button"
            className="btn btn-sm btn-link text-danger p-0 text-decoration-none"
            onClick={() => onChange({ target: { name, value: null } })}
            style={{ fontSize: "0.8rem" }}
          >
            <i className="fas fa-times me-1"></i>
            Seçimi Temizle
          </button>
        )}
      </div>

      {/* Arama Input */}
      <div className="input-group input-group-sm mb-2">
        <span className="input-group-text bg-white border-end-0">
          <i className="fas fa-search text-muted"></i>
        </span>
        <input
          type="text"
          className="form-control border-start-0"
          placeholder="Kategori veya alt kategori ara..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />
        {searchTerm && (
          <button
            type="button"
            className="btn btn-outline-secondary border-start-0"
            onClick={() => setSearchTerm("")}
          >
            <i className="fas fa-times"></i>
          </button>
        )}
      </div>

      {/* Kategori Listesi Ağacı */}
      <div
        className="border rounded p-2"
        style={{
          maxHeight: 280,
          overflowY: "auto",
          backgroundColor: "#f9fafb",
        }}
      >
        {loading ? (
          <div className="text-center py-3 text-muted">
            <i className="fas fa-spinner fa-spin me-2"></i>
            Kategoriler yükleniyor...
          </div>
        ) : filteredCategories.length === 0 ? (
          <div className="text-center py-3 text-muted">
            <i className="fas fa-folder-open me-2"></i>
            Kategori bulunamadı
          </div>
        ) : (
          filteredCategories.map((parent) => {
            const isParentSelected = Number(value) === Number(parent.id);
            const hasSubs = parent.subCategories?.length > 0;
            const isExpanded = expandedParents.has(parent.id) || Boolean(searchTerm);

            return (
              <div key={parent.id} className="mb-1">
                {/* Ana Kategori */}
                <div
                  className={`d-flex align-items-center p-2 rounded ${
                    isParentSelected
                      ? "bg-primary text-white shadow-sm"
                      : "bg-white hover-bg-light border"
                  }`}
                  style={{
                    cursor: "pointer",
                    transition: "all 0.15s ease",
                  }}
                  onClick={() => onChange({ target: { name, value: parent.id } })}
                >
                  {hasSubs ? (
                    <span
                      onClick={(e) => {
                        e.stopPropagation();
                        toggleParent(parent.id);
                      }}
                      className="p-1 me-1 text-center"
                      style={{ width: 22, cursor: "pointer" }}
                      title={isExpanded ? "Daralt" : "Genişlet"}
                    >
                      <i
                        className={`fas fa-chevron-${isExpanded ? "down" : "right"}`}
                        style={{
                          fontSize: "0.75rem",
                          color: isParentSelected ? "#fff" : "#6b7280",
                        }}
                      ></i>
                    </span>
                  ) : (
                    <span style={{ width: 22, display: "inline-block" }}></span>
                  )}

                  <i
                    className="fas fa-folder me-2"
                    style={{
                      fontSize: "0.875rem",
                      color: isParentSelected ? "#fff" : "#f59e0b",
                    }}
                  ></i>
                  <span style={{ fontWeight: 600, fontSize: "0.875rem" }}>
                    {parent.name}
                  </span>

                  {hasSubs && (
                    <span
                      className={`badge ms-auto ${
                        isParentSelected ? "bg-light text-primary" : "bg-secondary"
                      }`}
                      style={{ fontSize: "0.7rem" }}
                    >
                      {parent.subCategories.length} alt
                    </span>
                  )}
                </div>

                {/* Alt Kategoriler */}
                {isExpanded && hasSubs && (
                  <div style={{ paddingLeft: 24, marginTop: 4 }}>
                    {parent.subCategories.map((sub) => {
                      const isSubSelected = Number(value) === Number(sub.id);
                      return (
                        <div
                          key={sub.id}
                          className={`d-flex align-items-center p-2 rounded mb-1 ${
                            isSubSelected
                              ? "bg-primary text-white shadow-sm"
                              : "bg-white hover-bg-light border"
                          }`}
                          style={{
                            cursor: "pointer",
                            transition: "all 0.15s ease",
                          }}
                          onClick={() => onChange({ target: { name, value: sub.id } })}
                        >
                          <span
                            className="me-2"
                            style={{
                              color: isSubSelected ? "rgba(255,255,255,0.7)" : "#9ca3af",
                            }}
                          >
                            └─
                          </span>
                          <i
                            className="fas fa-tag me-2"
                            style={{
                              fontSize: "0.75rem",
                              color: isSubSelected ? "#fff" : "#3b82f6",
                            }}
                          ></i>
                          <span style={{ fontSize: "0.85rem" }}>{sub.name}</span>
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })
        )}
      </div>

      {/* Seçili Kategori Breadcrumb */}
      {selectedInfo && (
        <div
          className="alert alert-info mt-2 mb-0 py-2 px-3"
          style={{ fontSize: "0.85rem" }}
        >
          <div className="d-flex align-items-center justify-content-between">
            <div>
              <i className="fas fa-check-circle text-success me-2"></i>
              <strong>Seçili Kategori:</strong> {selectedInfo.breadcrumb}
            </div>
            <span className={`badge ${selectedInfo.isSub ? "bg-info" : "bg-primary"}`}>
              ID: {selectedInfo.category.id}
            </span>
          </div>
        </div>
      )}
    </div>
  );
}

CategorySelector.propTypes = {
  value: PropTypes.oneOfType([PropTypes.number, PropTypes.string]),
  onChange: PropTypes.func.isRequired,
  name: PropTypes.string,
};

