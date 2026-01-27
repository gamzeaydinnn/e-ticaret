import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { useCart } from "../contexts/CartContext";
import { CartService } from "../services/cartService";
import { OrderService } from "../services/orderService";
import { PaymentService } from "../services/paymentService";
import { ProductService } from "../services/productService";
import { CampaignService } from "../services/campaignService";
import shippingService from "../services/shippingService";
import LoginModal from "./LoginModal";
import { CreditCardPreview } from "./payment";
import "./PaymentPage.css";

// UUID v4 generator for clientOrderId
const generateUUID = () => {
  try {
    if (
      typeof crypto !== "undefined" &&
      typeof crypto.randomUUID === "function"
    ) {
      return crypto.randomUUID();
    }
  } catch {
    // ignore
  }
  // Fallback UUID v4 generator
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
};

const PaymentPage = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { cartItems: contextCartItems, getCartTotal, clearCart } = useCart();

  const [cartItems, setCartItems] = useState([]);
  const [products, setProducts] = useState({});
  const [loading, setLoading] = useState(true);
  const [processing, setProcessing] = useState(false);
  const [showLoginModal, setShowLoginModal] = useState(false);

  // Teslimat
  const [shippingMethod, setShippingMethod] = useState(() => {
    try {
      return CartService.getShippingMethod() || "motorcycle";
    } catch {
      return "motorcycle";
    }
  });

  // Dinamik kargo fiyatları (API'den)
  const [shippingPrices, setShippingPrices] = useState({
    motorcycle: 15, // varsayılan değerler
    car: 30,
  });
  const [shippingPricesLoading, setShippingPricesLoading] = useState(true);

  // Kupon sistemi
  const [couponCode, setCouponCode] = useState("");
  const [appliedCoupon, setAppliedCoupon] = useState(null);
  const [couponLoading, setCouponLoading] = useState(false);
  const [couponError, setCouponError] = useState("");
  const [couponSuccess, setCouponSuccess] = useState("");

  // =================================================================
  // KAMPANYA SİSTEMİ STATE'LERİ
  // =================================================================
  const [appliedCampaigns, setAppliedCampaigns] = useState([]);
  const [campaignDiscountTotal, setCampaignDiscountTotal] = useState(0);
  const [pricing, setPricing] = useState(null);
  const [pricingLoading, setPricingLoading] = useState(false);
  const [pricingError, setPricingError] = useState("");

  // Form verileri
  const [formData, setFormData] = useState({
    cardNumber: "",
    cardName: "",
    expiryMonth: "",
    expiryYear: "",
    cvv: "",
    firstName: "",
    lastName: "",
    email: "",
    phone: "",
    address: "",
    city: "",
    district: "",
    postalCode: "",
  });

  const [errors, setErrors] = useState({});
  const [paymentError, setPaymentError] = useState("");
  const [deliverySlot, setDeliverySlot] = useState("standard");
  const [deliveryNote, setDeliveryNote] = useState("");
  const [isCardFlipped, setIsCardFlipped] = useState(false); // CVV için kart çevirme

  // Client Order ID - proper UUID v4 format for backend GUID deserialization
  const [clientOrderId] = useState(() => generateUUID());

  // Kargo fiyatlarını API'den yükle
  useEffect(() => {
    const loadShippingPrices = async () => {
      try {
        setShippingPricesLoading(true);
        const settings = await shippingService.getActiveSettings();
        if (settings && settings.length > 0) {
          const prices = {};
          settings.forEach((setting) => {
            prices[setting.vehicleType] = setting.price;
          });
          setShippingPrices((prev) => ({
            ...prev,
            ...prices,
          }));
        }
      } catch (error) {
        console.warn(
          "Kargo fiyatları yüklenemedi, varsayılan değerler kullanılıyor:",
          error,
        );
      } finally {
        setShippingPricesLoading(false);
      }
    };
    loadShippingPrices();
  }, []);

  // Önceden uygulanmış kupon varsa yükle
  useEffect(() => {
    const savedCoupon = CartService.getAppliedCoupon();
    if (savedCoupon) {
      setAppliedCoupon(savedCoupon);
      setCouponCode(savedCoupon.code || "");
    }
  }, []);

  // Sepeti yükle
  useEffect(() => {
    loadCartItems();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Kampanya/kupon önizlemesi (ödeme sayfasında görünürlük için)
  useEffect(() => {
    let mounted = true;

    const loadPricingPreview = async () => {
      if (!cartItems || cartItems.length === 0) {
        setPricing(null);
        setAppliedCampaigns([]);
        setCampaignDiscountTotal(0);
        return;
      }

      setPricingLoading(true);
      setPricingError("");
      try {
        const result = await CartService.previewPrice({
          items: cartItems.map((item) => ({
            productId: item.productId || item.id,
            quantity: item.quantity,
          })),
          couponCode: appliedCoupon?.code || undefined,
        });

        if (!mounted) return;
        setPricing(result);
        setAppliedCampaigns(result?.appliedCampaigns || []);
        setCampaignDiscountTotal(result?.campaignDiscountTotal || 0);
      } catch (err) {
        if (!mounted) return;
        setPricingError("Kampanya bilgileri alınamadı.");
      } finally {
        if (mounted) setPricingLoading(false);
      }
    };

    loadPricingPreview();

    return () => {
      mounted = false;
    };
  }, [cartItems, appliedCoupon?.code]);

  useEffect(() => {
    if (!user) {
      setCartItems(contextCartItems);
    }
  }, [contextCartItems, user]);

  const loadCartItems = async () => {
    try {
      let items = [];
      if (user) {
        try {
          const serverItems = await CartService.getCartItems();
          items = Array.isArray(serverItems) ? serverItems : [];
        } catch {
          items = [];
        }
        if (!items.length && contextCartItems.length) {
          items = contextCartItems;
        }
      } else {
        items = contextCartItems.length
          ? contextCartItems
          : CartService.getGuestCart();
      }
      setCartItems(items);

      // Ürün detayları
      try {
        const allProducts = await ProductService.list();
        const map = {};
        for (const p of allProducts) map[p.id] = p;
        setProducts(map);
      } catch {
        setProducts({});
      }
    } finally {
      setLoading(false);
    }
  };

  // Fiyat hesaplamaları
  const getItemPrice = useCallback(
    (item) => {
      const pid = item.productId || item.id;
      const product = products[pid] || item.product;
      if (item.unitPrice) return Number(item.unitPrice);
      if (product) return Number(product.specialPrice || product.price || 0);
      return 0;
    },
    [products],
  );

  const getSubtotal = useCallback(() => {
    return cartItems.reduce((sum, item) => {
      return sum + getItemPrice(item) * item.quantity;
    }, 0);
  }, [cartItems, getItemPrice]);

  const getShippingCost = () => {
    if (
      appliedCoupon?.couponType === "FreeShipping" ||
      appliedCoupon?.type === 4
    ) {
      return 0;
    }
    // Dinamik kargo fiyatlarını kullan
    if (shippingMethod === "car" || shippingMethod === "express") {
      return shippingPrices.car || 30;
    }
    return shippingPrices.motorcycle || 15;
  };

  const getDiscount = () => {
    return appliedCoupon?.discountAmount || pricing?.couponDiscountTotal || 0;
  };

  const getTotal = () => {
    const subtotal = getSubtotal();
    const shipping = getShippingCost();
    const discount = getDiscount();
    return Math.max(0, subtotal + shipping - discount);
  };

  // Kupon uygula
  const handleApplyCoupon = async () => {
    setCouponError("");
    setCouponSuccess("");

    const code = couponCode?.trim();
    if (!code) {
      setCouponError("Lütfen bir kupon kodu girin.");
      return;
    }

    if (cartItems.length === 0) {
      setCouponError("Sepetiniz boş.");
      return;
    }

    setCouponLoading(true);

    try {
      const subtotal = getSubtotal();
      const itemsPayload = cartItems.map((item) => ({
        productId: item.productId || item.id,
        quantity: item.quantity,
        unitPrice: getItemPrice(item),
        categoryId:
          products[item.productId || item.id]?.categoryId ||
          products[item.productId || item.id]?.category?.id ||
          item.categoryId,
      }));
      const shippingCost = getShippingCost();

      const result = await CartService.validateCoupon(
        code,
        itemsPayload,
        subtotal,
        shippingCost,
      );

      if (result.isValid) {
        const discountAmount =
          result.discountAmount ??
          result.calculatedDiscount ??
          result.discount ??
          0;
        const couponData = {
          code: code,
          couponType: result.couponType,
          discountAmount,
          message: result.message,
        };
        setAppliedCoupon(couponData);
        CartService.setAppliedCoupon(couponData);
        setCouponSuccess(
          `${Number(discountAmount).toFixed(2)}₺ indirim uygulandı!`,
        );
      } else {
        setCouponError(result.message || "Kupon geçersiz veya kullanılamıyor.");
        setAppliedCoupon(null);
        CartService.clearAppliedCoupon();
      }
    } catch (error) {
      console.error("Kupon uygulanırken hata:", error);
      setCouponError("Kupon doğrulanamadı. Lütfen tekrar deneyin.");
    } finally {
      setCouponLoading(false);
    }
  };

  const handleRemoveCoupon = () => {
    setAppliedCoupon(null);
    CartService.clearAppliedCoupon();
    setCouponCode("");
    setCouponSuccess("");
    setCouponError("");
  };

  // Form işlemleri
  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    // Hata temizle
    if (errors[name]) {
      setErrors((prev) => ({ ...prev, [name]: null }));
    }
  };

  // Kart numarası formatlama
  const formatCardNumber = (value) => {
    const v = value.replace(/\s+/g, "").replace(/[^0-9]/gi, "");
    const parts = [];
    for (let i = 0; i < v.length && i < 16; i += 4) {
      parts.push(v.substring(i, i + 4));
    }
    return parts.join(" ");
  };

  const handleCardNumberChange = (e) => {
    const formatted = formatCardNumber(e.target.value);
    setFormData((prev) => ({ ...prev, cardNumber: formatted }));
    if (errors.cardNumber) {
      setErrors((prev) => ({ ...prev, cardNumber: null }));
    }
  };

  // Validasyon
  const validateForm = () => {
    const errs = {};

    // Kart numarası - sadece 16 hane kontrolü (Luhn POSNET tarafından yapılır)
    const cardDigits = (formData.cardNumber || "").replace(/\s/g, "");
    if (cardDigits.length !== 16) {
      errs.cardNumber = "Kart numarası 16 haneli olmalıdır";
    }

    if (!formData.cardName || formData.cardName.trim().length < 3) {
      errs.cardName = "Kart üzerindeki isim gerekli";
    }

    if (!formData.expiryMonth) {
      errs.expiryMonth = "Ay seçin";
    }

    if (!formData.expiryYear) {
      errs.expiryYear = "Yıl seçin";
    }

    if (!/^[0-9]{3,4}$/.test(formData.cvv || "")) {
      errs.cvv = "CVV 3-4 haneli olmalı";
    }

    // Teslimat bilgileri
    if (!formData.firstName?.trim()) errs.firstName = "Ad gerekli";
    if (!formData.lastName?.trim()) errs.lastName = "Soyad gerekli";
    if (!formData.email?.trim()) errs.email = "E-posta gerekli";
    if (!formData.phone?.trim()) errs.phone = "Telefon gerekli";
    if (!formData.address?.trim()) errs.address = "Adres gerekli";
    if (!formData.city?.trim()) errs.city = "İl gerekli";

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  // Ödeme işlemi
  const handleSubmit = async (e) => {
    e.preventDefault();
    setPaymentError("");

    if (!validateForm()) {
      return;
    }

    if (cartItems.length === 0) {
      setPaymentError("Sepetiniz boş. Lütfen önce ürün ekleyin.");
      return;
    }

    setProcessing(true);

    try {
      const total = getTotal();
      const shippingCost = getShippingCost();

      // Sipariş payload
      const orderItems = cartItems.map((ci) => {
        const pid = ci.productId || ci.id;
        const unitPrice = getItemPrice(ci);
        return {
          productId: pid,
          quantity: ci.quantity,
          unitPrice: unitPrice,
        };
      });

      const payload = {
        totalPrice: total,
        orderItems,
        shippingMethod:
          shippingMethod === "car" || shippingMethod === "express"
            ? "car"
            : "motorcycle",
        shippingCost: Number(shippingCost.toFixed(2)),
        customerName: `${formData.firstName} ${formData.lastName}`.trim(),
        customerPhone: formData.phone,
        customerEmail: formData.email,
        shippingAddress: formData.address,
        shippingCity: formData.city,
        shippingDistrict: formData.district,
        shippingPostalCode: formData.postalCode,
        paymentMethod: "creditCard",
        deliveryNotes: [
          deliverySlot !== "standard" ? `Slot: ${deliverySlot}` : null,
          deliveryNote ? `Not: ${deliveryNote}` : null,
        ]
          .filter(Boolean)
          .join(" | "),
        clientOrderId,
        couponCode: appliedCoupon?.code || couponCode?.trim() || null,
      };

      // Sipariş oluştur
      const orderRes = await OrderService.checkout(payload);

      if (!orderRes?.success || !orderRes?.orderId) {
        throw new Error(orderRes?.message || "Sipariş oluşturulamadı");
      }

      // POSNET 3D Secure başlat - Kart bilgileri ile
      const amountToCharge = Number(
        (orderRes.finalPrice ?? orderRes.totalPrice ?? total).toFixed(2),
      );

      // Kart son kullanma tarihi - ayrı ayrı gönderilmeli (Backend DTO beklentisi)
      // ExpireMonth: 01-12 formatında (2 haneli)
      // ExpireYear: YY formatında (2 haneli, örn: 25)
      const expireMonth = formData.expiryMonth.padStart(2, "0");
      const expireYear = formData.expiryYear.slice(-2);

      const initResult = await PaymentService.initiatePosnet3DSecure({
        orderId: orderRes.orderId,
        amount: amountToCharge,
        cardNumber: formData.cardNumber.replace(/\s/g, ""),
        expireMonth: expireMonth,
        expireYear: expireYear,
        cvv: formData.cvv,
        cardHolderName: formData.cardName,
        installmentCount: 0, // Peşin
      });

      // 3D Secure HTML formu varsa submit et - CSP uyumlu
      if (initResult?.threeDSecureHtml) {
        const container = document.createElement("div");
        container.style.display = "none";
        container.innerHTML = initResult.threeDSecureHtml;
        document.body.appendChild(container);
        const form = container.querySelector("form");
        if (form) {
          // Küçük gecikme ile submit (DOM'un hazır olması için)
          setTimeout(() => {
            form.submit();
          }, 100);
          return;
        } else {
          throw new Error("3D Secure formu oluşturulamadı");
        }
      }

      // Redirect URL varsa yönlendir
      if (initResult?.requiresRedirect && initResult?.redirectUrl) {
        window.location.href = initResult.redirectUrl;
        return;
      }

      // Başarısız ise hata göster
      if (!initResult?.success) {
        throw new Error(initResult?.error || "3D Secure başlatılamadı");
      }

      // Redirect gerekmiyorsa başarılı
      clearCart();
      CartService.clearGuestCart();
      CartService.clearAppliedCoupon();

      const successParams = new URLSearchParams();
      if (orderRes.orderNumber)
        successParams.set("orderNumber", orderRes.orderNumber);
      if (orderRes.orderId) successParams.set("orderId", orderRes.orderId);
      navigate(`/order-success?${successParams.toString()}`);
    } catch (error) {
      console.error("Ödeme sırasında hata:", error);
      const message =
        error?.response?.data?.message ||
        error?.message ||
        "Ödeme sırasında bir hata oluştu.";
      setPaymentError(message);
    } finally {
      setProcessing(false);
    }
  };

  // Loading
  if (loading) {
    return (
      <div className="payment-page">
        <div className="container py-5">
          <div className="loading-state">
            <div className="spinner"></div>
            <p>Ödeme sayfası yükleniyor...</p>
          </div>
        </div>
      </div>
    );
  }

  // Boş sepet
  if (!loading && cartItems.length === 0) {
    return (
      <div className="payment-page">
        <div className="container py-5">
          <div className="empty-cart-container">
            <div className="empty-cart-icon">
              <i className="fas fa-shopping-cart"></i>
            </div>
            <h2>Sepetiniz Boş</h2>
            <p>Ödeme adımına geçebilmek için sepetinize ürün ekleyin.</p>
            <Link to="/" className="btn-shop-now">
              <i className="fas fa-arrow-left me-2"></i>
              ALIŞVERİŞE DÖN
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="payment-page">
      <div className="container py-4">
        {/* Header */}
        <div className="payment-header">
          <div className="payment-header-left">
            <i className="fas fa-credit-card"></i>
            <div>
              <h1>Ödeme</h1>
              <span className="item-count">{cartItems.length} ürün</span>
            </div>
          </div>
          <Link to="/cart" className="back-to-cart">
            <i className="fas fa-arrow-left me-2"></i>
            Sepete Dön
          </Link>
        </div>

        {/* Giriş yapmamış kullanıcılar için */}
        {!user && (
          <div className="guest-notice">
            <div className="guest-notice-content">
              <i className="fas fa-user-circle"></i>
              <span>
                Hesabınızla giriş yaparak bilgilerinizi hızlıca
                kullanabilirsiniz.
              </span>
            </div>
            <button
              className="btn-login"
              onClick={() => setShowLoginModal(true)}
            >
              <i className="fas fa-sign-in-alt me-2"></i>
              Giriş Yap
            </button>
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="payment-content">
            {/* Sol Taraf - Form */}
            <div className="payment-form-section">
              {paymentError && (
                <div className="payment-error">
                  <i className="fas fa-exclamation-circle"></i>
                  <span>{paymentError}</span>
                  <button type="button" onClick={() => setPaymentError("")}>
                    <i className="fas fa-times"></i>
                  </button>
                </div>
              )}

              {/* Kart Bilgileri */}
              <div className="form-card">
                <div className="form-card-header card-header-teal">
                  <i className="fas fa-lock"></i>
                  <span>Kart Bilgileri</span>
                </div>
                <div className="form-card-body">
                  {/* Kredi Kartı Önizlemesi */}
                  <CreditCardPreview
                    cardNumber={formData.cardNumber}
                    cardHolderName={formData.cardName}
                    expiryDate={
                      formData.expiryMonth && formData.expiryYear
                        ? `${formData.expiryMonth}/${String(formData.expiryYear).slice(-2)}`
                        : ""
                    }
                    cvv={formData.cvv}
                    isFlipped={isCardFlipped}
                  />

                  <div className="form-row">
                    <div className="form-group full-width">
                      <label>Kart Numarası</label>
                      <input
                        type="text"
                        name="cardNumber"
                        placeholder="4506 3491 1654 3211"
                        value={formData.cardNumber}
                        onChange={handleCardNumberChange}
                        maxLength="19"
                        className={errors.cardNumber ? "error" : ""}
                      />
                      {errors.cardNumber && (
                        <span className="error-text">{errors.cardNumber}</span>
                      )}
                    </div>
                  </div>
                  <div className="form-row">
                    <div className="form-group full-width">
                      <label>Kart Üzerindeki İsim</label>
                      <input
                        type="text"
                        name="cardName"
                        placeholder="TEST KART"
                        value={formData.cardName}
                        onChange={handleInputChange}
                        className={errors.cardName ? "error" : ""}
                      />
                      {errors.cardName && (
                        <span className="error-text">{errors.cardName}</span>
                      )}
                    </div>
                  </div>
                  <div className="form-row three-cols">
                    <div className="form-group">
                      <label>Ay</label>
                      <select
                        name="expiryMonth"
                        value={formData.expiryMonth}
                        onChange={handleInputChange}
                        className={errors.expiryMonth ? "error" : ""}
                      >
                        <option value="">Ay</option>
                        {Array.from({ length: 12 }, (_, i) => (
                          <option
                            key={i + 1}
                            value={String(i + 1).padStart(2, "0")}
                          >
                            {String(i + 1).padStart(2, "0")}
                          </option>
                        ))}
                      </select>
                      {errors.expiryMonth && (
                        <span className="error-text">{errors.expiryMonth}</span>
                      )}
                    </div>
                    <div className="form-group">
                      <label>Yıl</label>
                      <select
                        name="expiryYear"
                        value={formData.expiryYear}
                        onChange={handleInputChange}
                        className={errors.expiryYear ? "error" : ""}
                      >
                        <option value="">Yıl</option>
                        {Array.from({ length: 10 }, (_, i) => {
                          const year = new Date().getFullYear() + i;
                          return (
                            <option key={year} value={year}>
                              {year}
                            </option>
                          );
                        })}
                      </select>
                      {errors.expiryYear && (
                        <span className="error-text">{errors.expiryYear}</span>
                      )}
                    </div>
                    <div className="form-group">
                      <label>CVV</label>
                      <input
                        type="text"
                        name="cvv"
                        placeholder="000"
                        value={formData.cvv}
                        onChange={handleInputChange}
                        onFocus={() => setIsCardFlipped(true)}
                        onBlur={() => setIsCardFlipped(false)}
                        maxLength="4"
                        className={errors.cvv ? "error" : ""}
                      />
                      {errors.cvv && (
                        <span className="error-text">{errors.cvv}</span>
                      )}
                    </div>
                  </div>
                  <div className="secure-badge">
                    <i className="fas fa-shield-alt"></i>
                    <span>Yapı Kredi 3D Secure ile güvende</span>
                  </div>
                </div>
              </div>

              {/* Teslimat Bilgileri */}
              <div className="form-card">
                <div className="form-card-header card-header-purple">
                  <i className="fas fa-truck"></i>
                  <span>Teslimat Bilgileri</span>
                </div>
                <div className="form-card-body">
                  <div className="form-row two-cols">
                    <div className="form-group">
                      <label>Ad</label>
                      <input
                        type="text"
                        name="firstName"
                        value={formData.firstName}
                        onChange={handleInputChange}
                        className={errors.firstName ? "error" : ""}
                      />
                      {errors.firstName && (
                        <span className="error-text">{errors.firstName}</span>
                      )}
                    </div>
                    <div className="form-group">
                      <label>Soyad</label>
                      <input
                        type="text"
                        name="lastName"
                        value={formData.lastName}
                        onChange={handleInputChange}
                        className={errors.lastName ? "error" : ""}
                      />
                      {errors.lastName && (
                        <span className="error-text">{errors.lastName}</span>
                      )}
                    </div>
                  </div>
                  <div className="form-row two-cols">
                    <div className="form-group">
                      <label>E-posta</label>
                      <input
                        type="email"
                        name="email"
                        value={formData.email}
                        onChange={handleInputChange}
                        className={errors.email ? "error" : ""}
                      />
                      {errors.email && (
                        <span className="error-text">{errors.email}</span>
                      )}
                    </div>
                    <div className="form-group">
                      <label>Telefon</label>
                      <input
                        type="tel"
                        name="phone"
                        placeholder="05XX XXX XX XX"
                        value={formData.phone}
                        onChange={handleInputChange}
                        className={errors.phone ? "error" : ""}
                      />
                      {errors.phone && (
                        <span className="error-text">{errors.phone}</span>
                      )}
                    </div>
                  </div>
                  <div className="form-row">
                    <div className="form-group full-width">
                      <label>Adres</label>
                      <textarea
                        name="address"
                        rows="3"
                        value={formData.address}
                        onChange={handleInputChange}
                        className={errors.address ? "error" : ""}
                      />
                      {errors.address && (
                        <span className="error-text">{errors.address}</span>
                      )}
                    </div>
                  </div>
                  <div className="form-row three-cols">
                    <div className="form-group">
                      <label>İl</label>
                      <input
                        type="text"
                        name="city"
                        value={formData.city}
                        onChange={handleInputChange}
                        className={errors.city ? "error" : ""}
                      />
                      {errors.city && (
                        <span className="error-text">{errors.city}</span>
                      )}
                    </div>
                    <div className="form-group">
                      <label>İlçe</label>
                      <input
                        type="text"
                        name="district"
                        value={formData.district}
                        onChange={handleInputChange}
                      />
                    </div>
                    <div className="form-group">
                      <label>Posta Kodu</label>
                      <input
                        type="text"
                        name="postalCode"
                        value={formData.postalCode}
                        onChange={handleInputChange}
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* Teslimat Zamanı */}
              <div className="form-card">
                <div className="form-card-header card-header-green">
                  <i className="fas fa-clock"></i>
                  <span>Teslimat Zamanı</span>
                </div>
                <div className="form-card-body">
                  <div className="delivery-slots">
                    {[
                      { id: "standard", label: "Gün içinde (Standart)" },
                      { id: "10:00-13:00", label: "10:00 - 13:00" },
                      { id: "13:00-16:00", label: "13:00 - 16:00" },
                      { id: "16:00-20:00", label: "16:00 - 20:00" },
                    ].map((slot) => (
                      <label
                        key={slot.id}
                        className={`delivery-slot ${deliverySlot === slot.id ? "active" : ""}`}
                      >
                        <input
                          type="radio"
                          name="deliverySlot"
                          checked={deliverySlot === slot.id}
                          onChange={() => setDeliverySlot(slot.id)}
                        />
                        <span>{slot.label}</span>
                      </label>
                    ))}
                  </div>
                  <div className="form-group full-width mt-3">
                    <label>Teslimat Notu (Opsiyonel)</label>
                    <textarea
                      rows="2"
                      placeholder="Örn. Kapı önüne bırakın, güvenlikte bırakın vb."
                      value={deliveryNote}
                      onChange={(e) => setDeliveryNote(e.target.value)}
                    />
                  </div>
                </div>
              </div>
            </div>

            {/* Sağ Taraf - Sipariş Özeti */}
            <div className="payment-summary-section">
              <div className="summary-card">
                <div className="summary-header">
                  <i className="fas fa-receipt"></i>
                  <span>Sipariş Özeti</span>
                </div>

                {/* Ürünler */}
                <div className="summary-items">
                  {cartItems.map((item) => {
                    const pid = item.productId || item.id;
                    const product = products[pid] || item.product || {};
                    const price = getItemPrice(item);
                    return (
                      <div key={item.id || pid} className="summary-item">
                        <div className="summary-item-image">
                          {product.imageUrl ? (
                            <img
                              src={product.imageUrl}
                              alt={product.name || "Ürün"}
                              onError={(e) => {
                                e.target.style.display = "none";
                              }}
                            />
                          ) : (
                            <i className="fas fa-box"></i>
                          )}
                        </div>
                        <div className="summary-item-info">
                          <span className="summary-item-name">
                            {product.name || "Ürün"}
                          </span>
                          <span className="summary-item-qty">
                            {item.quantity} x ₺{price.toFixed(2)}
                          </span>
                        </div>
                        <span className="summary-item-total">
                          ₺{(price * item.quantity).toFixed(2)}
                        </span>
                      </div>
                    );
                  })}
                </div>

                {/* Teslimat Yöntemi */}
                <div className="summary-section">
                  <h4>Teslimat Yöntemi</h4>
                  <div className="shipping-options">
                    <label
                      className={`shipping-option ${shippingMethod === "motorcycle" ? "active" : ""}`}
                    >
                      <input
                        type="radio"
                        name="shippingMethod"
                        checked={shippingMethod === "motorcycle"}
                        onChange={() => setShippingMethod("motorcycle")}
                      />
                      <i className="fas fa-motorcycle"></i>
                      <div>
                        <span className="shipping-name">Motokurye</span>
                        <span className="shipping-price">
                          {shippingPricesLoading ? (
                            <i className="fas fa-spinner fa-spin fa-sm"></i>
                          ) : (
                            `₺${shippingPrices.motorcycle}`
                          )}
                        </span>
                      </div>
                    </label>
                    <label
                      className={`shipping-option ${shippingMethod === "car" || shippingMethod === "express" ? "active" : ""}`}
                    >
                      <input
                        type="radio"
                        name="shippingMethod"
                        checked={
                          shippingMethod === "car" ||
                          shippingMethod === "express"
                        }
                        onChange={() => setShippingMethod("car")}
                      />
                      <i className="fas fa-car"></i>
                      <div>
                        <span className="shipping-name">Araç (Hızlı)</span>
                        <span className="shipping-price">
                          {shippingPricesLoading ? (
                            <i className="fas fa-spinner fa-spin fa-sm"></i>
                          ) : (
                            `₺${shippingPrices.car}`
                          )}
                        </span>
                      </div>
                    </label>
                  </div>
                </div>

                {/* Kupon */}
                <div className="summary-section">
                  <h4>Kupon Kodu</h4>
                  {appliedCoupon ? (
                    <div className="applied-coupon">
                      <div className="coupon-info">
                        <i className="fas fa-tag"></i>
                        <span>{appliedCoupon.code}</span>
                        <span className="coupon-discount">
                          -₺{Number(appliedCoupon.discountAmount).toFixed(2)}
                        </span>
                      </div>
                      <button
                        type="button"
                        className="remove-coupon"
                        onClick={handleRemoveCoupon}
                      >
                        <i className="fas fa-times"></i>
                      </button>
                    </div>
                  ) : (
                    <div className="coupon-input-group">
                      <input
                        type="text"
                        placeholder="Kupon kodunuzu girin"
                        value={couponCode}
                        onChange={(e) => setCouponCode(e.target.value)}
                      />
                      <button
                        type="button"
                        onClick={handleApplyCoupon}
                        disabled={couponLoading}
                      >
                        {couponLoading ? (
                          <i className="fas fa-spinner fa-spin"></i>
                        ) : (
                          "Uygula"
                        )}
                      </button>
                    </div>
                  )}
                  {couponError && (
                    <div className="coupon-error">{couponError}</div>
                  )}
                  {couponSuccess && (
                    <div className="coupon-success">{couponSuccess}</div>
                  )}
                </div>

                {/* Toplam */}
                <div className="summary-totals">
                  <div className="summary-row">
                    <span>Ara Toplam</span>
                    <span>₺{getSubtotal().toFixed(2)}</span>
                  </div>

                  {/* ========== KAMPANYA İNDİRİMİ ========== */}
                  {(campaignDiscountTotal > 0 ||
                    pricing?.campaignDiscountTotal > 0) && (
                    <div
                      className="summary-row discount"
                      style={{ color: "#2e7d32" }}
                    >
                      <span>
                        <i
                          className="fas fa-gift me-1"
                          style={{ fontSize: "0.8rem" }}
                        ></i>
                        Kampanya İndirimi
                      </span>
                      <span>
                        -₺
                        {(
                          campaignDiscountTotal ||
                          pricing?.campaignDiscountTotal ||
                          0
                        ).toFixed(2)}
                      </span>
                    </div>
                  )}

                  {/* ========== UYGULANAN KAMPANYALAR LİSTESİ ========== */}
                  {pricingError && (
                    <div className="coupon-error">{pricingError}</div>
                  )}
                  {pricingLoading && (
                    <div className="coupon-success">
                      Kampanyalar hesaplanıyor...
                    </div>
                  )}
                  {appliedCampaigns.length > 0 && (
                    <div
                      className="applied-campaigns"
                      style={{
                        padding: "8px 0",
                        borderBottom: "1px dashed #e0e0e0",
                        marginBottom: "8px",
                      }}
                    >
                      {appliedCampaigns.map((campaign, index) => (
                        <div
                          key={index}
                          style={{
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "space-between",
                            padding: "4px 0",
                            fontSize: "0.8rem",
                          }}
                        >
                          <span
                            style={{
                              color: "#666",
                              display: "flex",
                              alignItems: "center",
                              gap: "4px",
                            }}
                          >
                            <i
                              className={`fas ${CampaignService.getCampaignBadge(campaign.type).icon}`}
                              style={{ color: "#ff6b35", fontSize: "0.7rem" }}
                            ></i>
                            {campaign.displayText ||
                              campaign.campaignName ||
                              campaign.name ||
                              CampaignService.getDiscountText(campaign)}
                          </span>
                          {campaign.discountAmount > 0 && (
                            <span
                              style={{ color: "#2e7d32", fontWeight: "500" }}
                            >
                              -₺{campaign.discountAmount.toFixed(2)}
                            </span>
                          )}
                        </div>
                      ))}
                    </div>
                  )}

                  <div className="summary-row">
                    <span>Kargo</span>
                    <span className={getShippingCost() === 0 ? "free" : ""}>
                      {getShippingCost() === 0
                        ? "Ücretsiz"
                        : `₺${getShippingCost().toFixed(2)}`}
                    </span>
                  </div>
                  {getDiscount() > 0 && (
                    <div className="summary-row discount">
                      <span>
                        <i
                          className="fas fa-tag me-1"
                          style={{ fontSize: "0.8rem" }}
                        ></i>
                        Kupon İndirimi
                      </span>
                      <span>-₺{getDiscount().toFixed(2)}</span>
                    </div>
                  )}
                  <div className="summary-row total">
                    <span>Toplam</span>
                    <span>
                      ₺
                      {(
                        getTotal() -
                        (campaignDiscountTotal ||
                          pricing?.campaignDiscountTotal ||
                          0)
                      ).toFixed(2)}
                    </span>
                  </div>
                </div>

                {/* Ödeme Butonu */}
                <button type="submit" className="btn-pay" disabled={processing}>
                  {processing ? (
                    <>
                      <i className="fas fa-spinner fa-spin"></i>
                      <span>İşleniyor...</span>
                    </>
                  ) : (
                    <>
                      <i className="fas fa-lock"></i>
                      <span>Güvenli Ödeme Yap</span>
                    </>
                  )}
                </button>

                <div className="payment-security">
                  <i className="fas fa-shield-alt"></i>
                  <span>256-bit SSL ile güvende</span>
                </div>
              </div>
            </div>
          </div>
        </form>
      </div>

      <LoginModal
        show={showLoginModal}
        onHide={() => setShowLoginModal(false)}
        onLoginSuccess={() => setShowLoginModal(false)}
      />
    </div>
  );
};

export default PaymentPage;
