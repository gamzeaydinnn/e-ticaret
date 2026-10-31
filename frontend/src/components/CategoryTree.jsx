/**
 * CategoryTree.jsx
 *
 * Hiyerarşik kategori ağacı component'i (Mikro ERP grup kodları ve Aktif/Pasif yönetimi ile)
 */

import React, { useState } from "react";

const CategoryTreeNode = ({
  category,
  onEdit,
  onDelete,
  onToggleActive,
  onAddSubCategory,
  level = 0,
}) => {
  const [expanded, setExpanded] = useState(true);
  const children = category.children || category.subCategories || [];
  const hasChildren = children.length > 0;
  const mikroAna = category.mikroAnaGrupKod || category.MikroAnaGrupKod || "";
  const mikroAlt = category.mikroAltGrupKod || category.MikroAltGrupKod || "";

  return (
    <div className="category-tree-node">
      <div
        className="d-flex align-items-center py-2 px-3 rounded mb-1 position-relative"
        style={{
          marginLeft: level * 24,
          background:
            level === 0
              ? "linear-gradient(135deg, rgba(249, 115, 22, 0.08), rgba(251, 146, 60, 0.08))"
              : "rgba(245, 245, 245, 0.5)",
          border:
            level === 0
              ? "1px solid rgba(249, 115, 22, 0.2)"
              : "1px solid rgba(0,0,0,0.05)",
          opacity: category.isActive === false ? 0.65 : 1,
          transition: "all 0.2s ease",
        }}
      >
        {hasChildren ? (
          <button
            type="button"
            onClick={() => setExpanded(!expanded)}
            className="btn btn-sm p-0 me-2 border-0 bg-transparent"
            style={{ width: "20px", height: "20px" }}
            title={expanded ? "Daralt" : "Genişlet"}
          >
            <i
              className={`fas fa-chevron-${expanded ? "down" : "right"}`}
              style={{
                fontSize: "0.7rem",
                color: level === 0 ? "#f97316" : "#6b7280",
              }}
            ></i>
          </button>
        ) : (
          <div style={{ width: "28px" }}></div>
        )}

        <div
          className="d-flex align-items-center justify-content-center rounded me-2"
          style={{
            width: "28px",
            height: "28px",
            minWidth: "28px",
            background:
              level === 0
                ? "linear-gradient(135deg, #f97316, #fb923c)"
                : "linear-gradient(135deg, #3b82f6, #60a5fa)",
          }}
        >
          {category.imageUrl ? (
            <img
              src={category.imageUrl}
              alt={category.name}
              style={{
                width: "100%",
                height: "100%",
                objectFit: "cover",
                borderRadius: "4px",
              }}
              onError={(e) => {
                e.currentTarget.style.display = "none";
              }}
            />
          ) : (
            <i
              className={`fas ${level === 0 ? "fa-folder" : "fa-folder-open"}`}
              style={{ color: "white", fontSize: "0.75rem" }}
            ></i>
          )}
        </div>

        <div className="flex-grow-1 me-2">
          <div
            className="d-flex flex-wrap align-items-center"
            style={{ gap: "6px" }}
          >
            <span
              className="fw-semibold"
              style={{
                fontSize: level === 0 ? "0.9rem" : "0.85rem",
                color: level === 0 ? "#1e293b" : "#475569",
              }}
            >
              {category.name}
            </span>

            {(mikroAna || mikroAlt) && (
              <span
                className="badge bg-dark text-white"
                style={{ fontSize: "0.6rem", padding: "0.18em 0.5em" }}
                title="Mikro ERP Grup Kodu"
              >
                Mikro: {mikroAlt ? `${mikroAna} / ${mikroAlt}` : mikroAna}
              </span>
            )}

            <span
              className={`badge rounded-pill ${
                category.isActive ? "bg-success" : "bg-secondary"
              }`}
              style={{ fontSize: "0.6rem", padding: "0.15em 0.45em" }}
            >
              {category.isActive ? "Sitede Aktif" : "Sitede Gizli (Pasif)"}
            </span>

            {category.productCount > 0 && (
              <span
                className="badge bg-light text-dark"
                style={{
                  fontSize: "0.6rem",
                  padding: "0.15em 0.4em",
                  border: "1px solid rgba(0,0,0,0.1)",
                }}
              >
                <i className="fas fa-box me-1"></i>
                {category.productCount} ürün
              </span>
            )}

            {hasChildren && (
              <span
                className="badge bg-info text-white"
                style={{ fontSize: "0.6rem", padding: "0.15em 0.4em" }}
              >
                <i className="fas fa-sitemap me-1"></i>
                {children.length} alt
              </span>
            )}
          </div>

          {category.description && (
            <small
              className="text-muted d-block mt-1"
              style={{
                fontSize: "0.7rem",
                overflow: "hidden",
                textOverflow: "ellipsis",
                whiteSpace: "nowrap",
                maxWidth: "360px",
              }}
            >
              {category.description}
            </small>
          )}
        </div>

        <div className="d-flex flex-wrap gap-1">
          {level === 0 && onAddSubCategory && (
            <button
              type="button"
              className="btn btn-sm btn-success"
              style={{
                padding: "0.25rem 0.5rem",
                fontSize: "0.68rem",
                lineHeight: 1,
              }}
              onClick={() => onAddSubCategory(category)}
              title="Bu ana kategoriye yeni alt kategori ekle (Mikro'ya da kaydedilir)"
            >
              + Alt
            </button>
          )}
          {onToggleActive && (
            <button
              type="button"
              className={`btn btn-sm ${
                category.isActive
                  ? "btn-outline-warning"
                  : "btn-outline-success"
              }`}
              style={{
                padding: "0.25rem 0.5rem",
                fontSize: "0.68rem",
                lineHeight: 1,
              }}
              onClick={() => onToggleActive(category)}
              title={
                category.isActive
                  ? "Sitede gizle (Pasife al)"
                  : "Sitede göster (Aktife al)"
              }
            >
              <i
                className={`fas ${
                  category.isActive ? "fa-eye-slash" : "fa-eye"
                } me-1`}
              ></i>
              {category.isActive ? "Pasife Al" : "Aktif Yap"}
            </button>
          )}
          <button
            type="button"
            className="btn btn-sm btn-outline-primary"
            style={{
              padding: "0.25rem 0.5rem",
              fontSize: "0.7rem",
              lineHeight: 1,
            }}
            onClick={() => onEdit(category)}
            title="Düzenle"
          >
            <i className="fas fa-edit"></i>
          </button>
          <button
            type="button"
            className="btn btn-sm btn-outline-danger"
            style={{
              padding: "0.25rem 0.5rem",
              fontSize: "0.7rem",
              lineHeight: 1,
            }}
            onClick={() => onDelete(category.id)}
            title="Sil"
          >
            <i className="fas fa-trash"></i>
          </button>
        </div>
      </div>

      {hasChildren && expanded && (
        <div className="category-tree-children">
          {children.map((child) => (
            <CategoryTreeNode
              key={child.id}
              category={child}
              onEdit={onEdit}
              onDelete={onDelete}
              onToggleActive={onToggleActive}
              onAddSubCategory={onAddSubCategory}
              level={level + 1}
            />
          ))}
        </div>
      )}
    </div>
  );
};

export default function CategoryTree({
  categories,
  onEdit,
  onDelete,
  onToggleActive,
  onAddSubCategory,
}) {
  if (!categories || categories.length === 0) {
    return (
      <div className="text-center py-5">
        <i
          className="fas fa-sitemap fa-3x text-muted mb-3"
          style={{ opacity: 0.3 }}
        ></i>
        <h6 className="text-muted">Henüz kategori yok</h6>
        <p className="text-muted small">
          Yeni kategori eklemek için &quot;Ana kategori&quot; butonuna tıklayın
        </p>
      </div>
    );
  }

  return (
    <div className="category-tree">
      <div
        className="d-flex align-items-center justify-content-between mb-3 p-3 rounded"
        style={{
          background: "linear-gradient(135deg, #f8f9fa, #e9ecef)",
          border: "1px solid rgba(0,0,0,0.1)",
        }}
      >
        <div>
          <h6 className="mb-0 fw-bold" style={{ color: "#1e293b" }}>
            <i
              className="fas fa-sitemap me-2"
              style={{ color: "#f97316" }}
            ></i>
            Mikro ERP Kategori Hiyerarşisi
          </h6>
          <small className="text-muted" style={{ fontSize: "0.7rem" }}>
            Toplam {categories.length} ana kategori
          </small>
        </div>

        <div className="d-flex gap-2">
          <span className="badge bg-success" style={{ fontSize: "0.7rem" }}>
            <i className="fas fa-check-circle me-1"></i>
            Aktif / Pasif Yönetimi
          </span>
          <span className="badge bg-info" style={{ fontSize: "0.7rem" }}>
            <i className="fas fa-layer-group me-1"></i>
            Ağaç Görünümü
          </span>
        </div>
      </div>

      <div className="category-tree-container">
        {categories.map((category) => (
          <CategoryTreeNode
            key={category.id}
            category={category}
            onEdit={onEdit}
            onDelete={onDelete}
            onToggleActive={onToggleActive}
            onAddSubCategory={onAddSubCategory}
            level={0}
          />
        ))}
      </div>
    </div>
  );
}
