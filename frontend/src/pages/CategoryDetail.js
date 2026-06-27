/**
 * CategoryDetail.js
 *
 * Kategori detay sayfası - Alt kategoriler ve ürünler
 * Breadcrumb navigasyon ile kategori hiyerarşisini gösterir
 *
 * @author E-Ticaret Projesi - Hierarchical Category System
 * @version 1.0.0
 */

import { useEffect, useState } from "react";
import { Link, useParams, useNavigate } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import categoryService from "../services/categoryService";
import { ProductService } from "../services/productService";
import ProductCard from "./components/ProductCard";
import CategoryTile from "./components/CategoryTile";

export default function CategoryDetail() {
  const { slug } = useParams();
  const navigate = useNavigate();

  const [category, setCategory] = useState(null);
  const [subCategories, setSubCategories] = useState([]);
  const [products, setProducts] = useState([]);
  const [breadcrumb, setBreadcrumb] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [favorites, setFavorites] = useState(() => {
    try {
      return JSON.parse(localStorage.getItem("favorites")) || [];
    } catch {
      return [];
    }
  });

  useEffect(() => {
    loadCategoryData();
  }, [slug]);

  const loadCategoryData = async () => {
    try {
      setLoading(true);
      setError(null);

      // Kategori bilgisini getir
      const cat = await categoryService.getBySlug(slug);

      if (!cat) {
        setError("Kategori bulunamadı");
        setLoading(false);
        return;
      }

      setCategory(cat);

      // Alt kategoriler
      if (cat.id) {
        try {
          const subs = await categoryService.getSubCategories(cat.id);
          // Sadece aktif olanları göster
          setSubCategories((subs || []).filter((s) => s.isActive !== false));
        } catch (err) {
          console.error("Alt kategoriler yüklenemedi:", err);
          setSubCategories([]);
        }

        // Ürünler - kategori ID'sine göre filtrele
        try {
          const allProducts = await ProductService.list();
          const categoryProducts = allProducts.filter(
            (p) => p.categoryId === cat.id && p.isActive !== false,
          );
          setProducts(categoryProducts);
        } catch (err) {
          console.error("Ürünler yüklenemedi:", err);
          setProducts([]);
        }

        // Breadcrumb - kategori yolu
        try {
          const path = await categoryService.getCategoryPath(cat.id);
          setBreadcrumb(Array.isArray(path) ? path : []);
        } catch (err) {
          console.error("Breadcrumb yüklenemedi:", err);
          setBreadcrumb([]);
        }
      }
    } catch (err) {
      console.error("Kategori yükleme hatası:", err);
      setError("Kategori bilgileri yüklenirken bir hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  const handleToggleFavorite = (productId) => {
    setFavorites((prev) => {
      const updated = prev.includes(productId)
        ? prev.filter((id) => id !== productId)
        : [...prev, productId];
      localStorage.setItem("favorites", JSON.stringify(updated));
      return updated;
    });
  };

  const handleAddToCart = (productId) => {
    const product = products.find((p) => p.id === productId);
    if (product) alert(`${product.name} sepete eklendi!`);
  };

  if (loading) {
    return (
      <div
        style={{
          maxWidth: 1200,
          margin: "0 auto",
          padding: 16,
          textAlign: "center",
          paddingTop: 60,
        }}
      >
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
        <p className="text-muted mt-3">Kategori yükleniyor...</p>
      </div>
    );
  }

  if (error || !category) {
    return (
      <div style={{ maxWidth: 1200, margin: "0 auto", padding: 16 }}>
        <div
          className="alert alert-warning d-flex align-items-center"
          role="alert"
        >
          <i className="fas fa-exclamation-triangle me-3 fa-2x"></i>
          <div>
            <h5 className="alert-heading mb-1">Kategori Bulunamadı</h5>
            <p className="mb-2">{error || "Bu kategori mevcut değil"}</p>
            <Link to="/" className="btn btn-primary btn-sm">
              <i className="fas fa-home me-2"></i>
              Ana Sayfaya Dön
            </Link>
          </div>
        </div>
      </div>
    );
  }

  const siteUrl =
    process.env.REACT_APP_SITE_URL ||
    (typeof window !== "undefined" ? window.location.origin : "");

  return (
    <div style={{ maxWidth: 1200, margin: "0 auto", padding: 16 }}>
      <Helmet>
        <title>{category.name} — Doğadan Sofranza</title>
        <meta
          name="description"
          content={
            category.description ||
            `${category.name} kategorisindeki tüm ürünleri inceleyin`
          }
        />
        <meta
          property="og:title"
          content={`${category.name} — Doğadan Sofranza`}
        />
        <meta
          property="og:description"
          content={
            category.description ||
            `${category.name} kategorisindeki tüm ürünleri inceleyin`
          }
        />
        {category.imageUrl && (
          <meta property="og:image" content={category.imageUrl} />
        )}
        <link rel="canonical" href={`${siteUrl}/category/${category.slug}`} />
      </Helmet>

      {/* Breadcrumb Navigation */}
      {breadcrumb.length > 0 && (
        <nav aria-label="breadcrumb" className="mb-3">
          <ol
            className="breadcrumb mb-0 p-3 rounded"
            style={{
              background: "linear-gradient(135deg, #f8f9fa, #e9ecef)",
              border: "1px solid rgba(0,0,0,0.1)",
            }}
          >
            <li className="breadcrumb-item">
              <Link
                to="/"
                className="text-decoration-none"
                style={{ color: "#f97316", fontWeight: 500 }}
              >
                <i className="fas fa-home me-1"></i>
                Ana Sayfa
              </Link>
            </li>
            {breadcrumb.map((cat, index) => (
              <li
                key={cat.id}
                className={`breadcrumb-item ${
                  index === breadcrumb.length - 1 ? "active" : ""
                }`}
                aria-current={
                  index === breadcrumb.length - 1 ? "page" : undefined
                }
              >
                {index === breadcrumb.length - 1 ? (
                  <span style={{ fontWeight: 600 }}>{cat.name}</span>
                ) : (
                  <Link
                    to={`/category/${cat.slug}`}
                    className="text-decoration-none"
                    style={{ color: "#6b7280" }}
                  >
                    {cat.name}
                  </Link>
                )}
              </li>
            ))}
          </ol>
        </nav>
      )}

      {/* Category Header */}
      <section className="mb-4">
        <div
          className="p-4 rounded"
          style={{
            background: "linear-gradient(135deg, #38bdf8 0%, #6366f1 100%)",
            color: "white",
          }}
        >
          <div className="d-flex align-items-center gap-3">
            {category.imageUrl && (
              <div
                className="rounded overflow-hidden"
                style={{
                  width: 80,
                  height: 80,
                  minWidth: 80,
                  boxShadow: "0 4px 12px rgba(0,0,0,0.2)",
                }}
              >
                <img
                  src={category.imageUrl}
                  alt={category.name}
                  style={{
                    width: "100%",
                    height: "100%",
                    objectFit: "cover",
                  }}
                  onError={(e) => {
                    e.target.src = "/images/placeholder.png";
                  }}
                />
              </div>
            )}

            <div className="flex-grow-1">
              <h1
                style={{
                  fontSize: "1.75rem",
                  fontWeight: "bold",
                  marginBottom: 8,
                }}
              >
                <i className="fas fa-layer-group me-2"></i>
                {category.name}
              </h1>
              {category.description && (
                <p style={{ opacity: 0.95, marginBottom: 0 }}>
                  {category.description}
                </p>
              )}

              {/* Stats */}
              <div className="d-flex gap-3 mt-2">
                {subCategories.length > 0 && (
                  <span
                    className="badge bg-white text-dark"
                    style={{ fontSize: "0.85rem", padding: "0.4em 0.8em" }}
                  >
                    <i className="fas fa-sitemap me-1"></i>
                    {subCategories.length} Alt Kategori
                  </span>
                )}
                {products.length > 0 && (
                  <span
                    className="badge bg-white text-dark"
                    style={{ fontSize: "0.85rem", padding: "0.4em 0.8em" }}
                  >
                    <i className="fas fa-box me-1"></i>
                    {products.length} Ürün
                  </span>
                )}
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Alt Kategoriler */}
      {subCategories.length > 0 && (
        <section className="mb-4">
          <h2
            style={{
              fontSize: "1.25rem",
              fontWeight: 600,
              marginBottom: 12,
            }}
          >
            <i className="fas fa-sitemap me-2" style={{ color: "#3b82f6" }}></i>
            Alt Kategoriler
          </h2>
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(140px, 1fr))",
              gap: 12,
            }}
          >
            {subCategories.map((sub) => (
              <CategoryTile key={sub.id} category={sub} />
            ))}
          </div>
        </section>
      )}

      {/* Ürünler */}
      <section>
        <h2
          style={{
            fontSize: "1.25rem",
            fontWeight: 600,
            marginBottom: 12,
          }}
        >
          <i className="fas fa-box-open me-2" style={{ color: "#10b981" }}></i>
          {subCategories.length > 0 ? "Bu Kategorideki Ürünler" : "Ürünler"}
        </h2>

        {products.length === 0 ? (
          <div
            className="text-center py-5 rounded"
            style={{
              background: "rgba(0,0,0,0.02)",
              border: "2px dashed rgba(0,0,0,0.1)",
            }}
          >
            <i
              className="fas fa-box-open fa-3x text-muted mb-3"
              style={{ opacity: 0.3 }}
            ></i>
            <h5 className="text-muted mb-2">Bu kategoride henüz ürün yok</h5>
            <p className="text-muted">
              {subCategories.length > 0
                ? "Alt kategorilere göz atabilirsiniz"
                : "Başka kategorilere göz atın"}
            </p>
            <Link to="/" className="btn btn-primary mt-2">
              <i className="fas fa-arrow-left me-2"></i>
              Ana Sayfaya Dön
            </Link>
          </div>
        ) : (
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(160px, 1fr))",
              gap: 12,
            }}
          >
            {products.map((p) => (
              <ProductCard
                key={p.id}
                product={p}
                onToggleFavorite={handleToggleFavorite}
                isFavorite={favorites.includes(p.id)}
                onAddToCart={handleAddToCart}
              />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
