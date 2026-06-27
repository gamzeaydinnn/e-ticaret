// Kategoriye göre ürün listeleme (mevcut mimariye uygun)
import React, { useEffect, useState, useCallback, useMemo } from "react";
import { useParams, useSearchParams, Link } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import ProductGrid from "../components/ProductGrid";
import CategoryTile from "./components/CategoryTile";
import categoryServiceReal, {
  matchesCategorySlug,
  normalizeCategorySlug,
} from "../services/categoryServiceReal";
import { sanitizeUrlParam } from "../utils/securityHelpers";

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
  const { slug: rawSlug } = useParams();
  const [searchParams, setSearchParams] = useSearchParams();
  // GÜVENLİK: URL parametresini sanitize et
  const slug = useMemo(
    () => normalizeCategorySlug(sanitizeUrlParam(rawSlug)),
    [rawSlug],
  );
  const currentPage = Math.max(
    1,
    parseInt(searchParams.get("page") || "1", 10) || 1,
  );
  const pageSize = Math.max(
    1,
    parseInt(searchParams.get("size") || "25", 10) || 25,
  );

  const [category, setCategory] = useState(null);
  const [subCategories, setSubCategories] = useState([]);
  const [breadcrumb, setBreadcrumb] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const loadCategory = useCallback(async () => {
    // GÜVENLİK: Boş veya geçersiz slug kontrolü
    if (!slug) {
      setError("Geçersiz kategori.");
      setLoading(false);
      return;
    }

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

      // Önce ID ile eşleşme dene (slug sayısal ise)
      const numericSlug = parseInt(slug, 10);
      if (!isNaN(numericSlug)) {
        const foundById = allCategories.find((c) => c.id === numericSlug);
        if (foundById) {
          setCategory(foundById);
          setLoading(false);
          return;
        }
      }

      // Sonra slug ile eşleşme dene
      const foundCat = allCategories.find((c) => {
        const catSlug = c.slug || createSlug(c.name);
        return matchesCategorySlug(catSlug, slug);
      });

      if (foundCat) {
        setCategory(foundCat);

        // ✨ YENİ: Alt kategorileri yükle
        if (foundCat.id) {
          try {
            const subs = await categoryServiceReal.getSubCategories(
              foundCat.id,
            );
            setSubCategories(subs.filter((c) => c.isActive !== false));
          } catch (err) {
            console.error("Alt kategoriler yüklenemedi:", err);
            setSubCategories([]);
          }

          // ✨ YENİ: Breadcrumb yolunu yükle
          try {
            const path = await categoryServiceReal.getCategoryPath(foundCat.id);
            setBreadcrumb(path || []);
          } catch (err) {
            console.error("Kategori yolu yüklenemedi:", err);
            setBreadcrumb([]);
          }
        }
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

  const handlePaginationChange = useCallback(
    ({ page, pageSize: nextPageSize }) => {
      const params = new URLSearchParams(searchParams);

      if (page > 1) {
        params.set("page", String(page));
      } else {
        params.delete("page");
      }

      if (nextPageSize && nextPageSize !== 25) {
        params.set("size", String(nextPageSize));
      } else {
        params.delete("size");
      }

      setSearchParams(params, { replace: false });
    },
    [searchParams, setSearchParams],
  );

  // Subscribe to category changes
  useEffect(() => {
    const unsub = categoryServiceReal.subscribe(() => {
      loadCategory();
    });
    return () => unsub && unsub();
  }, [loadCategory]);

  return (
    <div className="py-4">
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
                  ? `${category.name} — Gölköy Gurme`
                  : "Kategori — Gölköy Gurme"}
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

      {/* Başlık - ProductGrid ile aynı hizada */}
      <div style={{ maxWidth: "1400px", margin: "0 auto", padding: "0 60px" }}>
        {/* ✨ YENİ: Breadcrumb navigasyon */}
        {breadcrumb.length > 0 && (
          <nav className="mb-3" aria-label="breadcrumb">
            <ol className="breadcrumb" style={{ fontSize: "0.9rem" }}>
              <li className="breadcrumb-item">
                <Link
                  to="/"
                  style={{ color: "#f57c00", textDecoration: "none" }}
                >
                  <i className="fas fa-home me-1"></i>
                  Ana Sayfa
                </Link>
              </li>
              {breadcrumb.map((cat, index) => (
                <li
                  key={cat.id}
                  className={`breadcrumb-item ${index === breadcrumb.length - 1 ? "active" : ""}`}
                  aria-current={
                    index === breadcrumb.length - 1 ? "page" : undefined
                  }
                >
                  {index === breadcrumb.length - 1 ? (
                    cat.name
                  ) : (
                    <Link
                      to={`/kategoriler/${cat.slug || createSlug(cat.name)}`}
                      style={{ color: "#f57c00", textDecoration: "none" }}
                    >
                      {cat.name}
                    </Link>
                  )}
                </li>
              ))}
            </ol>
          </nav>
        )}

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

        {/* ✨ YENİ: Alt Kategoriler Gösterimi */}
        {subCategories.length > 0 && (
          <section className="mb-4">
            <h2 className="h5 fw-semibold mb-3" style={{ color: "#2d3748" }}>
              <i
                className="fas fa-sitemap me-2"
                style={{ color: "#10b981" }}
              ></i>
              Alt Kategoriler
            </h2>
            <div
              style={{
                display: "grid",
                gridTemplateColumns: "repeat(auto-fill, minmax(140px, 1fr))",
                gap: 12,
              }}
            >
              {subCategories.map((subCat) => (
                <CategoryTile key={subCat.id} category={subCat} />
              ))}
            </div>
          </section>
        )}
      </div>

      {/* Ürün listesi — grid modda (satır-sütun) */}
      {category && (
        <ProductGrid
          categoryId={category.id}
          showTitle={false}
          displayMode="grid"
          initialPage={currentPage}
          initialPageSize={pageSize}
          onPaginationChange={handlePaginationChange}
        />
      )}

      {!category && !loading && !error && (
        <div className="text-muted">Kategori bulunamadı.</div>
      )}
    </div>
  );
}
