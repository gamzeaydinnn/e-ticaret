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
import { CartService } from "../services/cartService";
import getProductCategoryRules from "../config/productCategoryRules";
import variantStore, { getVariantsForProduct } from "../utils/variantStore";
import reviewService from "../services/reviewService"; // ✅ yorum servisi
import ReviewList from "../components/ReviewList"; // ✅ yorumları listeleyen component
import ReviewForm from "../components/ReviewForm"; // ✅ yorum formu
import { useAuth } from "../contexts/AuthContext"; // ✅ kullanıcı giriş kontrolü

export default function ProductDetail() {
  const { id } = useParams();
  const [product, setProduct] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [reviews, setReviews] = useState([]);
  const { user } = useAuth(); // kullanıcı giriş yapmış mı kontrol eder

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

  const addToCart = () => {
    // normalize to existing API wrapper
    CartService.addItem(product.id, quantity)
      .then(() => alert("Sepete eklendi"))
      .catch(() => alert("Hata oluştu"));
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
            String(e).toLowerCase()
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
        // load client-side variants (API not ready yet)
        try {
          const v = getVariantsForProduct(product.id);
          setVariants(v || []);
          if (v && v.length) setSelectedVariantId(v[0].id);
        } catch (e) {
          setVariants([]);
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
          `Maksimum ${max} ${rule.unit} ile sınırlıdır.`
        );
      // step validation: allow small float rounding
      const remainder = Math.abs(
        (q - min) / step - Math.round((q - min) / step)
      );
      if (remainder > 1e-6)
        return setValidationError(
          `Miktar ${step} ${rule.unit} adımlarıyla olmalıdır.`
        );
    } else {
      // default business rule: do not allow >5 units for regular (non-weighted) items
      if (!product.isWeighted && q > 5)
        return setValidationError(
          "Bu üründen en fazla 5 adet ekleyebilirsiniz."
        );
    }
    // pass validation
    CartService.addItem(product.id, q)
      .then(() => alert("Sepete eklendi"))
      .catch(() => alert("Hata oluştu"));
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
      <div className="container mx-auto px-4 py-8">
        {/* ÜRÜN DETAYI */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-10">
          <img
            src={product.imageUrl}
            alt={product.name}
            className="w-full rounded shadow"
          />
          <div>
            <h1 className="text-2xl font-bold mb-4">{product.name}</h1>
            <p className="mb-4">{product.description}</p>
            {(() => {
              const basePrice = Number(product.price || 0);
              const special = Number(
                product.specialPrice ?? product.discountPrice ?? basePrice
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
                  <div className="mb-2">
                    {hasDiscount && (
                      <div className="mb-1">
                        <span className="line-through text-gray-500 mr-2">
                          ₺{basePrice.toFixed(2)}
                        </span>
                        <span className="inline-block bg-red-600 text-white text-xs px-2 py-1 rounded-full">
                          -%{discountPct}
                        </span>
                      </div>
                    )}
                    <div className="text-2xl font-bold text-orange-600">
                      ₺{currentPrice.toFixed(2)}
                    </div>
                  </div>
                  {isOutOfStock && (
                    <div className="text-red-600 font-semibold mb-3">
                      Stokta Yok
                    </div>
                  )}
                  {isLowStock && !isOutOfStock && (
                    <div className="text-yellow-600 font-semibold mb-3">
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
                <label className="block text-sm mb-1">Varyant</label>
                <select
                  className="form-control"
                  value={selectedVariantId || ""}
                  onChange={(e) => {
                    const vid = e.target.value;
                    setSelectedVariantId(vid);
                    const v = variants.find(
                      (x) => String(x.id) === String(vid)
                    );
                    if (v) {
                      // adjust price/stock locally
                      if (v.priceAdjustment) {
                        // we don't mutate product here; just set displayed price
                        // store original price in a ref would be better; simple approach:
                        product.price =
                          (product.price || 0) + (v.priceAdjustment || 0);
                      }
                      if (v.quantity) setQuantity(v.quantity);
                    }
                  }}
                >
                  {variants.map((v) => (
                    <option key={v.id} value={v.id}>
                      {v.packageType || `${v.quantity} ${v.unit}`}
                      {v.expiresAt ? ` — SKT: ${v.expiresAt}` : ""}
                    </option>
                  ))}
                </select>
              </div>
            )}

            <div className="flex items-center gap-3 mb-3">
              <label className="block text-sm">Miktar</label>
              <input
                type="number"
                step={rule ? rule.step ?? (rule.unit === "kg" ? 0.25 : 1) : 1}
                value={quantity}
                onChange={(e) => setQuantity(e.target.value)}
                className="border rounded px-2 py-1"
                style={{ width: 120 }}
              />
              <div className="text-sm text-gray-600">
                {rule ? rule.unit : product.unit || "adet"}
              </div>
            </div>

            {rule && (
              <div className="text-sm text-muted mb-2">
                Min: {rule.min_quantity} — Max: {rule.max_quantity} — Adım:{" "}
                {rule.step ?? (rule.unit === "kg" ? 0.25 : 1)} {rule.unit}
              </div>
            )}

            {validationError && (
              <div className="text-sm text-danger mb-2">{validationError}</div>
            )}

            <button
              onClick={validateAndAdd}
              className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600"
            >
              Sepete Ekle
            </button>
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
