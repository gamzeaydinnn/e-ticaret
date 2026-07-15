import React, { useEffect, useMemo, useState, useCallback } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import ProductGrid from "../components/ProductGrid";
import { ProductService } from "../services/productService";
import "./DiscountedProductsPage.css";

const PAGE_SIZE = 24;

export default function DiscountedProductsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const currentPage = Math.max(
    1,
    parseInt(searchParams.get("page") || "1", 10) || 1,
  );

  useEffect(() => {
    let mounted = true;

    const load = async () => {
      setLoading(true);
      setError("");
      try {
        const items = await ProductService.getDiscounted();
        if (!mounted) return;
        setProducts(Array.isArray(items) ? items : []);
      } catch (err) {
        if (!mounted) return;
        console.error("[DiscountedProducts]", err);
        setError("İndirimli ürünler yüklenemedi.");
        setProducts([]);
      } finally {
        if (mounted) setLoading(false);
      }
    };

    load();
    return () => {
      mounted = false;
    };
  }, []);

  const totalPages = Math.max(1, Math.ceil(products.length / PAGE_SIZE));
  const safePage = Math.min(currentPage, totalPages);

  const pageProducts = useMemo(() => {
    const start = (safePage - 1) * PAGE_SIZE;
    return products.slice(start, start + PAGE_SIZE);
  }, [products, safePage]);

  const handlePaginationChange = useCallback(
    ({ page }) => {
      const params = new URLSearchParams(searchParams);
      if (page > 1) params.set("page", String(page));
      else params.delete("page");
      setSearchParams(params, { replace: false });
      window.scrollTo({ top: 0, behavior: "smooth" });
    },
    [searchParams, setSearchParams],
  );

  const maxDiscount = useMemo(() => {
    if (!products.length) return 0;
    return products.reduce((max, p) => {
      const pct =
        p.discountPercentage ||
        Math.round(
          (1 -
            (parseFloat(p.specialPrice) || 0) / (parseFloat(p.price) || 1)) *
            100,
        );
      return Math.max(max, pct || 0);
    }, 0);
  }, [products]);

  return (
    <div className="discounted-page">
      <Helmet>
        <title>İndirimli Ürünler — Gölköy Gurme</title>
        <meta
          name="description"
          content="Gölköy Gurme'de indirimli ürünler. Özel fiyatlı fırsatları kaçırmayın."
        />
      </Helmet>

      <div className="discounted-page-hero">
        <div className="discounted-page-hero-inner">
          <nav className="discounted-page-breadcrumb" aria-label="breadcrumb">
            <Link to="/">Ana Sayfa</Link>
            <span>/</span>
            <span>İndirimli Ürünler</span>
          </nav>

          <div className="discounted-page-hero-content">
            <div className="discounted-page-badge">
              <i className="fas fa-tags" aria-hidden="true" />
              Fırsat
            </div>
            <h1 className="discounted-page-title">İndirimli Ürünler</h1>
            <p className="discounted-page-subtitle">
              {loading
                ? "İndirimli ürünler yükleniyor…"
                : products.length > 0
                  ? `${products.length} fırsat ürünü${maxDiscount > 0 ? ` · %${maxDiscount}'e varan indirim` : ""}`
                  : "Şu an listelenecek indirimli ürün yok"}
            </p>
          </div>
        </div>
      </div>

      <div className="discounted-page-body">
        {error && (
          <div className="alert alert-warning" role="alert">
            {error}
          </div>
        )}

        {loading ? (
          <div className="discounted-page-loading">
            <div className="spinner-border text-warning" role="status">
              <span className="visually-hidden">Yükleniyor…</span>
            </div>
            <p>İndirimli ürünler getiriliyor…</p>
          </div>
        ) : products.length === 0 ? (
          <div className="discounted-page-empty">
            <i className="fas fa-percentage" aria-hidden="true" />
            <h2>Şu an indirimli ürün yok</h2>
            <p>Yakında yeni fırsatlarla burada olacağız.</p>
            <Link to="/" className="btn btn-warning fw-semibold">
              Ana Sayfaya Dön
            </Link>
          </div>
        ) : (
          <ProductGrid
            products={pageProducts}
            showTitle={false}
            displayMode="grid"
            showViewAll={false}
            initialPage={safePage}
            initialPageSize={PAGE_SIZE}
            onPaginationChange={
              totalPages > 1
                ? ({ page }) => handlePaginationChange({ page })
                : null
            }
          />
        )}

        {!loading && totalPages > 1 && (
          <div className="discounted-page-pager">
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              disabled={safePage <= 1}
              onClick={() => handlePaginationChange({ page: safePage - 1 })}
            >
              Önceki
            </button>
            <span>
              {safePage} / {totalPages}
            </span>
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              disabled={safePage >= totalPages}
              onClick={() => handlePaginationChange({ page: safePage + 1 })}
            >
              Sonraki
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
