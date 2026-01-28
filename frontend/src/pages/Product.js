/**
 * Product.js - Ürün Detay Sayfası
 *
 * ProductDetailModal'daki tüm özellikler bu sayfaya taşındı:
 * - Besin değerleri tablosu
 * - Ürün bilgileri sekmesi
 * - Güvenlik bilgileri
 * - Paket bilgileri
 * - İade koşulları
 * - Favorilere ekleme
 * - Karşılaştırmaya ekleme
 * - Miktar seçici (kg/adet destekli)
 *
 * Modal yerine ayrı sayfa olarak tasarlandı.
 *
 * @author Senior E-Ticaret Team
 * @version 2.0.0
 */
import React, { useEffect, useState, useCallback, useMemo } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import { ProductService } from "../services/productService";
import { useCart } from "../contexts/CartContext";
import { useFavorites } from "../contexts/FavoriteContext";
import { useCompare } from "../contexts/CompareContext";
import { useAuth } from "../contexts/AuthContext";
import getProductCategoryRules from "../config/productCategoryRules";
import "./Product.css";

// ============================================================
// SABİTLER
// ============================================================

// Sekme tanımları
const TABS = {
  NUTRITION: "nutrition",
  INFO: "info",
  SAFETY: "safety",
  PACKAGE: "package",
  RETURN: "return",
};

// Varsayılan besin değerleri
const DEFAULT_NUTRITION_VALUES = {
  energyKcal: null,
  energyKj: null,
  fat: null,
  saturatedFat: null,
  carbohydrate: null,
  sugar: null,
  sugarAlcohol: null,
  protein: null,
  salt: null,
  fiber: null,
};

// ============================================================
// YARDIMCI BİLEŞENLER
// ============================================================

/**
 * Besin değeri satırı komponenti
 * Tabloda tutarlı ve okunabilir satırlar için kullanılır
 */
const NutritionRow = ({ label, value, unit = "g" }) => {
  // Değer yoksa veya null ise gösterme
  if (value === null || value === undefined) return null;

  return (
    <tr className="nutrition-row">
      <td className="nutrition-label">{label}</td>
      <td className="nutrition-value">
        {typeof value === "number" ? value.toFixed(1) : value}
        {unit !== "" && ` ${unit}`}
      </td>
    </tr>
  );
};

/**
 * Bilgi kartı komponenti
 * Uyarı ve bilgilendirme mesajları için kullanılır
 */
const InfoCard = ({ icon, title, children, variant = "info" }) => (
  <div className={`info-card info-card--${variant}`}>
    <div className="info-card__header">
      <i className={`fas ${icon} info-card__icon`}></i>
      <h4 className="info-card__title">{title}</h4>
    </div>
    <div className="info-card__content">{children}</div>
  </div>
);

// ============================================================
// ANA KOMPONENT
// ============================================================

export default function Product() {
  const { id } = useParams();
  const navigate = useNavigate();

  // Context hooks
  // eslint-disable-next-line no-unused-vars
  const { user } = useAuth();
  const { addToCart: ctxAddToCart } = useCart();
  const { toggleFavorite, isFavorite } = useFavorites();
  const { toggleCompare, isInCompare } = useCompare();

  // State tanımlamaları
  const [product, setProduct] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [quantity, setQuantity] = useState(1);
  const [rule, setRule] = useState(null);
  const [validationError, setValidationError] = useState("");
  const [activeTab, setActiveTab] = useState(TABS.NUTRITION);
  const [imageLoaded, setImageLoaded] = useState(false);
  const [showFullDescription, setShowFullDescription] = useState(false);
  const [addedToCart, setAddedToCart] = useState(false);

  // ============================================================
  // ÜRÜN VERİSİNİ YÜKLE
  // ============================================================
  useEffect(() => {
    let mounted = true;

    const loadProduct = async () => {
      setLoading(true);
      setError(null);

      try {
        const data = await ProductService.get(id);

        if (!mounted) return;

        if (data && data.id) {
          setProduct(data);
        } else {
          setError("Ürün bulunamadı");
        }
      } catch (err) {
        console.error("Ürün yüklenirken hata:", err);
        if (mounted) {
          setError("Ürün yüklenirken bir hata oluştu");
        }
      } finally {
        if (mounted) {
          setLoading(false);
        }
      }
    };

    loadProduct();

    return () => {
      mounted = false;
    };
  }, [id]);

  // ============================================================
  // KATEGORİ KURALLARINI YÜKLE
  // ============================================================
  useEffect(() => {
    if (!product) return;

    let mounted = true;

    const loadRules = async () => {
      try {
        const rules = await getProductCategoryRules();
        if (!mounted) return;

        const pname = (product.name || "").toLowerCase();
        const pcat = (product.categoryName || "").toLowerCase();

        // Kategori bazlı kural eşleştirme
        let match = (rules || []).find((r) => {
          const examples = (r.examples || []).map((e) =>
            String(e).toLowerCase(),
          );
          return (
            (r.category || "").toLowerCase().includes(pname) ||
            examples.some((ex) => pname.includes(ex) || ex.includes(pname))
          );
        });

        // Tartılı ürün kategorileri için kg kuralı
        if (
          !match &&
          (pcat.includes("meyve") ||
            pcat.includes("sebze") ||
            pcat.includes("et") ||
            pcat.includes("tavuk") ||
            pcat.includes("balık") ||
            pcat.includes("balik"))
        ) {
          match = (rules || []).find(
            (r) => (r.unit || "").toLowerCase() === "kg",
          );
        }

        // Adet limitli kategoriler
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
        setQuantity(match?.min_quantity || 1);
      } catch (error) {
        console.error("Kural yükleme hatası:", error);
        setRule(null);
      }
    };

    loadRules();

    return () => {
      mounted = false;
    };
  }, [product]);

  // ============================================================
  // MİKTAR DEĞİŞİKLİĞİ
  // ============================================================
  const handleQuantityChange = useCallback(
    (delta) => {
      setValidationError("");
      const step = rule?.step ?? (rule?.unit === "kg" ? 0.25 : 1);
      const min = rule?.min_quantity || step;
      const max = rule?.max_quantity || 99;

      setQuantity((prev) => {
        const newQty = parseFloat(prev) + delta * step;
        return Math.max(min, Math.min(max, newQty));
      });
    },
    [rule],
  );

  // ============================================================
  // SEPETE EKLEME VALİDASYONU
  // ============================================================
  const handleAddToCart = useCallback(() => {
    if (!product) return;

    setValidationError("");
    const q = parseFloat(quantity);

    if (isNaN(q) || q <= 0) {
      setValidationError("Geçerli bir miktar girin.");
      return;
    }

    if (rule) {
      const min = parseFloat(rule.min_quantity ?? -Infinity);
      const max = parseFloat(rule.max_quantity ?? Infinity);
      const step = parseFloat(rule.step ?? (rule.unit === "kg" ? 0.25 : 1));

      if (q < min) {
        setValidationError(`Minimum ${min} ${rule.unit} olmalıdır.`);
        return;
      }
      if (q > max) {
        setValidationError(`Maksimum ${max} ${rule.unit} ile sınırlıdır.`);
        return;
      }

      // Step validasyonu
      const remainder = Math.abs(
        (q - min) / step - Math.round((q - min) / step),
      );
      if (remainder > 1e-6) {
        setValidationError(
          `Miktar ${step} ${rule.unit} adımlarıyla olmalıdır.`,
        );
        return;
      }
    } else if (!product.isWeighted && q > 5) {
      setValidationError("Bu üründen en fazla 5 adet ekleyebilirsiniz.");
      return;
    }

    // Sepete ekle
    ctxAddToCart(product, q);

    // Başarı geri bildirimi
    setAddedToCart(true);
    setTimeout(() => setAddedToCart(false), 2000);
  }, [quantity, rule, product, ctxAddToCart]);

  // ============================================================
  // PAYLAŞ FONKSİYONU
  // ============================================================
  const handleShare = useCallback(() => {
    const url = window.location.href;
    if (navigator.share) {
      navigator.share({ title: product?.name, url }).catch(() => {});
    } else {
      navigator.clipboard.writeText(url).then(() => {
        alert("Bağlantı kopyalandı!");
      });
    }
  }, [product?.name]);

  // ============================================================
  // FİYAT HESAPLAMALARI (memoized)
  // Backend'den gelen kampanya bilgilerini de kullanır
  // ============================================================
  const priceInfo = useMemo(() => {
    if (!product) return null;

    // Backend'den gelen originalPrice varsa onu kullan, yoksa price
    const basePrice = Number(
      product.originalPrice || product.price || product.unitPrice || 0,
    );
    const specialPrice = Number(
      product.specialPrice ?? product.discountPrice ?? basePrice,
    );
    const hasDiscount =
      !isNaN(specialPrice) && specialPrice > 0 && specialPrice < basePrice;
    const currentPrice = hasDiscount ? specialPrice : basePrice;

    // Backend'den gelen discountPercentage varsa onu kullan, yoksa hesapla
    const discountPercent = product.discountPercentage
      ? product.discountPercentage
      : hasDiscount
        ? Math.round(100 - (currentPrice / basePrice) * 100)
        : 0;

    // Kampanya bilgileri (backend'den geliyorsa)
    const campaignId = product.campaignId || null;
    const campaignName = product.campaignName || null;
    const hasActiveCampaign =
      product.hasActiveCampaign || (campaignId && hasDiscount);

    return {
      basePrice,
      currentPrice,
      hasDiscount,
      discountPercent,
      campaignId,
      campaignName,
      hasActiveCampaign,
    };
  }, [product]);

  // ============================================================
  // STOK DURUMU (memoized)
  // ============================================================
  const stockInfo = useMemo(() => {
    if (!product)
      return { isOutOfStock: false, isLowStock: false, stock: null };

    const stock = product.stock ?? product.stockQuantity ?? null;
    const isOutOfStock = typeof stock === "number" && stock <= 0;
    const isLowStock = typeof stock === "number" && stock > 0 && stock <= 5;

    return { isOutOfStock, isLowStock, stock };
  }, [product]);

  // ============================================================
  // BESİN DEĞERLERİ (memoized)
  // ============================================================
  const nutritionValues = useMemo(() => {
    if (!product) return DEFAULT_NUTRITION_VALUES;

    return {
      energyKcal: product.nutritionEnergyKcal ?? product.calories ?? 0.2,
      energyKj: product.nutritionEnergyKj ?? 0.8,
      fat: product.nutritionFat ?? product.fat ?? 0.0,
      saturatedFat: product.nutritionSaturatedFat ?? 0.0,
      carbohydrate: product.nutritionCarbs ?? product.carbs ?? 0.0,
      sugar: product.nutritionSugar ?? product.sugar ?? 0.0,
      sugarAlcohol: product.nutritionSugarAlcohol ?? 0.0,
      protein: product.nutritionProtein ?? product.protein ?? 0.0,
      salt: product.nutritionSalt ?? product.salt ?? 0.0,
      fiber: product.nutritionFiber ?? product.fiber ?? null,
    };
  }, [product]);

  // ============================================================
  // LOADING STATE
  // ============================================================
  if (loading) {
    return (
      <div className="product-page">
        <div className="product-page__loading">
          <div className="product-page__spinner">
            <i className="fas fa-spinner fa-spin"></i>
          </div>
          <p>Ürün yükleniyor...</p>
        </div>
      </div>
    );
  }

  // ============================================================
  // ERROR STATE
  // ============================================================
  if (error) {
    return (
      <div className="product-page">
        <div className="product-page__error">
          <i className="fas fa-exclamation-triangle"></i>
          <h2>{error}</h2>
          <p>Bu ürün mevcut değil veya kaldırılmış olabilir.</p>
          <button
            className="product-page__back-btn"
            onClick={() => navigate("/")}
          >
            <i className="fas fa-home"></i>
            Ana Sayfaya Dön
          </button>
        </div>
      </div>
    );
  }

  // ============================================================
  // ÜRÜN BULUNAMADI STATE
  // ============================================================
  if (!product) {
    return (
      <div className="product-page">
        <div className="product-page__not-found">
          <i className="fas fa-search"></i>
          <h2>Ürün Bulunamadı</h2>
          <p>Aradığınız ürün sistemde kayıtlı değil.</p>
          <button
            className="product-page__back-btn"
            onClick={() => navigate("/")}
          >
            <i className="fas fa-home"></i>
            Ana Sayfaya Dön
          </button>
        </div>
      </div>
    );
  }

  // Breadcrumb oluşturma
  const breadcrumb = [
    { label: "Anasayfa", path: "/" },
    product.categoryName && {
      label: product.categoryName,
      path: `/category/${product.categoryId}`,
    },
    product.subCategoryName && {
      label: product.subCategoryName,
      path: null,    },
    { label: product.name, path: null },
  ].filter(Boolean);

  // ============================================================
  // ANA RENDER
  // ============================================================
  return (
    <div className="product-page">
      {/* Breadcrumb Navigasyonu */}
      <nav className="product-page__breadcrumb">
        {breadcrumb.map((item, index) => (
          <span key={index} className="product-page__breadcrumb-item">
            {index > 0 && (
              <i className="fas fa-chevron-right product-page__breadcrumb-separator"></i>
            )}
            {item.path ? (
              <Link to={item.path}>{item.label}</Link>
            ) : (
              <span className="product-page__breadcrumb-current">
                {item.label}
              </span>
            )}
          </span>
        ))}
      </nav>

      {/* Ana İçerik */}
      <div className="product-page__content">
        {/* Sol Taraf - Ürün Görseli */}
        <div className="product-page__image-section">
          <div className="product-page__image-container">
            {/* Yükleniyor göstergesi */}
            {!imageLoaded && (
              <div className="product-page__image-skeleton">
                <i className="fas fa-image"></i>
              </div>
            )}

            {/* Ürün görseli - Mevcut imageUrl korunuyor */}
            <img
              src={product.imageUrl || "/images/placeholder.png"}
              alt={product.name}
              className={`product-page__image ${
                imageLoaded ? "product-page__image--loaded" : ""
              }`}
              onLoad={() => setImageLoaded(true)}
              onError={(e) => {
                e.target.src = "/images/placeholder.png";
                setImageLoaded(true);
              }}
            />

            {/* İndirim rozeti */}
            {priceInfo?.hasDiscount && (
              <div className="product-page__discount-badge">
                <span>%{priceInfo.discountPercent}</span>
                <small>İNDİRİM</small>
              </div>
            )}

            {/* Yeni ürün rozeti */}
            {product.isNew && (
              <div className="product-page__new-badge">YENİ</div>
            )}
          </div>

          {/* Hızlı aksiyonlar (mobil için) */}
          <div className="product-page__quick-actions product-page__quick-actions--mobile">
            <button
              className={`product-page__action-btn ${
                isFavorite(product.id) ? "product-page__action-btn--active" : ""
              }`}
              onClick={() => toggleFavorite(product.id)}
              aria-label={
                isFavorite(product.id)
                  ? "Favorilerden çıkar"
                  : "Favorilere ekle"
              }
            >
              <i
                className={`${isFavorite(product.id) ? "fas" : "far"} fa-heart`}
              ></i>
            </button>
            <button
              className={`product-page__action-btn ${
                isInCompare(product.id)
                  ? "product-page__action-btn--active"
                  : ""
              }`}
              onClick={() => toggleCompare(product)}
              aria-label={
                isInCompare(product.id)
                  ? "Karşılaştırmadan çıkar"
                  : "Karşılaştırmaya ekle"
              }
            >
              <i className="fas fa-balance-scale"></i>
            </button>
            <button
              className="product-page__action-btn"
              onClick={handleShare}
              aria-label="Paylaş"
            >
              <i className="fas fa-share-alt"></i>
            </button>
          </div>
        </div>

        {/* Sağ Taraf - Ürün Bilgileri */}
        <div className="product-page__info-section">
          {/* Marka */}
          {product.brand && (
            <span className="product-page__brand">{product.brand}</span>
          )}

          {/* Ürün Adı */}
          <h1 className="product-page__title">{product.name}</h1>

          {/* Değerlendirme */}
          {product.rating > 0 && (
            <div className="product-page__rating">
              <div className="product-page__rating-stars">
                {[...Array(5)].map((_, i) => (
                  <i
                    key={i}
                    className={`fas fa-star ${
                      i < Math.floor(product.rating)
                        ? "product-page__rating-star--filled"
                        : ""
                    }`}
                  ></i>
                ))}
              </div>
              <span className="product-page__rating-count">
                ({product.reviewCount || 0} değerlendirme)
              </span>
            </div>
          )}

          {/* Fiyat Bilgisi */}
          <div className="product-page__price-section">
            {/* Kampanya rozeti - sağ üstte */}
            {priceInfo?.hasDiscount && priceInfo?.discountPercent > 0 && (
              <div className="product-page__campaign-badge">
                <i className="fas fa-bolt"></i>%{priceInfo.discountPercent}{" "}
                İNDİRİM
              </div>
            )}

            {/* Kampanya adı (varsa) */}
            {priceInfo?.campaignName && (
              <div className="product-page__campaign-name">
                <i className="fas fa-tag"></i>
                {priceInfo.campaignName}
              </div>
            )}

            {/* Eski fiyat - üzeri çizili */}
            {priceInfo?.hasDiscount && (
              <span className="product-page__price--original">
                {priceInfo.basePrice.toFixed(2)} <small>TL</small>
              </span>
            )}

            {/* Kampanyalı fiyat kutusu */}
            {priceInfo?.hasDiscount && (
              <div className="product-page__special-price-box">
                <span className="product-page__special-price-label">
                  <i className="fas fa-fire"></i> Kampanyalı Fiyat
                </span>
                <span className="product-page__special-price-value">
                  {priceInfo.currentPrice.toFixed(2)} <small>TL</small>
                </span>
                {/* Ne kadar tasarruf */}
                <span className="product-page__savings">
                  {(priceInfo.basePrice - priceInfo.currentPrice).toFixed(2)} TL
                  tasarruf!
                </span>
              </div>
            )}

            {!priceInfo?.hasDiscount && (
              <span className="product-page__price--current">
                {priceInfo?.currentPrice.toFixed(2)} <small>TL</small>
              </span>
            )}

            {/* Birim fiyatı */}
            {product.unitPrice && (
              <span className="product-page__unit-price">
                ({product.unitPrice.toFixed(2)} TL/{product.unit || "Litre"})
              </span>
            )}
          </div>

          {/* Miktar Seçici */}
          <div className="product-page__quantity-section">
            <label className="product-page__quantity-label">Adet</label>
            <div className="product-page__quantity-controls">
              <button
                className="product-page__quantity-btn"
                onClick={() => handleQuantityChange(-1)}
                disabled={quantity <= (rule?.min_quantity || 1)}
                aria-label="Azalt"
              >
                <i className="fas fa-minus"></i>
              </button>
              <input
                type="number"
                className="product-page__quantity-input"
                value={quantity}
                onChange={(e) => setQuantity(e.target.value)}
                min={rule?.min_quantity || 1}
                max={rule?.max_quantity || 99}
                step={rule?.step ?? (rule?.unit === "kg" ? 0.25 : 1)}
              />
              <button
                className="product-page__quantity-btn"
                onClick={() => handleQuantityChange(1)}
                disabled={quantity >= (rule?.max_quantity || 99)}
                aria-label="Artır"
              >
                <i className="fas fa-plus"></i>
              </button>
              <span className="product-page__quantity-unit">
                {rule?.unit || product.unit || "adet"}
              </span>
            </div>

            {/* Miktar kuralı bilgisi */}
            {rule && (
              <div className="product-page__quantity-info">
                <i className="fas fa-info-circle"></i>
                Min: {rule.min_quantity} — Max: {rule.max_quantity} — Adım:{" "}
                {rule.step ?? 1} {rule.unit}
              </div>
            )}

            {/* Validasyon hatası */}
            {validationError && (
              <div className="product-page__validation-error">
                <i className="fas fa-exclamation-circle"></i>
                {validationError}
              </div>
            )}
          </div>

          {/* Sepete Ekle ve Favori Butonları */}
          <div className="product-page__actions">
            <button
              className={`product-page__add-to-cart ${
                stockInfo.isOutOfStock
                  ? "product-page__add-to-cart--disabled"
                  : ""
              } ${addedToCart ? "product-page__add-to-cart--success" : ""}`}
              onClick={handleAddToCart}
              disabled={stockInfo.isOutOfStock}
            >
              <i
                className={`fas ${
                  addedToCart ? "fa-check" : "fa-shopping-cart"
                }`}
              ></i>
              {stockInfo.isOutOfStock
                ? "Stokta Yok"
                : addedToCart
                  ? "Sepete Eklendi!"
                  : "Sepete Ekle"}
            </button>

            <button
              className={`product-page__favorite-btn ${
                isFavorite(product.id)
                  ? "product-page__favorite-btn--active"
                  : ""
              }`}
              onClick={() => toggleFavorite(product.id)}
              aria-label={
                isFavorite(product.id)
                  ? "Favorilerden çıkar"
                  : "Favorilere ekle"
              }
            >
              <i
                className={`${isFavorite(product.id) ? "fas" : "far"} fa-heart`}
              ></i>
            </button>

            {/* Karşılaştırma butonu (desktop) */}
            <button
              className={`product-page__compare-btn ${
                isInCompare(product.id)
                  ? "product-page__compare-btn--active"
                  : ""
              }`}
              onClick={() => toggleCompare(product)}
              aria-label={
                isInCompare(product.id)
                  ? "Karşılaştırmadan çıkar"
                  : "Karşılaştırmaya ekle"
              }
            >
              <i className="fas fa-balance-scale"></i>
            </button>
          </div>

          {/* Stok Uyarıları */}
          <div className="product-page__stock-alerts">
            {stockInfo.stock !== null && !stockInfo.isOutOfStock && (
              <div
                className={`product-page__stock-info ${
                  stockInfo.isLowStock ? "product-page__stock-info--low" : ""
                }`}
              >
                <i
                  className={`fas ${
                    stockInfo.isLowStock
                      ? "fa-exclamation-triangle"
                      : "fa-check-circle"
                  }`}
                ></i>
                {stockInfo.isLowStock
                  ? `Bu fiyattan satılmak üzere toplam ${stockInfo.stock} adet stok bulunmaktadır.`
                  : `Bu fiyattan satılmak üzere toplam ${stockInfo.stock} adet stok bulunmaktadır.`}
              </div>
            )}

            {product.maxOrderQuantity && (
              <div className="product-page__order-limit">
                <i className="fas fa-info-circle"></i>
                Bu üründen en fazla {product.maxOrderQuantity} adet sipariş
                verilebilir. Belirtilen adet üzerindeki siparişlerin iptal
                edilmesi hakkı saklıdır.
              </div>
            )}

            <div className="product-page__store-info">
              <i className="fas fa-store"></i>
              Ürünün stok, fiyat ve kampanya bilgisi, teslimatı gerçekleştirecek
              mağazanın stok, fiyat ve kampanya bilgilerine göre
              belirlenmektedir.
            </div>
          </div>
        </div>
      </div>

      {/* Sekmeler */}
      <div className="product-page__tabs-section">
        <div className="product-page__tabs">
          <button
            className={`product-page__tab ${
              activeTab === TABS.NUTRITION ? "product-page__tab--active" : ""
            }`}
            onClick={() => setActiveTab(TABS.NUTRITION)}
          >
            Besin Değerleri
          </button>
          <button
            className={`product-page__tab ${
              activeTab === TABS.INFO ? "product-page__tab--active" : ""
            }`}
            onClick={() => setActiveTab(TABS.INFO)}
          >
            Ürün Bilgileri
          </button>
          <button
            className={`product-page__tab ${
              activeTab === TABS.SAFETY ? "product-page__tab--active" : ""
            }`}
            onClick={() => setActiveTab(TABS.SAFETY)}
          >
            Ürün Güvenliği
          </button>
          <button
            className={`product-page__tab ${
              activeTab === TABS.PACKAGE ? "product-page__tab--active" : ""
            }`}
            onClick={() => setActiveTab(TABS.PACKAGE)}
          >
            Ürün Paketi
          </button>
          <button
            className={`product-page__tab ${
              activeTab === TABS.RETURN ? "product-page__tab--active" : ""
            }`}
            onClick={() => setActiveTab(TABS.RETURN)}
          >
            İade Koşulları
          </button>
        </div>

        {/* Sekme İçerikleri */}
        <div className="product-page__tab-content">
          {/* Besin Değerleri */}
          {activeTab === TABS.NUTRITION && (
            <div className="product-page__nutrition">
              <div className="product-page__nutrition-table-wrapper">
                <table className="product-page__nutrition-table">
                  <thead>
                    <tr>
                      <th>Besin Değeri</th>
                      <th>100 g / ml</th>
                    </tr>
                  </thead>
                  <tbody>
                    <NutritionRow
                      label="Enerji (kcal)"
                      value={nutritionValues.energyKcal}
                      unit=""
                    />
                    <NutritionRow
                      label="Enerji (kJ)"
                      value={nutritionValues.energyKj}
                      unit=""
                    />
                    <NutritionRow label="Yağ (g)" value={nutritionValues.fat} />
                    <NutritionRow
                      label="Doymuş yağ (g)"
                      value={nutritionValues.saturatedFat}
                    />
                    <NutritionRow
                      label="Karbonhidrat (g)"
                      value={nutritionValues.carbohydrate}
                    />
                    <NutritionRow
                      label="Şeker (g)"
                      value={nutritionValues.sugar}
                    />
                    <NutritionRow
                      label="Şeker Alkolü (g)"
                      value={nutritionValues.sugarAlcohol}
                    />
                    <NutritionRow
                      label="Protein (g)"
                      value={nutritionValues.protein}
                    />
                    <NutritionRow
                      label="Tuz (g)"
                      value={nutritionValues.salt}
                    />
                    {nutritionValues.fiber !== null && (
                      <NutritionRow
                        label="Lif (g)"
                        value={nutritionValues.fiber}
                      />
                    )}
                  </tbody>
                </table>
              </div>

              <InfoCard
                icon="fa-info-circle"
                title="Ürün Bilgilerini Kullanma Hakkında"
                variant="warning"
              >
                <p>
                  İnternet sitemizde ve online satış kanallarımızda yer alan
                  ürün etiket bilgileri, ürünün tedarikçisi tarafından Dijital
                  Platform Gıda Hizmetleri A.Ş.'ye iletilen en güncel
                  bilgilerdir. Ürün etiket bilgileri ile internet sitemiz ve
                  online satış kanallarımızda bulunan bilgiler arasında herhangi
                  bir farklılık bulunması halinde sorumluluk tamamen tedarikçi
                  firmaya aittir.
                </p>
              </InfoCard>
            </div>
          )}

          {/* Ürün Bilgileri */}
          {activeTab === TABS.INFO && (
            <div className="product-page__info">
              <div className="product-page__info-description">
                <h3>Ürün Açıklaması</h3>
                <p>
                  {showFullDescription
                    ? product.description ||
                      product.longDescription ||
                      "Bu ürün hakkında detaylı bilgi için müşteri hizmetleri ile iletişime geçebilirsiniz."
                    : (
                        product.description ||
                        product.shortDescription ||
                        "Bu ürün hakkında detaylı bilgi için müşteri hizmetleri ile iletişime geçebilirsiniz."
                      ).slice(0, 300)}
                  {(product.description?.length > 300 ||
                    product.longDescription) && (
                    <button
                      className="product-page__read-more"
                      onClick={() =>
                        setShowFullDescription(!showFullDescription)
                      }
                    >
                      {showFullDescription ? "Daha az göster" : "Devamını oku"}
                    </button>
                  )}
                </p>
              </div>

              <div className="product-page__info-details">
                <h3>Ürün Detayları</h3>
                <table className="product-page__info-table">
                  <tbody>
                    {product.brand && (
                      <tr>
                        <td>Marka</td>
                        <td>{product.brand}</td>
                      </tr>
                    )}
                    {product.sku && (
                      <tr>
                        <td>SKU</td>
                        <td>{product.sku}</td>
                      </tr>
                    )}
                    {product.barcode && (
                      <tr>
                        <td>Barkod</td>
                        <td>{product.barcode}</td>
                      </tr>
                    )}
                    {product.weight && (
                      <tr>
                        <td>Ağırlık</td>
                        <td>
                          {product.weight} {product.weightUnit || "g"}
                        </td>
                      </tr>
                    )}
                    {product.origin && (
                      <tr>
                        <td>Menşei</td>
                        <td>{product.origin}</td>
                      </tr>
                    )}
                    {product.categoryName && (
                      <tr>
                        <td>Kategori</td>
                        <td>{product.categoryName}</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>

              {product.ingredients && (
                <div className="product-page__info-ingredients">
                  <h3>İçindekiler</h3>
                  <p>{product.ingredients}</p>
                </div>
              )}
            </div>
          )}

          {/* Ürün Güvenliği */}
          {activeTab === TABS.SAFETY && (
            <div className="product-page__safety">
              <InfoCard
                icon="fa-shield-alt"
                title="Ürün Güvenliği"
                variant="success"
              >
                <ul>
                  <li>
                    Ürünlerimiz TSE ve ISO standartlarına uygun olarak
                    üretilmektedir.
                  </li>
                  <li>
                    Tüm gıda ürünlerimiz HACCP standartlarına uygun tesislerde
                    işlenmektedir.
                  </li>
                  <li>
                    Soğuk zincir gerektiren ürünler, özel araçlarla
                    taşınmaktadır.
                  </li>
                  <li>
                    Ürün son kullanma tarihleri düzenli olarak kontrol
                    edilmektedir.
                  </li>
                </ul>
              </InfoCard>

              {product.allergens && (
                <InfoCard
                  icon="fa-exclamation-triangle"
                  title="Alerjen Uyarısı"
                  variant="danger"
                >
                  <p>{product.allergens}</p>
                </InfoCard>
              )}

              <InfoCard
                icon="fa-thermometer-half"
                title="Saklama Koşulları"
                variant="info"
              >
                <p>
                  {product.storageConditions ||
                    "Ürünü serin ve kuru bir ortamda, direkt güneş ışığından uzak tutunuz. Açıldıktan sonra buzdolabında saklayınız ve kısa sürede tüketiniz."}
                </p>
              </InfoCard>
            </div>
          )}

          {/* Ürün Paketi */}
          {activeTab === TABS.PACKAGE && (
            <div className="product-page__package">
              <div className="product-page__package-info">
                <h3>Paket Bilgileri</h3>
                <table className="product-page__info-table">
                  <tbody>
                    <tr>
                      <td>Paket İçeriği</td>
                      <td>{product.packageContent || "1 adet"}</td>
                    </tr>
                    <tr>
                      <td>Paket Boyutu</td>
                      <td>{product.packageSize || "Standart"}</td>
                    </tr>
                    {product.packageWeight && (
                      <tr>
                        <td>Paket Ağırlığı</td>
                        <td>{product.packageWeight}</td>
                      </tr>
                    )}
                    {product.packageDimensions && (
                      <tr>
                        <td>Paket Boyutları</td>
                        <td>{product.packageDimensions}</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>

              <InfoCard
                icon="fa-recycle"
                title="Geri Dönüşüm Bilgisi"
                variant="success"
              >
                <p>
                  {product.recyclingInfo ||
                    "Ambalajı çöpe atmadan önce geri dönüşüm kutusuna atınız. Çevreyi korumak hepimizin sorumluluğudur."}
                </p>
              </InfoCard>
            </div>
          )}

          {/* İade Koşulları */}
          {activeTab === TABS.RETURN && (
            <div className="product-page__return">
              <InfoCard
                icon="fa-undo-alt"
                title="İade ve Değişim Koşulları"
                variant="info"
              >
                <ul>
                  <li>
                    Gıda ürünlerinde ambalajı açılmamış ve son kullanma tarihi
                    geçmemiş ürünler iade edilebilir.
                  </li>
                  <li>
                    İade taleplerinizi sipariş teslimatından itibaren 14 gün
                    içinde oluşturabilirsiniz.
                  </li>
                  <li>
                    Hasarlı veya hatalı ürünler için fotoğraflı bildirim
                    yapmanız gerekmektedir.
                  </li>
                  <li>
                    İade işlemleri onaylandıktan sonra 3-5 iş günü içinde ödeme
                    iadesi yapılır.
                  </li>
                </ul>
              </InfoCard>

              <InfoCard
                icon="fa-headset"
                title="Müşteri Hizmetleri"
                variant="primary"
              >
                <p>
                  İade ve değişim talepleriniz için müşteri hizmetlerimize
                  ulaşabilirsiniz.
                </p>
                <div className="product-page__contact-info">
                  <p>
                    <i className="fas fa-phone"></i> +90 533 478 30 72
                  </p>
                  <p>
                    <i className="fas fa-envelope"></i>{" "}
                    golturkbuku@golkoygurme.com.tr
                  </p>
                  <p>
                    <i className="fas fa-clock"></i> Hafta içi 08:30 - 17:30
                  </p>
                </div>
              </InfoCard>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
