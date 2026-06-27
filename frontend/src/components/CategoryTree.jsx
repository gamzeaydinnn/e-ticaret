/**
 * CategoryTree.jsx
 *
 * Hiyerarşik kategori ağacı component'i
 * Admin panelinde kategorileri tree yapısında gösterir
 *
 * @author E-Ticaret Projesi - Hierarchical Category System
 * @version 1.0.0
 */

import React, { useState } from "react";

/**
 * Tekil kategori node'u (recursive)
 */
const CategoryTreeNode = ({ category, onEdit, onDelete, level = 0 }) => {
  const [expanded, setExpanded] = useState(true);
  const hasChildren = category.children?.length > 0;

  return (
    <div className="category-tree-node">
      {/* Node Container */}
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
          transition: "all 0.2s ease",
        }}
        onMouseEnter={(e) => {
          e.currentTarget.style.background =
            level === 0
              ? "linear-gradient(135deg, rgba(249, 115, 22, 0.15), rgba(251, 146, 60, 0.15))"
              : "rgba(240, 240, 240, 0.8)";
          e.currentTarget.style.transform = "translateX(2px)";
        }}
        onMouseLeave={(e) => {
          e.currentTarget.style.background =
            level === 0
              ? "linear-gradient(135deg, rgba(249, 115, 22, 0.08), rgba(251, 146, 60, 0.08))"
              : "rgba(245, 245, 245, 0.5)";
          e.currentTarget.style.transform = "translateX(0)";
        }}
      >
        {/* Expand/Collapse Button */}
        {hasChildren && (
          <button
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
                transition: "transform 0.2s ease",
              }}
            ></i>
          </button>
        )}

        {/* Spacer for alignment when no children */}
        {!hasChildren && <div style={{ width: "28px" }}></div>}

        {/* Category Icon */}
        <div
          className="d-flex align-items-center justify-content-center rounded me-2"
          style={{
            width: "28px",
            height: "28px",
            minWidth: "28px",
            background:
              level === 0
                ? "linear-gradient(135deg, #f97316, #fb923c)"
                : level === 1
                  ? "linear-gradient(135deg, #3b82f6, #60a5fa)"
                  : "linear-gradient(135deg, #8b5cf6, #a78bfa)",
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

        {/* Category Name */}
        <div className="flex-grow-1 me-2">
          <div className="d-flex align-items-center" style={{ gap: "8px" }}>
            <span
              className="fw-semibold"
              style={{
                fontSize: level === 0 ? "0.9rem" : "0.85rem",
                color: level === 0 ? "#1e293b" : "#475569",
              }}
            >
              {category.name}
            </span>

            {/* Active/Inactive Badge */}
            <span
              className={`badge rounded-pill ${
                category.isActive ? "bg-success" : "bg-secondary"
              }`}
              style={{
                fontSize: "0.6rem",
                padding: "0.15em 0.4em",
              }}
            >
              {category.isActive ? "Aktif" : "Pasif"}
            </span>

            {/* Product Count */}
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

            {/* Children Count */}
            {hasChildren && (
              <span
                className="badge bg-info text-white"
                style={{
                  fontSize: "0.6rem",
                  padding: "0.15em 0.4em",
                }}
              >
                <i className="fas fa-sitemap me-1"></i>
                {category.children.length} alt
              </span>
            )}
          </div>

          {/* Description */}
          {category.description && (
            <small
              className="text-muted d-block mt-1"
              style={{
                fontSize: "0.7rem",
                overflow: "hidden",
                textOverflow: "ellipsis",
                whiteSpace: "nowrap",
                maxWidth: "300px",
              }}
            >
              {category.description}
            </small>
          )}
        </div>

        {/* Action Buttons */}
        <div className="d-flex gap-1">
          <button
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

      {/* Children (Recursive) */}
      {hasChildren && expanded && (
        <div className="category-tree-children">
          {category.children.map((child) => (
            <CategoryTreeNode
              key={child.id}
              category={child}
              onEdit={onEdit}
              onDelete={onDelete}
              level={level + 1}
            />
          ))}
        </div>
      )}
    </div>
  );
};

/**
 * Ana CategoryTree Component
 */
export default function CategoryTree({ categories, onEdit, onDelete }) {
  if (!categories || categories.length === 0) {
    return (
      <div className="text-center py-5">
        <i
          className="fas fa-sitemap fa-3x text-muted mb-3"
          style={{ opacity: 0.3 }}
        ></i>
        <h6 className="text-muted">Henüz kategori yok</h6>
        <p className="text-muted small">
          Yeni kategori eklemek için "Yeni" butonuna tıklayın
        </p>
      </div>
    );
  }

  return (
    <div className="category-tree">
      {/* Header */}
      <div
        className="d-flex align-items-center justify-content-between mb-3 p-3 rounded"
        style={{
          background: "linear-gradient(135deg, #f8f9fa, #e9ecef)",
          border: "1px solid rgba(0,0,0,0.1)",
        }}
      >
        <div>
          <h6 className="mb-0 fw-bold" style={{ color: "#1e293b" }}>
            <i className="fas fa-sitemap me-2" style={{ color: "#f97316" }}></i>
            Kategori Hiyerarşisi
          </h6>
          <small className="text-muted" style={{ fontSize: "0.7rem" }}>
            Toplam {categories.length} ana kategori
          </small>
        </div>

        <div className="d-flex gap-2">
          <span className="badge bg-success" style={{ fontSize: "0.7rem" }}>
            <i className="fas fa-check-circle me-1"></i>
            Aktif Kategoriler
          </span>
          <span className="badge bg-info" style={{ fontSize: "0.7rem" }}>
            <i className="fas fa-layer-group me-1"></i>
            Ağaç Görünümü
          </span>
        </div>
      </div>

      {/* Tree Nodes */}
      <div className="category-tree-container">
        {categories.map((category) => (
          <CategoryTreeNode
            key={category.id}
            category={category}
            onEdit={onEdit}
            onDelete={onDelete}
            level={0}
          />
        ))}
      </div>

      {/* Legend */}
      <div
        className="mt-3 p-2 rounded"
        style={{
          background: "rgba(249, 115, 22, 0.05)",
          border: "1px dashed rgba(249, 115, 22, 0.3)",
        }}
      >
        <small
          className="text-muted d-flex align-items-center gap-2"
          style={{ fontSize: "0.65rem" }}
        >
          <i className="fas fa-info-circle" style={{ color: "#f97316" }}></i>
          <span>
            <strong>İpucu:</strong> Kategorileri genişletmek için ok simgesine
            tıklayın. Alt kategorisi olan kategoriler silinemez.
          </span>
        </small>
      </div>
    </div>
  );
}
