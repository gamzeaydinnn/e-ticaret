// Kategoriye göre ürün listeleme (mevcut mimariye uygun)
import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../services/api";
import { Helmet } from "react-helmet-async";
import ProductGrid from "../components/ProductGrid";
import { shouldUseMockData, debugLog } from "../config/apiConfig";

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
        // Mock moda düş: bilinen slug -> kategori id eşlemesi
        if (shouldUseMockData()) {
          debugLog("Kategori API başarısız, mock eşleşmeye düşülüyor", {
            slug,
            error: e?.message,
          });
          const map = {
            "meyve-sebze": {
              id: 1,
              name: "Meyve & Sebze",
              description: "Taze meyve sebze ürünleri",
            },
            "et-tavuk-balik": {
              id: 2,
              name: "Et & Tavuk & Balık",
              description: "Kırmızı et, tavuk ve balık",
            },
            "sut-urunleri": {
              id: 3,
              name: "Süt Ürünleri",
              description: "Süt, peynir, yoğurt",
            },
            "temel-gida": {
              id: 4,
              name: "Temel Gıda",
              description: "Bakliyat, temel ihtiyaç",
            },
            icecekler: {
              id: 5,
              name: "İçecekler",
              description: "Soğuk ve sıcak içecekler",
            },
            atistirmalik: {
              id: 6,
              name: "Atıştırmalık",
              description: "Cips, kraker ve atıştırmalıklar",
            },
            temizlik: {
              id: 7,
              name: "Temizlik",
              description: "Ev temizlik ürünleri",
            },
          };
          const mockCat = map[slug];
          if (mockCat) {
            setCategory(mockCat);
            setError("");
            return;
          }
        }

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
