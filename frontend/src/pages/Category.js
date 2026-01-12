// Kategoriye göre ürün listeleme (mevcut mimariye uygun)
import React, { useEffect, useState, useCallback } from "react";
import { useParams } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import ProductGrid from "../components/ProductGrid";
import categoryServiceReal from "../services/categoryServiceReal";

// Slug oluşturma fonksiyonu
const createSlug = (name) => {
  return name
    .toLowerCase()
    .replace(/ç/g, "c")
    .replace(/ğ/g, "g")
    .replace(/ı/g, "i")
    .replace(/ö/g, "o")
    .replace(/ş/g, "s")
    .replace(/ü/g, "u")
    .replace(/[^a-z0-9\s-]/g, "")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-")
    .trim();
};

export default function Category() {
  const { slug } = useParams();
  const [category, setCategory] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const loadCategory = useCallback(async () => {
    setLoading(true);
    setError("");

    // Gerçek Backend API'den kategori al
    try {
      // Önce slug ile dene
      const cat = await categoryServiceReal.getBySlug(slug);
      if (cat) {
        setCategory(cat);
        setLoading(false);
        return;
      }

      // Slug bulunamazsa tüm kategorilerden ara
      const allCategories = await categoryServiceReal.getActive();
      const foundCat = allCategories.find((c) => {
        const catSlug = c.slug || createSlug(c.name);
        return catSlug === slug;
      });

      if (foundCat) {
        setCategory(foundCat);
      } else {
        setError("Kategori bulunamadı.");
      }
    } catch (err) {
      console.error("Kategoriler yüklenemedi:", err);
      setError(err?.message || "Kategori bilgisi yüklenemedi.");
    } finally {
      setLoading(false);
    }
  }, [slug]);

  useEffect(() => {
    loadCategory();
  }, [loadCategory]);

  // Subscribe to category changes
  useEffect(() => {
    const unsub = categoryServiceReal.subscribe(() => {
      loadCategory();
    });
    return () => unsub && unsub();
  }, [loadCategory]);

  return (
    <div className="container-fluid px-4 py-4">
      <Helmet>
        {(() => {
          const siteUrl =
            process.env.REACT_APP_SITE_URL ||
            (typeof window !== "undefined" ? window.location.origin : "");
          const ogImage =
            category && category.image
              ? `${siteUrl}${category.image}`
              : `${siteUrl}/images/og-default.jpg`;
          const canonical = `${siteUrl}${
            typeof window !== "undefined" ? window.location.pathname : ""
          }`;
          return (
            <>
              <title>
                {category?.name
                  ? `${category.name} — Doğadan Sofranza`
                  : "Kategori — Doğadan Sofranza"}
              </title>
              <meta
                name="description"
                content={
                  category?.description || `Kategori: ${category?.name || ""}`
                }
              />
              <meta
                property="og:title"
                content={category?.name || "Kategori"}
              />
              <meta
                property="og:description"
                content={category?.description || ""}
              />
              <meta property="og:image" content={ogImage} />
              <link rel="canonical" href={canonical} />
            </>
          );
        })()}
      </Helmet>
      <div className="d-flex align-items-center justify-content-between mb-4">
        <div>
          <h1 className="h3 fw-bold mb-1" style={{ color: "#2d3748" }}>
            <i
              className="fas fa-layer-group me-2"
              style={{ color: "#f57c00" }}
            ></i>
            {category?.name || "Kategori"}
          </h1>
          {category?.description ? (
            <div className="text-muted" style={{ maxWidth: 720 }}>
              {category.description}
            </div>
          ) : null}
        </div>
      </div>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      {/* Ürün listesi */}
      {category && <ProductGrid categoryId={category.id} />}

      {!category && !loading && !error && (
        <div className="text-muted">Kategori bulunamadı.</div>
      )}
    </div>
  );
}
