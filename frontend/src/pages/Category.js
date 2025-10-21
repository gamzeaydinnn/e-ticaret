// Kategoriye göre ürün listeleme (mevcut mimariye uygun)
import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../services/api";
import ProductGrid from "../components/ProductGrid";

export default function Category() {
  const { slug } = useParams();
  const [category, setCategory] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let mounted = true;
    setLoading(true);
    setError("");

    api
      .get(`/api/categories/${encodeURIComponent(slug)}`)
      .then((cat) => {
        if (!mounted) return;
        setCategory(cat);
      })
      .catch((e) => {
        if (!mounted) return;
        setError(e.message || "Kategori bilgisi yüklenemedi.");
      })
      .finally(() => {
        if (!mounted) return;
        setLoading(false);
      });

    return () => {
      mounted = false;
    };
  }, [slug]);

  return (
    <div className="container-fluid px-4 py-4">
      <div className="d-flex align-items-center justify-content-between mb-4">
        <div>
          <h1 className="h3 fw-bold mb-1" style={{ color: "#2d3748" }}>
            <i className="fas fa-layer-group me-2" style={{ color: "#f57c00" }}></i>
            {category?.name || "Kategori"}
          </h1>
          {category?.description ? (
            <div className="text-muted" style={{ maxWidth: 720 }}>{category.description}</div>
          ) : null}
        </div>
      </div>

      {error && (
        <div className="alert alert-danger" role="alert">{error}</div>
      )}

      {/* Ürün listesi */}
      {category && (
        <ProductGrid categoryId={category.id} />
      )}

      {!category && !loading && !error && (
        <div className="text-muted">Kategori bulunamadı.</div>
      )}
    </div>
  );
}
