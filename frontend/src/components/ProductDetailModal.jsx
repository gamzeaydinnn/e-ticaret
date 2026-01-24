/**
 * ProductDetailModal.jsx
 *
 * Modern, detaylı ürün bilgi modalı.
 * Migros/A101 benzeri tasarımla ürün besin değerleri, stok durumu,
 * fiyat ve indirim bilgilerini gösteren sekme yapısına sahip modal.
 *
 * Özellikler:
 * - Mobil uyumlu (responsive) tasarım
 * - Besin değerleri tablosu (100g/ml bazında)
 * - Ürün bilgileri ve açıklama
 * - İade koşulları ve güvenlik bilgileri
 * - Favori ve karşılaştırma özellikleri
 * - Miktar seçici (kg/adet destekli)
 * - Animasyonlu açılış/kapanış
 *
 * @author Senior E-Ticaret Team
 * @version 2.0.0
 */

import React, { useState, useEffect, useCallback, useMemo } from "react";
import { useCart } from "../contexts/CartContext";
import { useFavorites } from "../contexts/FavoriteContext";
import { useCompare } from "../contexts/CompareContext";
import { useAuth } from "../contexts/AuthContext";
import getProductCategoryRules from "../config/productCategoryRules";
import "./ProductDetailModal.css";

// Varsayılan besin değerleri (ürüne özgü değilse gösterilir)
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

// Sekme sabitleri
const TABS = {
  NUTRITION: "nutrition",
  INFO: "info",
  SAFETY: "safety",
  PACKAGE: "package",
  RETURN: "return",
};

/**
 * Besin değeri satırı komponenti
 * Tutarlı ve okunabilir tablo satırları için kullanılır
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
 * Uyarı ve bilgilendirme mesajları için
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

/**
 * Ana ProductDetailModal Komponenti
 */
export default function ProductDetailModal({
  product,
  isOpen,
  onClose,
  onAddToCart,
}) {
  // eslint-disable-next-line no-unused-vars
  const { user } = useAuth();
  const { addToCart: ctxAddToCart } = useCart();
  const { toggleFavorite, isFavorite } = useFavorites();
  const { toggleCompare, isInCompare } = useCompare();

  // State tanımlamaları
  const [activeTab, setActiveTab] = useState(TABS.NUTRITION);
  const [quantity, setQuantity] = useState(1);
  const [rule, setRule] = useState(null);
  const [validationError, setValidationError] = useState("");
  const [isClosing, setIsClosing] = useState(false);
  const [imageLoaded, setImageLoaded] = useState(false);
  const [showFullDescription, setShowFullDescription] = useState(false);

  // Ürün kategorisine göre kural yükleme
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

  // Modal açıldığında scroll'u kilitle
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = "hidden";
      setIsClosing(false);
      setActiveTab(TABS.NUTRITION);
      setValidationError("");
      setImageLoaded(false);
    } else {
      document.body.style.overflow = "auto";
    }

    return () => {
      document.body.style.overflow = "auto";
    };
  }, [isOpen]);

  // Animasyonlu kapanış
  const handleClose = useCallback(() => {
    setIsClosing(true);
    setTimeout(() => {
      onClose();
      setIsClosing(false);
    }, 300);
  }, [onClose]);

  // ESC tuşu ile kapatma
  useEffect(() => {
    const handleEscape = (e) => {
      if (e.key === "Escape" && isOpen) {
        handleClose();
      }
    };

    document.addEventListener("keydown", handleEscape);
    return () => document.removeEventListener("keydown", handleEscape);
  }, [isOpen, handleClose]);

  // Miktar değişikliği handler'ı
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

  // Sepete ekleme validasyonu
  const handleAddToCartClick = useCallback(() => {
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
    if (onAddToCart) {
      onAddToCart(product, q);
    } else {
      ctxAddToCart(product, q);
    }

    handleClose();
  }, [quantity, rule, product, onAddToCart, ctxAddToCart, handleClose]);

  // Fiyat hesaplamaları (memoized)
  const priceInfo = useMemo(() => {
    if (!product) return null;

    const basePrice = Number(product.price || 0);
    const specialPrice = Number(
      product.specialPrice ?? product.discountPrice ?? basePrice,
    );
    const hasDiscount =
      !isNaN(specialPrice) && specialPrice > 0 && specialPrice < basePrice;
    const currentPrice = hasDiscount ? specialPrice : basePrice;
    const discountPercent = hasDiscount
      ? Math.round(100 - (currentPrice / basePrice) * 100)
      : 0;

    return { basePrice, currentPrice, hasDiscount, discountPercent };
  }, [product]);

  // Stok durumu
  const stockInfo = useMemo(() => {
    if (!product)
      return { isOutOfStock: false, isLowStock: false, stock: null };

    const stock = product.stock ?? product.stockQuantity ?? null;
    const isOutOfStock = typeof stock === "number" && stock <= 0;
    const isLowStock = typeof stock === "number" && stock > 0 && stock <= 5;

    return { isOutOfStock, isLowStock, stock };
  }, [product]);

  // Besin değerleri (ürüne özgü veya varsayılan)
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

  // Render edilmeyecek durumlar
  if (!isOpen || !product) return null;

  // Breadcrumb oluşturma
  const breadcrumb = [
    "Anasayfa",
    product.categoryName || "Kategori",
    product.subCategoryName || null,
    product.name,
  ].filter(Boolean);

  return (
    <div
      className={`pdm-overlay ${isClosing ? "pdm-overlay--closing" : ""}`}
      onClick={(e) => e.target === e.currentTarget && handleClose()}
      role="dialog"
      aria-modal="true"
      aria-labelledby="product-detail-title"
    >
      <div className={`pdm-modal ${isClosing ? "pdm-modal--closing" : ""}`}>
        {/* Kapatma butonu */}
        <button
          className="pdm-close-btn"
          onClick={handleClose}
          aria-label="Kapat"
        >
          <i className="fas fa-times"></i>
        </button>

        {/* Breadcrumb */}
        <div className="pdm-breadcrumb">
          {breadcrumb.map((item, index) => (
            <span key={index}>
              {index > 0 && (
                <i className="fas fa-chevron-right pdm-breadcrumb__separator"></i>
              )}
              <span
                className={
                  index === breadcrumb.length - 1
                    ? "pdm-breadcrumb__current"
                    : ""
                }
              >
                {item}
              </span>
            </span>
          ))}
        </div>

        {/* Ana içerik */}
        <div className="pdm-content">
          {/* Sol: Ürün görseli */}
          <div className="pdm-image-section">
            <div className="pdm-image-container">
              {!imageLoaded && (
                <div className="pdm-image-skeleton">
                  <i className="fas fa-image"></i>
                </div>
              )}
              <img
                src={product.imageUrl || "/images/placeholder.png"}
                alt={product.name}
                className={`pdm-image ${imageLoaded ? "pdm-image--loaded" : ""}`}
                onLoad={() => setImageLoaded(true)}
                onError={(e) => {
                  e.target.src = "/images/placeholder.png";
                  setImageLoaded(true);
                }}
              />

              {/* İndirim rozeti */}
              {priceInfo?.hasDiscount && (
                <div className="pdm-discount-badge">
                  <span>%{priceInfo.discountPercent}</span>
                  <small>İNDİRİM</small>
                </div>
              )}

              {/* Yeni ürün rozeti */}
              {product.isNew && <div className="pdm-new-badge">YENİ</div>}
            </div>

            {/* Hızlı aksiyonlar (mobil için) */}
            <div className="pdm-quick-actions pdm-quick-actions--mobile">
              <button
                className={`pdm-action-btn ${isFavorite(product.id) ? "pdm-action-btn--active" : ""}`}
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
                className={`pdm-action-btn ${isInCompare(product.id) ? "pdm-action-btn--active" : ""}`}
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
          </div>

          {/* Sağ: Ürün bilgileri */}
          <div className="pdm-info-section">
            {/* Marka */}
            {product.brand && (
              <span className="pdm-brand">{product.brand}</span>
            )}

            {/* Ürün adı */}
            <h1 id="product-detail-title" className="pdm-title">
              {product.name}
            </h1>

            {/* Değerlendirme */}
            {product.rating > 0 && (
              <div className="pdm-rating">
                <div className="pdm-rating__stars">
                  {[...Array(5)].map((_, i) => (
                    <i
                      key={i}
                      className={`fas fa-star ${
                        i < Math.floor(product.rating)
                          ? "pdm-rating__star--filled"
                          : ""
                      }`}
                    ></i>
                  ))}
                </div>
                <span className="pdm-rating__count">
                  ({product.reviewCount || 0} değerlendirme)
                </span>
              </div>
            )}

            {/* Fiyat bilgisi */}
            <div className="pdm-price-section">
              {priceInfo?.hasDiscount && (
                <span className="pdm-price--original">
                  {priceInfo.basePrice.toFixed(2)} <small>TL</small>
                </span>
              )}

              {/* Kampanyalı fiyat kutusu */}
              {priceInfo?.hasDiscount && (
                <div className="pdm-special-price-box">
                  <span className="pdm-special-price-label">
                    <i className="fas fa-tag"></i> İndirimli
                  </span>
                  <span className="pdm-special-price-value">
                    {priceInfo.currentPrice.toFixed(2)} <small>TL</small>
                  </span>
                </div>
              )}

              {!priceInfo?.hasDiscount && (
                <span className="pdm-price--current">
                  {priceInfo?.currentPrice.toFixed(2)} <small>TL</small>
                </span>
              )}

              {/* Birim fiyatı */}
              {product.unitPrice && (
                <span className="pdm-unit-price">
                  ({product.unitPrice.toFixed(2)} TL/{product.unit || "Litre"})
                </span>
              )}
            </div>

            {/* Miktar seçici */}
            <div className="pdm-quantity-section">
              <label className="pdm-quantity-label">Adet</label>
              <div className="pdm-quantity-controls">
                <button
                  className="pdm-quantity-btn"
                  onClick={() => handleQuantityChange(-1)}
                  disabled={quantity <= (rule?.min_quantity || 1)}
                  aria-label="Azalt"
                >
                  <i className="fas fa-minus"></i>
                </button>
                <input
                  type="number"
                  className="pdm-quantity-input"
                  value={quantity}
                  onChange={(e) => setQuantity(e.target.value)}
                  min={rule?.min_quantity || 1}
                  max={rule?.max_quantity || 99}
                  step={rule?.step ?? (rule?.unit === "kg" ? 0.25 : 1)}
                />
                <button
                  className="pdm-quantity-btn"
                  onClick={() => handleQuantityChange(1)}
                  disabled={quantity >= (rule?.max_quantity || 99)}
                  aria-label="Artır"
                >
                  <i className="fas fa-plus"></i>
                </button>
                <span className="pdm-quantity-unit">
                  {rule?.unit || product.unit || "adet"}
                </span>
              </div>

              {/* Miktar kuralı bilgisi */}
              {rule && (
                <div className="pdm-quantity-info">
                  <i className="fas fa-info-circle"></i>
                  Min: {rule.min_quantity} — Max: {rule.max_quantity} — Adım:{" "}
                  {rule.step ?? 1} {rule.unit}
                </div>
              )}

              {/* Validasyon hatası */}
              {validationError && (
                <div className="pdm-validation-error">
                  <i className="fas fa-exclamation-circle"></i>
                  {validationError}
                </div>
              )}
            </div>

            {/* Sepete ekle ve favori butonları */}
            <div className="pdm-actions">
              <button
                className={`pdm-add-to-cart ${stockInfo.isOutOfStock ? "pdm-add-to-cart--disabled" : ""}`}
                onClick={handleAddToCartClick}
                disabled={stockInfo.isOutOfStock}
              >
                <i className="fas fa-shopping-cart"></i>
                {stockInfo.isOutOfStock ? "Stokta Yok" : "Sepete Ekle"}
              </button>

              <button
                className={`pdm-favorite-btn ${isFavorite(product.id) ? "pdm-favorite-btn--active" : ""}`}
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
            </div>

            {/* Stok uyarıları */}
            <div className="pdm-stock-alerts">
              {stockInfo.stock !== null && !stockInfo.isOutOfStock && (
                <div
                  className={`pdm-stock-info ${stockInfo.isLowStock ? "pdm-stock-info--low" : ""}`}
                >
                  <i
                    className={`fas ${stockInfo.isLowStock ? "fa-exclamation-triangle" : "fa-check-circle"}`}
                  ></i>
                  {stockInfo.isLowStock
                    ? `Bu fiyattan satılmak üzere toplam ${stockInfo.stock} adet stok bulunmaktadır.`
                    : `Bu fiyattan satılmak üzere toplam ${stockInfo.stock} adet stok bulunmaktadır.`}
                </div>
              )}

              {product.maxOrderQuantity && (
                <div className="pdm-order-limit">
                  <i className="fas fa-info-circle"></i>
                  Bu üründen en fazla {product.maxOrderQuantity} adet sipariş
                  verilebilir. Belirtilen adet üzerindeki siparişlerin iptal
                  edilmesi hakkı saklıdır.
                </div>
              )}

              <div className="pdm-store-info">
                <i className="fas fa-store"></i>
                Ürünün stok, fiyat ve kampanya bilgisi, teslimatı
                gerçekleştirecek mağazanın stok, fiyat ve kampanya bilgilerine
                göre belirlenmektedir.
              </div>
            </div>
          </div>
        </div>

        {/* Sekmeler */}
        <div className="pdm-tabs-section">
          <div className="pdm-tabs">
            <button
              className={`pdm-tab ${activeTab === TABS.NUTRITION ? "pdm-tab--active" : ""}`}
              onClick={() => setActiveTab(TABS.NUTRITION)}
            >
              Besin Değerleri
            </button>
            <button
              className={`pdm-tab ${activeTab === TABS.INFO ? "pdm-tab--active" : ""}`}
              onClick={() => setActiveTab(TABS.INFO)}
            >
              Ürün Bilgileri
            </button>
            <button
              className={`pdm-tab ${activeTab === TABS.SAFETY ? "pdm-tab--active" : ""}`}
              onClick={() => setActiveTab(TABS.SAFETY)}
            >
              Ürün Güvenliği
            </button>
            <button
              className={`pdm-tab ${activeTab === TABS.PACKAGE ? "pdm-tab--active" : ""}`}
              onClick={() => setActiveTab(TABS.PACKAGE)}
            >
              Ürün Paketi
            </button>
            <button
              className={`pdm-tab ${activeTab === TABS.RETURN ? "pdm-tab--active" : ""}`}
              onClick={() => setActiveTab(TABS.RETURN)}
            >
              İade Koşulları
            </button>
          </div>

          {/* Sekme içerikleri */}
          <div className="pdm-tab-content">
            {/* Besin Değerleri */}
            {activeTab === TABS.NUTRITION && (
              <div className="pdm-nutrition">
                <div className="pdm-nutrition__table-wrapper">
                  <table className="pdm-nutrition__table">
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
                      <NutritionRow
                        label="Yağ (g)"
                        value={nutritionValues.fat}
                      />
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
                    online satış kanallarımızda bulunan bilgiler arasında
                    herhangi bir farklılık bulunması halinde sorumluluk tamamen
                    tedarikçi firmaya aittir.
                  </p>
                </InfoCard>
              </div>
            )}

            {/* Ürün Bilgileri */}
            {activeTab === TABS.INFO && (
              <div className="pdm-info">
                <div className="pdm-info__description">
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
                        className="pdm-read-more"
                        onClick={() =>
                          setShowFullDescription(!showFullDescription)
                        }
                      >
                        {showFullDescription
                          ? "Daha az göster"
                          : "Devamını oku"}
                      </button>
                    )}
                  </p>
                </div>

                <div className="pdm-info__details">
                  <h3>Ürün Detayları</h3>
                  <table className="pdm-info__table">
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
                  <div className="pdm-info__ingredients">
                    <h3>İçindekiler</h3>
                    <p>{product.ingredients}</p>
                  </div>
                )}
              </div>
            )}

            {/* Ürün Güvenliği */}
            {activeTab === TABS.SAFETY && (
              <div className="pdm-safety">
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
              <div className="pdm-package">
                <div className="pdm-package__info">
                  <h3>Paket Bilgileri</h3>
                  <table className="pdm-info__table">
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
              <div className="pdm-return">
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
                      İade işlemleri onaylandıktan sonra 3-5 iş günü içinde
                      ödeme iadesi yapılır.
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
                  <div className="pdm-contact-info">
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
    </div>
  );
}
