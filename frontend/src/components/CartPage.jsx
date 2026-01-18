import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { useCart } from "../contexts/CartContext";
import { CartService } from "../services/cartService";
import { ProductService } from "../services/productService";
import "./CartPage.css";

const CartPage = () => {
  const {
    cartItems,
    loading: cartLoading,
    updateQuantity,
    removeFromCart,
    getCartTotal,
  } = useCart();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [products, setProducts] = useState({});
  const { user } = useAuth();
  const [shippingMethod, setShippingMethod] = useState(() => {
    try {
      return CartService.getShippingMethod();
    } catch {
      return "motorcycle";
    }
  });
  const [couponCode, setCouponCode] = useState("");
  const [pricing, setPricing] = useState(null);
  const [pricingLoading, setPricingLoading] = useState(false);
  const [pricingError, setPricingError] = useState("");
  const [couponSuccess, setCouponSuccess] = useState("");
  const [appliedCoupon, setAppliedCoupon] = useState(null);

  // Sayfa yüklendiğinde önceden uygulanmış kupon varsa geri yükle
  useEffect(() => {
    const savedCoupon = CartService.getAppliedCoupon();
    if (savedCoupon) {
      setAppliedCoupon(savedCoupon);
      setCouponCode(savedCoupon.code || "");
    }
  }, []);

  // Ürün detaylarını yükle
  const loadProductData = useCallback(async () => {
    if (cartItems.length === 0) {
      setProducts({});
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      let allProducts = [];
      let productData = {};

      try {
        allProducts = await ProductService.list();
      } catch (error) {
        allProducts = [];
      }

      for (const item of cartItems) {
        const pid = item.productId || item.id;
        const product = allProducts.find(
          (p) => p.id === pid || String(p.id) === String(pid),
        );

        if (product) {
          productData[pid] = {
            ...product,
            imageUrl: product.imageUrl || `/images/placeholder.png`,
          };
        } else if (item.product) {
          productData[pid] = item.product;
        } else {
          productData[pid] = {
            id: pid,
            name: "Ürün",
            price: item.unitPrice || 0,
            imageUrl: "/images/placeholder.png",
          };
        }
      }

      setProducts(productData);
    } catch (error) {
      console.error("Ürün bilgileri alınırken hata:", error);
    } finally {
      setLoading(false);
    }
  }, [cartItems]);

  useEffect(() => {
    loadProductData();
  }, [loadProductData]);

  const getItemUnitPrice = (item) => {
    if (
      item?.unitPrice !== undefined &&
      item?.unitPrice !== null &&
      !Number.isNaN(Number(item.unitPrice))
    ) {
      return Number(item.unitPrice);
    }
    const pid = item.productId || item.id;
    const product = products[pid] || item.product;
    const fallback = (product && (product.specialPrice || product.price)) || 0;
    return Number(fallback) || 0;
  };

  const handleUpdateQuantity = (item, newQuantity) => {
    if (newQuantity < 1) {
      handleRemoveItem(item);
      return;
    }
    const pid = item.productId || item.id;
    updateQuantity(pid, newQuantity);
  };

  const handleRemoveItem = (item) => {
    const pid = item.productId || item.id;
    removeFromCart(pid);
  };

  const getTotalPrice = () => {
    return getCartTotal();
  };

  const getShippingCost = () => {
    if (
      appliedCoupon?.couponType === "FreeShipping" ||
      appliedCoupon?.type === 4
    ) {
      return 0;
    }
    return shippingMethod === "car" ? 30 : 15;
  };

  const getCouponTypeName = (type) => {
    const types = {
      0: "Yüzde",
      1: "Sabit",
      2: "İlk Sipariş",
      3: "X Al Y Öde",
      4: "Ücretsiz Kargo",
      Percentage: "Yüzde",
      FixedAmount: "Sabit",
      FirstOrder: "İlk Sipariş",
      BuyXGetY: "X Al Y Öde",
      FreeShipping: "Ücretsiz Kargo",
    };
    return types[type] || "İndirim";
  };

  const handleApplyCoupon = async () => {
    setPricingError("");
    setCouponSuccess("");
    setPricingLoading(true);

    const code = couponCode?.trim();
    if (!code) {
      setPricingError("Lütfen bir kupon kodu girin.");
      setPricingLoading(false);
      return;
    }

    try {
      const subtotal = getTotalPrice();
      const itemsPayload = cartItems.map((item) => ({
        productId: item.productId || item.id,
        quantity: item.quantity,
        unitPrice: getItemUnitPrice(item),
        categoryId:
          products[item.productId || item.id]?.categoryId ||
          products[item.productId || item.id]?.category?.id ||
          item.categoryId ||
          item?.product?.categoryId ||
          item?.product?.category?.id,
      }));
      const shippingCost = getShippingCost();

      const validationResult = await CartService.validateCoupon(
        code,
        itemsPayload,
        subtotal,
        shippingCost,
      );

      if (validationResult.isValid) {
        const discountAmount =
          validationResult.discountAmount ??
          validationResult.calculatedDiscount ??
          validationResult.discount ??
          0;
        const couponData = {
          code: code,
          couponType: validationResult.couponType,
          discountAmount,
          finalTotal: validationResult.finalTotal ?? validationResult.finalPrice,
          message: validationResult.message || validationResult.errorMessage,
        };
        setAppliedCoupon(couponData);
        CartService.setAppliedCoupon(couponData);
        setCouponSuccess(
          validationResult.message ||
            `${Number(discountAmount || 0).toFixed(2)}₺ indirim uygulandı!`,
        );

        try {
          const pricingResult = await CartService.previewPrice({
            items: cartItems.map((item) => ({
              productId: item.productId || item.id,
              quantity: item.quantity,
            })),
            couponCode: code,
          });
          setPricing(pricingResult);
        } catch (e) {
          console.log("Price preview hatası:", e);
        }
      } else {
        setPricingError(
          validationResult.message || "Kupon geçersiz veya kullanılamıyor.",
        );
        setAppliedCoupon(null);
        CartService.clearAppliedCoupon();
      }
    } catch (error) {
      console.error("Kupon uygulanırken hata:", error);
      try {
        const result = await CartService.previewPrice({
          items: cartItems.map((item) => ({
            productId: item.productId || item.id,
            quantity: item.quantity,
          })),
          couponCode: code,
        });
        setPricing(result);
        if (result.couponDiscountTotal > 0 || result.appliedCouponCode) {
          const couponData = {
            code: result.appliedCouponCode || code,
            discountAmount: result.couponDiscountTotal,
          };
          setAppliedCoupon(couponData);
          CartService.setAppliedCoupon(couponData);
          setCouponSuccess(
            `${result.couponDiscountTotal?.toFixed(2)}₺ indirim uygulandı!`,
          );
        } else {
          setPricingError("Bu kupon sepetiniz için geçerli değil.");
        }
      } catch (fallbackError) {
        setPricingError("Kupon geçersiz veya kullanılamıyor.");
      }
    } finally {
      setPricingLoading(false);
    }
  };

  const handleRemoveCoupon = () => {
    setAppliedCoupon(null);
    CartService.clearAppliedCoupon();
    setCouponCode("");
    setCouponSuccess("");
    setPricingError("");
    setPricing(null);
  };

  const getDiscountAmount = () => {
    if (appliedCoupon?.discountAmount) return appliedCoupon.discountAmount;
    if (pricing?.couponDiscountTotal) return pricing.couponDiscountTotal;
    return 0;
  };

  const getFinalTotal = () => {
    const subtotal = getTotalPrice();
    const shipping = getShippingCost();
    const discount = getDiscountAmount();
    return Math.max(0, subtotal + shipping - discount);
  };

  // Empty Cart State
  if (!loading && cartItems.length === 0) {
    return (
      <div className="cart-page">
        <div className="container py-5">
          <div className="empty-cart-container">
            <div className="empty-cart-icon">
              <i className="fas fa-shopping-cart"></i>
            </div>
            <h2>Sepetiniz Boş</h2>
            <p>Henüz sepetinize ürün eklemediniz. Hemen alışverişe başlayın!</p>
            <Link to="/" className="btn-shop-now">
              <i className="fas fa-store me-2"></i>
              Alışverişe Başla
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="cart-page">
      <div className="container py-4">
        {/* Header */}
        <div className="cart-header">
          <div className="cart-header-left">
            <i className="fas fa-shopping-cart"></i>
            <div>
              <h1>Sepetim</h1>
              <span className="item-count">
                {cartItems.reduce((total, item) => total + item.quantity, 0)}{" "}
                ürün
              </span>
            </div>
          </div>
          <Link to="/" className="continue-shopping">
            <i className="fas fa-arrow-left me-2"></i>
            Alışverişe Devam Et
          </Link>
        </div>

        <div className="cart-content">
          {/* Cart Items */}
          <div className="cart-items-section">
            {loading ? (
              <div className="loading-state">
                <div className="spinner"></div>
                <p>Sepet yükleniyor...</p>
              </div>
            ) : (
              <div className="cart-items-list">
                {cartItems.map((item) => {
                  const product = products[item.productId];
                  return (
                    <div key={item.id || item.productId} className="cart-item">
                      <div className="item-image">
                        <img
                          src={product?.imageUrl || "/images/placeholder.png"}
                          alt={product?.name || "Ürün"}
                          onError={(e) => {
                            e.target.src = "/images/placeholder.png";
                          }}
                        />
                      </div>

                      <div className="item-details">
                        <h3 className="item-name">
                          {product?.name || "Yükleniyor..."}
                        </h3>
                        {(item.variantTitle || item.sku) && (
                          <div className="item-variants">
                            {item.variantTitle && (
                              <span className="variant-badge">
                                {item.variantTitle}
                              </span>
                            )}
                            {item.sku && (
                              <span className="sku-badge">SKU: {item.sku}</span>
                            )}
                          </div>
                        )}
                        <div className="item-price-mobile">
                          ₺{getItemUnitPrice(item).toFixed(2)}
                        </div>
                      </div>

                      <div className="item-quantity">
                        <button
                          className="qty-btn minus"
                          onClick={() =>
                            handleUpdateQuantity(item, item.quantity - 1)
                          }
                        >
                          <i className="fas fa-minus"></i>
                        </button>
                        <span className="qty-value">{item.quantity}</span>
                        <button
                          className="qty-btn plus"
                          onClick={() =>
                            handleUpdateQuantity(item, item.quantity + 1)
                          }
                        >
                          <i className="fas fa-plus"></i>
                        </button>
                      </div>

                      <div className="item-total">
                        <span className="price">
                          ₺{(getItemUnitPrice(item) * item.quantity).toFixed(2)}
                        </span>
                        <button
                          className="remove-btn"
                          onClick={() => handleRemoveItem(item)}
                          title="Ürünü Kaldır"
                        >
                          <i className="fas fa-trash-alt"></i>
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Order Summary */}
          <div className="order-summary-section">
            <div className="order-summary-card">
              <h2>
                <i className="fas fa-receipt me-2"></i>
                Sipariş Özeti
              </h2>

              {/* Coupon Section */}
              <div className="coupon-section">
                <label>
                  <i className="fas fa-tag me-2"></i>
                  İndirim Kuponu
                </label>

                {appliedCoupon ? (
                  <div className="applied-coupon">
                    <div className="coupon-info">
                      <div className="coupon-badge">
                        <i className="fas fa-check-circle"></i>
                        <span className="coupon-code">
                          {appliedCoupon.code}
                        </span>
                      </div>
                      <span className="coupon-type">
                        {getCouponTypeName(appliedCoupon.couponType)}
                      </span>
                    </div>
                    <div className="coupon-discount">
                      {appliedCoupon.couponType === "FreeShipping" ||
                      appliedCoupon.type === 4 ? (
                        <span className="free-shipping">Ücretsiz Kargo</span>
                      ) : (
                        <span className="discount-amount">
                          -₺{appliedCoupon.discountAmount?.toFixed(2)}
                        </span>
                      )}
                      <button
                        className="remove-coupon"
                        onClick={handleRemoveCoupon}
                      >
                        <i className="fas fa-times"></i>
                      </button>
                    </div>
                  </div>
                ) : (
                  <div className="coupon-input-group">
                    <input
                      type="text"
                      placeholder="Kupon kodunuz"
                      value={couponCode}
                      onChange={(e) =>
                        setCouponCode(e.target.value.toUpperCase())
                      }
                      onKeyPress={(e) =>
                        e.key === "Enter" && handleApplyCoupon()
                      }
                    />
                    <button
                      onClick={handleApplyCoupon}
                      disabled={pricingLoading || !couponCode.trim()}
                      className="apply-coupon-btn"
                    >
                      {pricingLoading ? (
                        <i className="fas fa-spinner fa-spin"></i>
                      ) : (
                        "Uygula"
                      )}
                    </button>
                  </div>
                )}

                {couponSuccess && (
                  <div className="coupon-message success">
                    <i className="fas fa-check-circle"></i>
                    {couponSuccess}
                  </div>
                )}

                {pricingError && (
                  <div className="coupon-message error">
                    <i className="fas fa-exclamation-circle"></i>
                    {pricingError}
                  </div>
                )}
              </div>

              {/* Shipping Selection */}
              <div className="shipping-section">
                <label>
                  <i className="fas fa-truck me-2"></i>
                  Teslimat Yöntemi
                </label>

                <div className="shipping-options">
                  <div
                    className={`shipping-option ${shippingMethod === "motorcycle" ? "selected" : ""}`}
                    onClick={() => {
                      setShippingMethod("motorcycle");
                      CartService.setShippingMethod("motorcycle");
                    }}
                  >
                    <div className="shipping-icon">
                      <i className="fas fa-motorcycle"></i>
                    </div>
                    <div className="shipping-details">
                      <span className="shipping-name">Hızlı Teslimat</span>
                      <span className="shipping-desc">30-45 dakika içinde</span>
                    </div>
                    <div className="shipping-price">
                      {appliedCoupon?.couponType === "FreeShipping" ? (
                        <>
                          <span className="original-price">₺15</span>
                          <span className="free">Ücretsiz</span>
                        </>
                      ) : (
                        <span>₺15</span>
                      )}
                    </div>
                    <div className="check-icon">
                      <i className="fas fa-check-circle"></i>
                    </div>
                  </div>

                  <div
                    className={`shipping-option ${shippingMethod === "car" ? "selected" : ""}`}
                    onClick={() => {
                      setShippingMethod("car");
                      CartService.setShippingMethod("car");
                    }}
                  >
                    <div className="shipping-icon">
                      <i className="fas fa-car"></i>
                    </div>
                    <div className="shipping-details">
                      <span className="shipping-name">Standart Teslimat</span>
                      <span className="shipping-desc">
                        Büyük siparişler için ideal
                      </span>
                    </div>
                    <div className="shipping-price">
                      {appliedCoupon?.couponType === "FreeShipping" ? (
                        <>
                          <span className="original-price">₺30</span>
                          <span className="free">Ücretsiz</span>
                        </>
                      ) : (
                        <span>₺30</span>
                      )}
                    </div>
                    <div className="check-icon">
                      <i className="fas fa-check-circle"></i>
                    </div>
                  </div>
                </div>
              </div>

              {/* Price Summary */}
              <div className="price-summary">
                <div className="summary-row">
                  <span>Ara Toplam</span>
                  <span>₺{getTotalPrice().toFixed(2)}</span>
                </div>

                {getDiscountAmount() > 0 && (
                  <div className="summary-row discount">
                    <span>
                      <i className="fas fa-tag me-1"></i>
                      İndirim
                    </span>
                    <span>-₺{getDiscountAmount().toFixed(2)}</span>
                  </div>
                )}

                <div className="summary-row">
                  <span>Teslimat</span>
                  <span className={getShippingCost() === 0 ? "free-text" : ""}>
                    {getShippingCost() === 0
                      ? "Ücretsiz"
                      : `₺${getShippingCost().toFixed(2)}`}
                  </span>
                </div>

                <div className="summary-row total">
                  <span>Toplam</span>
                  <span>₺{getFinalTotal().toFixed(2)}</span>
                </div>
              </div>

              {/* Checkout Button */}
              <button
                className="checkout-btn"
                onClick={() => navigate("/payment")}
                disabled={cartItems.length === 0}
              >
                <i className="fas fa-lock me-2"></i>
                Güvenli Ödemeye Geç
                <i className="fas fa-arrow-right ms-2"></i>
              </button>

              {/* Trust Badges */}
              <div className="trust-badges">
                <div className="badge-item">
                  <i className="fas fa-shield-alt"></i>
                  <span>256-bit SSL</span>
                </div>
                <div className="badge-item">
                  <i className="fas fa-undo"></i>
                  <span>Kolay İade</span>
                </div>
                <div className="badge-item">
                  <i className="fas fa-headset"></i>
                  <span>7/24 Destek</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CartPage;
