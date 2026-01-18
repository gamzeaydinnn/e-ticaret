/*
1. Amaç
Kullanıcının seçtiği ürünün detaylarını göstermek.
Ürün adı, açıklama, fiyat, stok durumu, görsel.
Ürünü sepete ekleme butonu.
İsteğe bağlı olarak ürün yorumları, benzer ürünler veya kategori bilgisi.*/
import React, { useEffect, useState } from "react";
import { Helmet } from "react-helmet-async";
import { useParams } from "react-router-dom";
import { ProductService } from "../services/productService";
import getProductCategoryRules from "../config/productCategoryRules";
import { getVariantsForProduct } from "../utils/variantStore";
import variantService from "../services/variantService";
import reviewService from "../services/reviewService";
import ReviewList from "../components/ReviewList";
import ReviewForm from "../components/ReviewForm";
import { useAuth } from "../contexts/AuthContext";
import { useCart } from "../contexts/CartContext";

export default function ProductDetail() {
  const { id } = useParams();
  const [product, setProduct] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [reviews, setReviews] = useState([]);
  const { user } = useAuth();
  const { addToCart: ctxAddToCart } = useCart();

  // Ürün detayını getir
  useEffect(() => {
    setLoading(true);
    setError(null);
    ProductService.get(id)
      .then((data) => {
        if (data && data.id) {
          setProduct(data);
        } else {
          setError("Ürün bulunamadı");
        }
      })
      .catch((err) => {
        console.error("Ürün yüklenirken hata:", err);
        setError("Ürün yüklenirken bir hata oluştu");
      })
      .finally(() => {
        setLoading(false);
      });
  }, [id]);

  // Yorumları getir
  const fetchReviews = async () => {
    try {
      const response = await reviewService.getReviewsByProductId(id);
      // `api` wrapper returns response data directly
      const revs = Array.isArray(response)
        ? response
        : Array.isArray(response?.items)
          ? response.items
          : [];
      setReviews(revs);
    } catch (error) {
      console.error("Yorumlar getirilirken hata oluştu:", error);
    }
  };

  useEffect(() => {
    fetchReviews();
  }, [id]);

  // Yorum gönderilince listeyi yenile
  const handleReviewSubmitted = () => {
    fetchReviews();
  };

  const [quantity, setQuantity] = useState(1);
  const [rule, setRule] = useState(null);
  const [validationError, setValidationError] = useState("");
  const [variants, setVariants] = useState([]);
  const [selectedVariantId, setSelectedVariantId] = useState(null);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const rules = await getProductCategoryRules();
        if (!mounted) return;
        if (!product) return; // guard when product not yet loaded
        let match = (rules || []).find((r) => {
          const examples = (r.examples || []).map((e) =>
            String(e).toLowerCase(),
          );
          const pname = (product.name || "").toLowerCase();
          return (
            (r.category || "").toLowerCase().includes(pname) ||
            examples.some((ex) => pname.includes(ex) || ex.includes(pname))
          );
        });
        const pcat = (product.categoryName || "").toLowerCase();
        if (
          !match &&
          (pcat.includes("meyve") ||
            pcat.includes("sebze") ||
            pcat.includes("et") ||
            pcat.includes("tavuk") ||
            pcat.includes("balık") ||
            pcat.includes("balik"))
        ) {
          match =
            (rules || []).find((r) => (r.unit || "").toLowerCase() === "kg") ||
            null;
        }
        // categories that should be sold as units with min 1 max 10
        const unitLimitCats = [
          "süt",
          "süt ürünleri",
          "süt urunleri",
          "temel gıda",
          "temel gida",
          "temizlik",
          "içecek",
          "icecek",
          "atıştırmalık",
          "atistirmalik",
        ];
        if (!match && unitLimitCats.some((tok) => pcat.includes(tok))) {
          match = {
            category: "Kategori adedi sınırı",
            unit: "adet",
            min_quantity: 1,
            max_quantity: 10,
            step: 1,
          };
        }
        setRule(match || null);
        if (match) {
          // set sensible default quantity respecting min and step
          const defaultQ = match.min_quantity || 1;
          setQuantity(defaultQ);
        }
        // load variants from API (with fallback to client-side variantStore)
        try {
          // Önce API'den çekmeyi dene
          const apiVariants = await variantService.getByProduct(product.id);
          if (apiVariants && apiVariants.length > 0) {
            setVariants(apiVariants);
            // Varsayılan varyantı seç (ilk stoklu olanı)
            const defaultVariant =
              apiVariants.find((v) => v.stock > 0) || apiVariants[0];
            setSelectedVariantId(defaultVariant.id);
          } else {
            // API'de yoksa local store'a bak (fallback)
            const v = getVariantsForProduct(product.id);
            setVariants(v || []);
            if (v && v.length) setSelectedVariantId(v[0].id);
          }
        } catch (e) {
          // API hatası - local store'a fallback
          const v = getVariantsForProduct(product.id);
          setVariants(v || []);
          if (v && v.length) setSelectedVariantId(v[0].id);
        }
      } catch (e) {
        setRule(null);
      }
    })();
    return () => (mounted = false);
  }, [product]);

  const validateAndAdd = () => {
    setValidationError("");
    const q = parseFloat(quantity);
    if (isNaN(q) || q <= 0) return setValidationError("Geçerli miktar girin.");
    if (rule) {
      const min = parseFloat(rule.min_quantity ?? -Infinity);
      const max = parseFloat(rule.max_quantity ?? Infinity);
      const step = parseFloat(rule.step ?? (rule.unit === "kg" ? 0.25 : 1));
      if (q < min)
        return setValidationError(`Minimum ${min} ${rule.unit} olmalıdır.`);
      if (q > max)
        return setValidationError(
          `Maksimum ${max} ${rule.unit} ile sınırlıdır.`,
        );
      // step validation: allow small float rounding
      const remainder = Math.abs(
        (q - min) / step - Math.round((q - min) / step),
      );
      if (remainder > 1e-6)
        return setValidationError(
          `Miktar ${step} ${rule.unit} adımlarıyla olmalıdır.`,
        );
    } else {
      // default business rule: do not allow >5 units for regular (non-weighted) items
      if (!product.isWeighted && q > 5)
        return setValidationError(
          "Bu üründen en fazla 5 adet ekleyebilirsiniz.",
        );
    }
    // pass validation
    ctxAddToCart(product, q);
    setValidationError("");
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8 text-center">
        <div
          className="spinner-border text-warning"
          role="status"
          style={{ width: "3rem", height: "3rem" }}
        >
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
        <p className="mt-3 text-muted">Ürün yükleniyor...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="container mx-auto px-4 py-8 text-center">
        <div className="alert alert-warning" style={{ borderRadius: "15px" }}>
          <i className="fas fa-exclamation-triangle me-2"></i>
          {error}
        </div>
        <a href="/" className="btn btn-outline-warning mt-3">
          <i className="fas fa-home me-2"></i>
          Ana Sayfaya Dön
        </a>
      </div>
    );
  }

  if (!product) {
    return (
      <div className="container mx-auto px-4 py-8 text-center">
        <div className="alert alert-info" style={{ borderRadius: "15px" }}>
          <i className="fas fa-info-circle me-2"></i>
          Ürün bulunamadı
        </div>
        <a href="/" className="btn btn-outline-primary mt-3">
          <i className="fas fa-home me-2"></i>
          Ana Sayfaya Dön
        </a>
      </div>
    );
  }

  return (
    <>
      <Helmet>
        {(() => {
          const siteUrl =
            process.env.REACT_APP_SITE_URL ||
            (typeof window !== "undefined" ? window.location.origin : "");
          const ogImage = product.imageUrl
            ? `${siteUrl}${product.imageUrl}`
            : `${siteUrl}/images/og-default.jpg`;
          const canonical = `${siteUrl}${
            typeof window !== "undefined" ? window.location.pathname : ""
          }`;
          return (
            <>
              <title>{product.name} — Doğadan Sofranza</title>
              <meta
                name="description"
                content={
                  product.shortDescription ||
                  (product.description || "").slice(0, 150)
                }
              />
              <meta property="og:title" content={product.name} />
              <meta
                property="og:description"
                content={
                  product.shortDescription ||
                  (product.description || "").slice(0, 150)
                }
              />
              <meta property="og:image" content={ogImage} />
              <meta property="og:type" content="product" />
              <meta name="twitter:card" content="summary_large_image" />
              <link rel="canonical" href={canonical} />
            </>
          );
        })()}
      </Helmet>
      <div className="container mx-auto px-4 py-4 py-md-8">
        {/* ÜRÜN DETAYI */}
        <div className="row g-4 mb-4">
          <div className="col-12 col-md-6">
            <img
              src={product.imageUrl}
              alt={product.name}
              className="w-100 rounded shadow"
              style={{ maxHeight: "500px", objectFit: "contain" }}
            />
          </div>
          <div className="col-12 col-md-6">
            <h1 className="h3 h2-md fw-bold mb-3">{product.name}</h1>
            <p className="mb-3 text-muted">{product.description}</p>
            {(() => {
              const basePrice = Number(product.price || 0);
              const special = Number(
                product.specialPrice ?? product.discountPrice ?? basePrice,
              );
              const hasDiscount =
                !Number.isNaN(special) && special > 0 && special < basePrice;
              const currentPrice = hasDiscount ? special : basePrice;
              const discountPct = hasDiscount
                ? Math.round(100 - (currentPrice / basePrice) * 100)
                : 0;

              const stock = product.stock ?? product.stockQuantity ?? null;
              const isOutOfStock = typeof stock === "number" && stock <= 0;
              const isLowStock =
                typeof stock === "number" && stock > 0 && stock <= 5;

              return (
                <>
                  <div className="mb-3">
                    {hasDiscount && (
                      <div className="mb-2">
                        <span className="text-decoration-line-through text-muted me-2 fs-5">
                          ₺{basePrice.toFixed(2)}
                        </span>
                        <span className="badge bg-danger rounded-pill">
                          -%{discountPct}
                        </span>
                      </div>
                    )}
                    <div className="fs-2 fw-bold text-warning">
                      ₺{currentPrice.toFixed(2)}
                    </div>
                  </div>
                  {isOutOfStock && (
                    <div className="alert alert-danger py-2 mb-3">
                      <i className="fas fa-times-circle me-2"></i>Stokta Yok
                    </div>
                  )}
                  {isLowStock && !isOutOfStock && (
                    <div className="alert alert-warning py-2 mb-3">
                      <i className="fas fa-exclamation-triangle me-2"></i>
                      Az Stok{" "}
                      {typeof stock === "number" && stock > 0
                        ? `(${stock} adet kaldı)`
                        : ""}
                    </div>
                  )}
                </>
              );
            })()}

            {variants.length > 0 && (
              <div className="mb-3">
                <label className="form-label fw-semibold">
                  <i className="fas fa-boxes me-2 text-warning"></i>Seçenek
                </label>
                {/* SKU ve Stok bilgisi */}
                {selectedVariantId &&
                  (() => {
                    const selectedVar = variants.find(
                      (v) => String(v.id) === String(selectedVariantId),
                    );
                    return selectedVar ? (
                      <div className="d-flex gap-2 mb-2 flex-wrap">
                        <small className="badge bg-secondary">
                          SKU: {selectedVar.sku}
                        </small>
                        <small
                          className={`badge ${selectedVar.stock > 0 ? "bg-success" : "bg-danger"}`}
                        >
                          Stok: {selectedVar.stock}
                        </small>
                        {selectedVar.barcode && (
                          <small className="badge bg-info">
                            Barkod: {selectedVar.barcode}
                          </small>
                        )}
                      </div>
                    ) : null;
                  })()}
                {/* Varyant butonları veya dropdown */}
                {variants.length <= 5 ? (
                  <div className="d-flex gap-2 flex-wrap">
                    {variants.map((v) => {
                      const isSelected =
                        String(v.id) === String(selectedVariantId);
                      const isOutOfStock = v.stock <= 0;
                      return (
                        <button
                          key={v.id}
                          type="button"
                          className={`btn ${isSelected ? "btn-warning" : "btn-outline-secondary"} ${isOutOfStock ? "opacity-50" : ""}`}
                          style={{
                            borderRadius: "8px",
                            padding: "8px 16px",
                            fontSize: "0.9rem",
                          }}
                          onClick={() => {
                            setSelectedVariantId(v.id);
                            // Fiyat varsa güncelle
                            if (v.price) {
                              product.price = v.price;
                            }
                          }}
                          disabled={isOutOfStock}
                        >
                          {v.variantTitle || v.sku}
                          {isOutOfStock && " (Tükendi)"}
                        </button>
                      );
                    })}
                  </div>
                ) : (
                  <select
                    className="form-select"
                    value={selectedVariantId || ""}
                    onChange={(e) => {
                      const vid = e.target.value;
                      setSelectedVariantId(vid);
                      const v = variants.find(
                        (x) => String(x.id) === String(vid),
                      );
                      if (v && v.price) {
                        product.price = v.price;
                      }
                    }}
                  >
                    {variants.map((v) => (
                      <option key={v.id} value={v.id} disabled={v.stock <= 0}>
                        {v.variantTitle || v.sku} - ₺
                        {v.price?.toFixed(2) || "0.00"}
                        {v.stock <= 0 ? " (Tükendi)" : ` (${v.stock} stokta)`}
                      </option>
                    ))}
                  </select>
                )}
              </div>
            )}

            {variants.length === 0 && (
              <div className="mb-3">
                {/* Varyant yoksa sadece ana ürün bilgisi göster */}
              </div>
            )}

            <div className="row g-2 mb-3 align-items-center">
              <div className="col-auto">
                <label className="form-label fw-semibold mb-0">
                  <i className="fas fa-sort-numeric-up me-2 text-warning"></i>
                  Miktar
                </label>
              </div>
              <div className="col-auto">
                <div className="input-group" style={{ width: "140px" }}>
                  <button
                    className="btn btn-outline-warning"
                    type="button"
                    onClick={() => {
                      const step = rule
                        ? (rule.step ?? (rule.unit === "kg" ? 0.25 : 1))
                        : 1;
                      const newQty = Math.max(
                        (parseFloat(quantity) || 0) - step,
                        rule?.min_quantity || step,
                      );
                      setQuantity(newQty);
                    }}
                  >
                    <i className="fas fa-minus"></i>
                  </button>
                  <input
                    type="number"
                    step={
                      rule ? (rule.step ?? (rule.unit === "kg" ? 0.25 : 1)) : 1
                    }
                    value={quantity}
                    onChange={(e) => setQuantity(e.target.value)}
                    className="form-control text-center"
                  />
                  <button
                    className="btn btn-outline-warning"
                    type="button"
                    onClick={() => {
                      const step = rule
                        ? (rule.step ?? (rule.unit === "kg" ? 0.25 : 1))
                        : 1;
                      const newQty = (parseFloat(quantity) || 0) + step;
                      if (!rule || newQty <= rule.max_quantity) {
                        setQuantity(newQty);
                      }
                    }}
                  >
                    <i className="fas fa-plus"></i>
                  </button>
                </div>
              </div>
              <div className="col-auto">
                <span className="text-muted">
                  {rule ? rule.unit : product.unit || "adet"}
                </span>
              </div>
            </div>

            {rule && (
              <div className="alert alert-info py-2 mb-3 small">
                <i className="fas fa-info-circle me-2"></i>
                Min: {rule.min_quantity} — Max: {rule.max_quantity} — Adım:{" "}
                {rule.step ?? (rule.unit === "kg" ? 0.25 : 1)} {rule.unit}
              </div>
            )}

            {validationError && (
              <div className="alert alert-danger py-2 mb-3">
                <i className="fas fa-exclamation-circle me-2"></i>
                {validationError}
              </div>
            )}

            {(() => {
              const stock = product.stock ?? product.stockQuantity ?? null;
              const isOutOfStock = typeof stock === "number" && stock <= 0;
              return (
                <button
                  onClick={validateAndAdd}
                  className="btn btn-warning btn-lg w-100 fw-bold shadow"
                  disabled={isOutOfStock}
                >
                  <i className="fas fa-shopping-cart me-2"></i>
                  {isOutOfStock ? "Stokta Yok" : "Sepete Ekle"}
                </button>
              );
            })()}
          </div>
        </div>

        {/* YORUMLAR */}
        <div className="mt-8">
          <h2 className="text-xl font-semibold mb-4">Ürün Yorumları</h2>
          <ReviewList reviews={reviews} />

          {user ? (
            <div className="mt-6">
              <h3 className="text-lg font-medium mb-2">Yorum Yap</h3>
              <ReviewForm
                productId={id}
                onReviewSubmitted={handleReviewSubmitted}
              />
            </div>
          ) : (
            <p className="text-gray-500 italic mt-4">
              Yorum yapmak için giriş yapmalısınız.
            </p>
          )}
        </div>
      </div>
    </>
  );
}
