import React, { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import categoryServiceReal from "../services/categoryServiceReal";
import CategoryDiscoverSection from "../components/CategoryDiscoverSection";

/**
 * /kategoriler — Google sitelink ve footer için gerçek kategori listesi.
 */
export default function CategoriesPage() {
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const tree = await categoryServiceReal.getCategoryTree();
        const list = Array.isArray(tree) ? tree : [];
        if (mounted) setCategories(list);
      } catch (err) {
        console.error("[CategoriesPage] yüklenemedi:", err);
        if (mounted) setCategories([]);
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => {
      mounted = false;
    };
  }, []);

  return (
    <div style={{ maxWidth: "1200px", margin: "0 auto", padding: "16px" }}>
      <Helmet>
        <title>Kategoriler | Gölköy Gurme</title>
        <meta
          name="description"
          content="Gölköy Gurme ürün kategorileri: meyve-sebze, süt ürünleri, et, temel gıda ve daha fazlası."
        />
      </Helmet>

      {/* Üst Navigasyon & Geri Dön Butonu (Mobil ve Masaüstü Uyumlu) */}
      <div className="categories-page-top-bar d-flex align-items-center justify-content-between flex-wrap gap-2 mb-3 pb-2 border-bottom">
        <nav aria-label="breadcrumb">
          <ol className="breadcrumb mb-0" style={{ fontSize: "0.85rem" }}>
            <li className="breadcrumb-item">
              <Link to="/" style={{ color: "#f57c00", textDecoration: "none" }}>
                <i className="fas fa-home me-1"></i>
                Ana Sayfa
              </Link>
            </li>
            <li className="breadcrumb-item active" aria-current="page">
              Kategoriler
            </li>
          </ol>
        </nav>

        <Link
          to="/"
          className="category-back-btn"
          title="Ana Sayfaya Dön"
          aria-label="Ana Sayfaya Dön"
        >
          <i className="fas fa-arrow-left category-back-icon"></i>
          <span>Ana Sayfaya Dön</span>
        </Link>
      </div>

      <CategoryDiscoverSection
        categories={categories}
        loading={loading}
        title="Kategoriler"
      />
    </div>
  );
}
