// Kategoriye göre ürün listeleme (mevcut mimariye uygun)
import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../services/api";
import { Helmet } from "react-helmet-async";
import ProductGrid from "../components/ProductGrid";
import { shouldUseMockData, debugLog } from "../config/apiConfig";
import mockDataStore from "../services/mockDataStore";

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

  useEffect(() => {
    let mounted = true;
    setLoading(true);
    setError("");

    // Mock modda mockDataStore'dan kategori bul
    if (shouldUseMockData()) {
      const allCategories = mockDataStore.getCategories();
      const foundCat = allCategories.find((c) => {
        const catSlug = c.slug || createSlug(c.name);
        return catSlug === slug;
      });
      
      if (foundCat) {
        setCategory(foundCat);
        setLoading(false);
        return;
      }
    }

    // API'den dene
    api
      .get(`/api/categories/${encodeURIComponent(slug)}`)
      .then((cat) => {
        if (!mounted) return;
        setCategory(cat);
      })
      .catch((e) => {
        if (!mounted) return;
        debugLog("Kategori API başarısız", { slug, error: e?.message });
        setError(e?.message || "Kategori bilgisi yüklenemedi.");
      })
      .finally(() => {
        if (!mounted) return;
        setLoading(false);
      });

    return () => {
      mounted = false;
    };
  }, [slug]);

  // Subscribe to category changes
  useEffect(() => {
    if (shouldUseMockData()) {
      const unsub = mockDataStore.subscribe("categories", () => {
        const allCategories = mockDataStore.getCategories();
        const foundCat = allCategories.find((c) => {
          const catSlug = c.slug || createSlug(c.name);
          return catSlug === slug;
        });
        if (foundCat) {
          setCategory(foundCat);
        }
      });
      return () => unsub && unsub();
    }
  }, [slug]);

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
