import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { CartService } from "../services/cartService";
import { OrderService } from "../services/orderService";
import { PaymentService } from "../services/paymentService";
import { ProductService } from "../services/productService";
import LoginModal from "./LoginModal";

const PaymentPage = () => {
  const { user } = useAuth();
  const [cartItems, setCartItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [processing, setProcessing] = useState(false);
  const [paymentMethod, setPaymentMethod] = useState("creditCard");
  const [paymentProvider, setPaymentProvider] = useState("iyzico"); // stripe | iyzico | paypal
  const [products, setProducts] = useState({});
  const [shippingMethod, setShippingMethod] = useState(() => {
    try {
      return CartService.getShippingMethod() || "motorcycle";
    } catch {
      return "motorcycle";
    }
  }); // 'motorcycle' (motokurye) | 'car' (araç)
  const [couponCode, setCouponCode] = useState("");
  const [pricing, setPricing] = useState(null);
  const [pricingLoading, setPricingLoading] = useState(false);
  const [pricingError, setPricingError] = useState("");
  const [errors, setErrors] = useState({});
  const [deliverySlot, setDeliverySlot] = useState("standard");
  const [deliveryNote, setDeliveryNote] = useState("");
  const [serverVat, setServerVat] = useState(null);
  const [showLoginModal, setShowLoginModal] = useState(false);
  const [paymentError, setPaymentError] = useState("");
  const [clientOrderId] = useState(() => {
    try {
      if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
        return crypto.randomUUID();
      }
    } catch {
      // ignore
    }
    return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  });
  const [formData, setFormData] = useState({
    // Kredi Kartı Bilgileri
    cardNumber: "",
    cardName: "",
    expiryMonth: "",
    expiryYear: "",
    cvv: "",

    // Adres Bilgileri
    firstName: "",
    lastName: "",
    email: "",
    phone: "",
    address: "",
    city: "",
    district: "",
    postalCode: "",

    // Fatura Adresi
    billingAddress: "",
    billingCity: "",
    billingDistrict: "",
    billingPostalCode: "",
    sameAsDelivery: true,
  });

  const normalizeShippingChoice = (method) => {
    const value = (method || "").toLowerCase();
    if (value === "car" || value === "express" || value === "arac" || value === "araç") {
      return "car";
    }
    return "motorcycle";
  };

  const getShippingCost = (method) =>
    normalizeShippingChoice(method) === "car" ? 30 : 15;

  const mergePricingWithShipping = (pricingResult, method) => {
    if (!pricingResult) return null;

    const shippingFee = getShippingCost(method);
    const subtotal = Number(pricingResult.subtotal || 0);
    const campaignDiscount = Number(pricingResult.campaignDiscountTotal || 0);
    const couponDiscount = Number(pricingResult.couponDiscountTotal || 0);
    const deliveryFee = shippingFee;
    const discountTotal = campaignDiscount + couponDiscount;
    const grandTotal = Math.max(0, subtotal - discountTotal + deliveryFee);

    return {
      ...pricingResult,
      deliveryFee,
      grandTotal,
    };
  };

  useEffect(() => {
    loadCartItems();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    setPricing((prev) => (prev ? mergePricingWithShipping(prev, shippingMethod) : null));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [shippingMethod]);

  const loadCartItems = async () => {
    try {
      let items = [];
      // Önce oturum açmış kullanıcıdan backend sepeti; değilse guest localStorage
      if (user) {
        try {
          const serverItems = await CartService.getCartItems();
          items = Array.isArray(serverItems) ? serverItems : [];
        } catch (e) {
          // Backend erişilemiyorsa guest'e dön
          items = CartService.getGuestCart();
        }
      } else {
        items = CartService.getGuestCart();
      }

      setCartItems(items);

      // Ürün detayları (fiyat için gerekli)
      try {
        let allProducts = [];
        try {
          allProducts = await ProductService.list();
        } catch (error) {
          // Mock ürünler (CartPage ile tutarlı)
          allProducts = [
            {
              id: 1,
              name: "Cif Krem Doğanın Gücü Hijyen 675Ml",
              price: 204.95,
              specialPrice: 129.95,
              categoryId: 7,
              imageUrl: "/images/yeşil-cif-krem.jpg",
            },
            {
              id: 2,
              name: "Ülker Altınbaşak Tahıl Cipsi 50 Gr",
              price: 18.0,
              specialPrice: 14.9,
              categoryId: 6,
              imageUrl: "/images/tahil-cipsi.jpg",
            },
            {
              id: 3,
              name: "Lipton Ice Tea Limon 330 Ml",
              price: 60.0,
              specialPrice: 40.9,
              categoryId: 5,
              imageUrl: "/images/lipton-ice-tea.jpg",
            },
            {
              id: 4,
              name: "Dana But Tas Kebaplık Et Çiftlik Kg",
              price: 375.95,
              specialPrice: 279.0,
              categoryId: 2,
              imageUrl: "/images/dana-kusbasi.jpg",
            },
            {
              id: 5,
              name: "Kuzu İncik Kg",
              price: 1399.95,
              specialPrice: 699.95,
              categoryId: 2,
              imageUrl: "/images/kuzu-incik.webp",
            },
            {
              id: 6,
              name: "Nescafe 2si 1 Arada Sütlü Köpüklü 15 x 10g",
              price: 145.55,
              specialPrice: 84.5,
              categoryId: 5,
              imageUrl: "/images/nescafe.jpg",
            },
            {
              id: 7,
              name: "Domates Kg",
              price: 45.9,
              specialPrice: 45.9,
              categoryId: 1,
              imageUrl: "/images/domates.webp",
            },
            {
              id: 8,
              name: "Pınar Süt 1L",
              price: 28.5,
              specialPrice: 28.5,
              categoryId: 3,
              imageUrl: "/images/pınar-süt.jpg",
            },
            {
              id: 9,
              name: "Sek Kaşar Peyniri 200 G",
              price: 75.9,
              specialPrice: 64.5,
              categoryId: 3,
              imageUrl: "/images/sek-kasar-peyniri-200-gr-38be46-1650x1650.jpg",
            },
            {
              id: 10,
              name: "Mis Bulgur Pilavlık 1Kg",
              price: 32.9,
              specialPrice: 32.9,
              categoryId: 4,
              imageUrl: "/images/bulgur.png",
            },
            {
              id: 11,
              name: "Coca-Cola Orijinal Tat Kutu 330ml",
              price: 12.5,
              specialPrice: 10.0,
              categoryId: 5,
              imageUrl: "/images/coca-cola.jpg",
            },
            {
              id: 12,
              name: "Salatalık Kg",
              price: 28.9,
              specialPrice: 28.9,
              categoryId: 1,
              imageUrl: "/images/salatalik.jpg",
            },
          ];
        }

        const map = {};
        for (const p of allProducts) map[p.id] = p;
        setProducts(map);
      } catch (error) {
        console.error("Ürün verileri yüklenemedi:", error);
      }
    } finally {
      setLoading(false);
    }
  };

  const calculateTotals = () => {
    const subtotal = cartItems.reduce((sum, item) => {
      const p = products[item.productId];
      const price = p ? p.specialPrice || p.price : 0;
      return sum + price * item.quantity;
    }, 0);
    const baseShipping = getShippingCost(shippingMethod);
    const shipping = baseShipping; // Always apply chosen shipping cost (frontend choice)
    const fallbackTax = subtotal * 0.18; // KDV %18 (frontend fallback)
    const tax =
      serverVat != null && !Number.isNaN(Number(serverVat))
        ? Number(serverVat)
        : fallbackTax;
    const total = subtotal + shipping + tax;

    return { subtotal, shipping, tax, total };
  };

  const handleApplyCoupon = async () => {
    if (!cartItems || cartItems.length === 0) return;
    setPricingLoading(true);
    setPricingError("");
    try {
      const itemsPayload = cartItems.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
      }));
      const result = await CartService.previewPrice({
        items: itemsPayload,
        couponCode: couponCode?.trim() || null,
      });
      setPricing(mergePricingWithShipping(result, shippingMethod));
    } catch (error) {
      console.error("Kupon uygulanırken hata:", error);
      setPricing(null);
      setPricingError("Kupon geçersiz veya kullanılamıyor.");
    } finally {
      setPricingLoading(false);
    }
  };

  const handleInputChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  // Kart doğrulama yardımcıları (Luhn ve alan kontrolleri)
  const onlyDigits = (s) => (s || "").replace(/\D/g, "");
  const luhnCheck = (num) => {
    const arr = onlyDigits(num)
      .split("")
      .reverse()
      .map((n) => parseInt(n, 10));
    if (!arr.length) return false;
    let sum = 0;
    for (let i = 0; i < arr.length; i++) {
      let n = arr[i];
      if (i % 2 === 1) {
        n *= 2;
        if (n > 9) n -= 9;
      }
      sum += n;
    }
    return sum % 10 === 0;
  };
  const validateCard = () => {
    const errs = {};
    if (paymentMethod === "creditCard") {
      const num = onlyDigits(formData.cardNumber || "");
      if (num.length < 13 || num.length > 19 || !luhnCheck(num)) {
        errs.cardNumber = "Geçersiz kart numarası";
      }
      if (!formData.cardName || formData.cardName.trim().length < 3) {
        errs.cardName = "Kart üzerindeki isim gerekli";
      }
      const m = parseInt(formData.expiryMonth, 10);
      const y = parseInt(formData.expiryYear, 10);
      if (!(m >= 1 && m <= 12)) {
        errs.expiryMonth = "Ay 01-12 arasında olmalı";
      }
      const currentYY = new Date().getFullYear();
      if (!(y >= currentYY)) {
        errs.expiryYear = "Yıl geçersiz";
      }
      if (!/^[0-9]{3,4}$/.test(formData.cvv || "")) {
        errs.cvv = "CVV 3-4 haneli olmalı";
      }
    }
    return errs;
  };

  const formatCardNumber = (value) => {
    // Sadece rakamları al ve 4'lü gruplar halinde böl
    const v = value.replace(/\s+/g, "").replace(/[^0-9]/gi, "");
    const matches = v.match(/\d{4,16}/g);
    const match = (matches && matches[0]) || "";
    const parts = [];
    for (let i = 0, len = match.length; i < len; i += 4) {
      parts.push(match.substring(i, i + 4));
    }
    if (parts.length) {
      return parts.join(" ");
    } else {
      return v;
    }
  };

  const handleCardNumberChange = (e) => {
    const formatted = formatCardNumber(e.target.value);
    setFormData((prev) => ({ ...prev, cardNumber: formatted }));
  };

  const validateForm = () => {
    const required = [
      "firstName",
      "lastName",
      "email",
      "phone",
      "address",
      "city",
    ];
    if (paymentMethod === "creditCard") {
      required.push(
        "cardNumber",
        "cardName",
        "expiryMonth",
        "expiryYear",
        "cvv"
      );
    }

    return required.every((field) => formData[field].trim() !== "");
  };

  const performCheckout = async () => {
    setPaymentError("");

    if (!validateForm()) {
      alert("Lütfen tüm zorunlu alanları doldurun.");
      return;
    }

    if (cartItems.length === 0) {
      alert("Sepetiniz boş. Lütfen önce ürün ekleyin.");
      return;
    }

    // Kart doğrulama (kredi kartı seçiliyse)
    const v = validateCard();
    setErrors(v);
    if (Object.keys(v).length > 0) {
      return;
    }

    setProcessing(true);

    try {
      const { total, shipping } = calculateTotals();
      const normalizedShippingMethod = normalizeShippingChoice(shippingMethod);
      const shippingCost = shipping;

      // Backend DTO'suna uygun payload
      const orderItems = cartItems.map((ci) => {
        const p = products[ci.productId];
        const unitPrice = p ? p.specialPrice || p.price : 0;
        return {
          productId: ci.productId,
          quantity: ci.quantity,
          unitPrice: unitPrice,
        };
      });

      const payload = {
        totalPrice: total, // sunucu yeniden hesaplayacak
        orderItems,
        shippingMethod: normalizedShippingMethod,
        shippingCost: Number(shippingCost.toFixed(2)),
        customerName: `${formData.firstName} ${formData.lastName}`.trim(),
        customerPhone: formData.phone,
        customerEmail: formData.email,
        shippingAddress: formData.address,
        shippingCity: formData.city,
        shippingDistrict: formData.district,
        shippingPostalCode: formData.postalCode,
        paymentMethod,
        deliveryNotes: [
          deliverySlot ? `Slot: ${deliverySlot}` : null,
          deliveryNote ? `Not: ${deliveryNote}` : null,
        ]
          .filter(Boolean)
          .join(" | "),
        clientOrderId,
        couponCode: couponCode?.trim() || null,
      };

      const orderRes = await OrderService.checkout(payload);

      if (!orderRes?.success || !orderRes?.orderId) {
        throw new Error(orderRes?.message || "Sipariş oluşturulamadı");
      }

      // Backend'den gelen KDV (varsa) state'e yaz
      if (typeof orderRes?.vatAmount !== "undefined") {
        setServerVat(Number(orderRes.vatAmount || 0));
      }

      // Kredi kartı ile ödeme ise seçilen sağlayıcıyı başlat (Stripe/Iyzico/PayPal).
      const amountToCharge = Number(
        (orderRes.finalPrice ?? orderRes.totalPrice ?? total).toFixed(2)
      );

      if (paymentMethod === "creditCard") {
        const init = await PaymentService.initiate(
          orderRes.orderId,
          amountToCharge,
          paymentProvider,
          "TRY"
        );

        if (init?.requiresRedirect && init?.redirectUrl) {
          // Stripe/Iyzico/PayPal hosted checkout sayfasına yönlendir (3D Secure vb.)
          window.location.href = init.redirectUrl;
          return;
        }
      }

      const vatForAlert =
        typeof orderRes?.vatAmount !== "undefined"
          ? Number(orderRes.vatAmount || 0)
          : tax;

      // Redirect gerekmiyorsa başarılı kabul et
      alert(
        `Ödeme tamamlandı! Sipariş numaranız: ${orderRes.orderNumber || "-"} • Tahsil edilen tutar: ₺${amountToCharge.toFixed(
          2
        )} • KDV: ₺${vatForAlert.toFixed(2)}`
      );
      try {
        CartService.clearGuestCart();
      } catch {}
      const successParams = new URLSearchParams();
      if (orderRes.orderNumber) successParams.set("orderNumber", orderRes.orderNumber);
      if (orderRes.orderId) successParams.set("orderId", orderRes.orderId);
      window.location.href = `/order-success?${successParams.toString()}`;
    } catch (error) {
      console.error("Ödeme sırasında hata:", error);
      const message =
        error?.response?.data?.message ||
        error?.message ||
        "Ödeme sırasında bir hata oluştu. Lütfen tekrar deneyin.";
      setPaymentError(message);
    } finally {
      setProcessing(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    await performCheckout();
  };

  const { subtotal, shipping, tax, total } = calculateTotals();

  if (loading) {
    return (
      <div className="text-center py-5">
        <div
          className="spinner-border mb-3"
          role="status"
          style={{ color: "#ff8f00", width: "3rem", height: "3rem" }}
        >
          <span className="visually-hidden">Loading...</span>
        </div>
        <p className="text-muted fw-bold">Ödeme sayfası yükleniyor...</p>
      </div>
    );
  }

  // Boş sepet için görsel durum ve CTA
  if (!loading && (!cartItems || cartItems.length === 0)) {
    return (
      <div
        style={{
          minHeight: "100vh",
          background:
            "linear-gradient(135deg, #fff3e0 0%, #ffe0b2 50%, #ffcc80 100%)",
          paddingTop: "2rem",
          paddingBottom: "2rem",
        }}
      >
        <div className="container">
          <div className="row justify-content-center">
            <div className="col-lg-8">
              <div
                className="card shadow-lg border-0"
                style={{ borderRadius: 20 }}
              >
                <div className="card-body text-center p-5">
                  <div
                    className="mx-auto d-flex align-items-center justify-content-center mb-4"
                    style={{
                      width: 80,
                      height: 80,
                      borderRadius: "50%",
                      background:
                        "linear-gradient(135deg, #ff6f00, #ff8f00, #ffa000)",
                      boxShadow: "0 10px 30px rgba(255,111,0,0.25)",
                      color: "white",
                    }}
                  >
                    <i className="fas fa-shopping-cart fa-2x"></i>
                  </div>
                  <h3 className="fw-bold text-warning mb-2">Sepetiniz Boş</h3>
                  <p className="text-muted mb-4">
                    Ödeme adımına geçebilmek için sepetinize ürün ekleyin.
                  </p>
                  <Link
                    to="/"
                    className="btn btn-lg text-white fw-bold shadow-lg border-0"
                    style={{
                      background:
                        "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)",
                      borderRadius: 15,
                      padding: "0.9rem 1.5rem",
                    }}
                  >
                    <i className="fas fa-arrow-left me-2"></i>
                    Alışverişe Dön
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div
      style={{
        minHeight: "100vh",
        background:
          "linear-gradient(135deg, #fff3e0 0%, #ffe0b2 50%, #ffcc80 100%)",
        paddingTop: "2rem",
        paddingBottom: "2rem",
      }}
    >
      <div className="container">
        {!user && (
          <div
            className="alert d-flex justify-content-between align-items-center shadow-sm"
            style={{
              borderRadius: 12,
              background: "linear-gradient(90deg, #fff8f0, #fff3e0)",
              border: "1px solid #ffe0b2",
            }}
          >
            <div className="d-flex align-items-center">
              <i className="fas fa-user-alt me-2 text-warning"></i>
              <span className="text-muted">
                Giriş yapmadan da devam edebilirsiniz. İsterseniz hesabınızla
                giriş yaparak adres ve ödeme bilgilerinizi hızlıca kullanın.
              </span>
            </div>
            <div className="d-flex gap-2">
              <button
                type="button"
                className="btn btn-sm btn-outline-warning"
                onClick={() => setShowLoginModal(true)}
                style={{ borderRadius: 20 }}
              >
                <i className="fas fa-sign-in-alt me-1"></i> Giriş Yap
              </button>
              <a
                href="#checkout-form"
                className="btn btn-sm btn-warning text-white"
                style={{ borderRadius: 20 }}
              >
                Misafir Olarak Devam Et
              </a>
            </div>
          </div>
        )}
        <form onSubmit={handleSubmit} id="checkout-form">
          <div className="row">
            {/* Sol Taraf: Ödeme Formu */}
            <div className="col-lg-8">
              {paymentError && (
                <div className="alert alert-danger d-flex justify-content-between align-items-center">
                  <span>{paymentError}</span>
                  <button
                    type="button"
                    className="btn btn-sm btn-outline-light"
                    onClick={performCheckout}
                    disabled={processing}
                  >
                    Tekrar Dene
                  </button>
                </div>
              )}
              {/* Ödeme Yöntemi Seçimi */}
              <div
                className="card shadow-lg border-0 mb-4"
                style={{ borderRadius: "20px" }}
              >
                <div
                  className="card-header text-white border-0"
                  style={{
                    background:
                      "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)",
                    borderTopLeftRadius: "20px",
                    borderTopRightRadius: "20px",
                    padding: "1.5rem",
                  }}
                >
                  <h5 className="mb-0 fw-bold">
                    <i className="fas fa-credit-card me-2"></i>Ödeme Yöntemi
                  </h5>
                </div>
                <div className="card-body" style={{ padding: "2rem" }}>
                  <div className="row">
                    <div className="col-md-6 mb-3">
                      <div
                        className={`card h-100 ${
                          paymentMethod === "creditCard" ? "border-warning" : ""
                        }`}
                        style={{
                          cursor: "pointer",
                          borderRadius: "15px",
                          border:
                            paymentMethod === "creditCard"
                              ? "2px solid #ffa000"
                              : "1px solid #dee2e6",
                        }}
                        onClick={() => setPaymentMethod("creditCard")}
                      >
                        <div className="card-body text-center p-4">
                          <i className="fas fa-credit-card fa-3x text-warning mb-3"></i>
                          <h6 className="fw-bold">Kredi Kartı</h6>
                          <p className="text-muted small mb-0">
                            Visa, MasterCard, AmEx
                          </p>
                        </div>
                      </div>
                    </div>
                    <div className="col-md-6 mb-3">
                      <div
                        className={`card h-100 ${
                          paymentMethod === "bankTransfer"
                            ? "border-warning"
                            : ""
                        }`}
                        style={{
                          cursor: "pointer",
                          borderRadius: "15px",
                          border:
                            paymentMethod === "bankTransfer"
                              ? "2px solid #ffa000"
                              : "1px solid #dee2e6",
                        }}
                        onClick={() => setPaymentMethod("bankTransfer")}
                      >
                        <div className="card-body text-center p-4">
                          <i className="fas fa-university fa-3x text-warning mb-3"></i>
                          <h6 className="fw-bold">Havale/EFT</h6>
                          <p className="text-muted small mb-0">
                            Banka hesabına havale
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>
                  {paymentMethod === "creditCard" && (
                    <div className="mt-4">
                      <h6 className="fw-bold mb-2">Ödeme Altyapısı</h6>
                      <div className="d-flex flex-wrap gap-2">
                        <button
                          type="button"
                          className={`btn btn-sm ${
                            paymentProvider === "iyzico"
                              ? "btn-warning text-white"
                              : "btn-outline-warning"
                          }`}
                          onClick={() => setPaymentProvider("iyzico")}
                        >
                          Iyzico
                        </button>
                        <button
                          type="button"
                          className={`btn btn-sm ${
                            paymentProvider === "stripe"
                              ? "btn-warning text-white"
                              : "btn-outline-warning"
                          }`}
                          onClick={() => setPaymentProvider("stripe")}
                        >
                          Stripe
                        </button>
                        <button
                          type="button"
                          className={`btn btn-sm ${
                            paymentProvider === "paypal"
                              ? "btn-warning text-white"
                              : "btn-outline-warning"
                          }`}
                          onClick={() => setPaymentProvider("paypal")}
                        >
                          PayPal
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {/* Kredi Kartı Bilgileri */}
              {paymentMethod === "creditCard" && (
                <div
                  className="card shadow-lg border-0 mb-4"
                  style={{ borderRadius: "20px" }}
                >
                  <div
                    className="card-header text-white border-0"
                    style={{
                      background: "linear-gradient(45deg, #17a2b8, #20c997)",
                      borderTopLeftRadius: "20px",
                      borderTopRightRadius: "20px",
                      padding: "1.5rem",
                    }}
                  >
                    <h5 className="mb-0 fw-bold">
                      <i className="fas fa-lock me-2"></i>Kart Bilgileri
                    </h5>
                  </div>
                  <div className="card-body" style={{ padding: "2rem" }}>
                    <div className="row">
                      <div className="col-md-6 mb-3">
                        <label className="form-label fw-bold text-warning">
                          Kart Numarası
                        </label>
                        <input
                          type="text"
                          name="cardNumber"
                          className="form-control form-control-lg border-0 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderRadius: "15px",
                            padding: "1rem 1.5rem",
                          }}
                          placeholder="1234 5678 9012 3456"
                          value={formData.cardNumber}
                          onChange={handleCardNumberChange}
                          maxLength="19"
                          required
                        />
                        {errors.cardNumber && (
                          <small className="text-danger">
                            {errors.cardNumber}
                          </small>
                        )}
                      </div>
                      <div className="col-md-6 mb-3">
                        <label className="form-label fw-bold text-warning">
                          Kart Üzerindeki İsim
                        </label>
                        <input
                          type="text"
                          name="cardName"
                          className="form-control form-control-lg border-0 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderRadius: "15px",
                            padding: "1rem 1.5rem",
                          }}
                          placeholder="JOHN DOE"
                          value={formData.cardName}
                          onChange={handleInputChange}
                          required
                        />
                        {errors.cardName && (
                          <small className="text-danger">
                            {errors.cardName}
                          </small>
                        )}
                      </div>
                    </div>
                    <div className="row">
                      <div className="col-md-4 mb-3">
                        <label className="form-label fw-bold text-warning">
                          Ay
                        </label>
                        <select
                          name="expiryMonth"
                          className="form-select form-select-lg border-0 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderRadius: "15px",
                            padding: "1rem 1.5rem",
                          }}
                          value={formData.expiryMonth}
                          onChange={handleInputChange}
                          required
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
                          <small className="text-danger">
                            {errors.expiryMonth}
                          </small>
                        )}
                      </div>
                      <div className="col-md-4 mb-3">
                        <label className="form-label fw-bold text-warning">
                          Yıl
                        </label>
                        <select
                          name="expiryYear"
                          className="form-select form-select-lg border-0 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderRadius: "15px",
                            padding: "1rem 1.5rem",
                          }}
                          value={formData.expiryYear}
                          onChange={handleInputChange}
                          required
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
                          <small className="text-danger">
                            {errors.expiryYear}
                          </small>
                        )}
                      </div>
                      <div className="col-md-4 mb-3">
                        <label className="form-label fw-bold text-warning">
                          CVV
                        </label>
                        <input
                          type="text"
                          name="cvv"
                          className="form-control form-control-lg border-0 shadow-sm"
                          style={{
                            backgroundColor: "#fff8f0",
                            borderRadius: "15px",
                            padding: "1rem 1.5rem",
                          }}
                          placeholder="123"
                          value={formData.cvv}
                          onChange={handleInputChange}
                          maxLength="4"
                          required
                        />
                        {errors.cvv && (
                          <small className="text-danger">{errors.cvv}</small>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* Havale/EFT Bilgi Kartı */}
              {paymentMethod === "bankTransfer" && (
                <div
                  className="card shadow-lg border-0 mb-4"
                  style={{ borderRadius: 20 }}
                >
                  <div
                    className="card-header text-white border-0"
                    style={{
                      background: "linear-gradient(45deg, #6f42c1, #6610f2)",
                      borderTopLeftRadius: 20,
                      borderTopRightRadius: 20,
                      padding: "1.25rem",
                    }}
                  >
                    <h5 className="mb-0 fw-bold">
                      <i className="fas fa-university me-2"></i>Havale/EFT
                      Bilgileri
                    </h5>
                  </div>
                  <div className="card-body" style={{ padding: "1.5rem" }}>
                    <div className="row g-3">
                      <div className="col-md-6">
                        <div className="p-3 bg-light rounded">
                          <div className="text-muted">Banka</div>
                          <div className="fw-bold">ABC Bank</div>
                        </div>
                      </div>
                      <div className="col-md-6">
                        <div className="p-3 bg-light rounded">
                          <div className="text-muted">Hesap Adı</div>
                          <div className="fw-bold">Gölköy Gourmet Market</div>
                        </div>
                      </div>
                      <div className="col-md-12">
                        <div className="p-3 bg-light rounded d-flex justify-content-between align-items-center">
                          <div>
                            <div className="text-muted">IBAN</div>
                            <div className="fw-bold">
                              TR00 0000 0000 0000 0000 0000 00
                            </div>
                          </div>
                          <button
                            type="button"
                            className="btn btn-outline-primary"
                            onClick={() =>
                              navigator.clipboard.writeText(
                                "TR0000000000000000000000"
                              )
                            }
                          >
                            <i className="fas fa-copy me-2"></i>Kopyala
                          </button>
                        </div>
                      </div>
                    </div>
                    <div
                      className="mt-3 text-muted"
                      style={{ fontSize: "0.95rem" }}
                    >
                      Lütfen açıklama kısmına sipariş numaranızı yazınız.
                      Sipariş tamamlandıktan sonra “Sipariş Başarı” sayfasında
                      sipariş numaranız görüntülenecektir.
                    </div>
                  </div>
                </div>
              )}

              {/* Teslimat Bilgileri */}
              <div
                className="card shadow-lg border-0 mb-4"
                style={{ borderRadius: "20px" }}
              >
                <div
                  className="card-header text-white border-0"
                  style={{
                    background: "linear-gradient(45deg, #6f42c1, #e83e8c)",
                    borderTopLeftRadius: "20px",
                    borderTopRightRadius: "20px",
                    padding: "1.5rem",
                  }}
                >
                  <h5 className="mb-0 fw-bold">
                    <i className="fas fa-truck me-2"></i>Teslimat Bilgileri
                  </h5>
                </div>
                <div className="card-body" style={{ padding: "2rem" }}>
                  <div className="row">
                    <div className="col-md-6 mb-3">
                      <label className="form-label fw-bold text-warning">
                        Ad
                      </label>
                      <input
                        type="text"
                        name="firstName"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.firstName}
                        onChange={handleInputChange}
                        required
                      />
                    </div>
                    <div className="col-md-6 mb-3">
                      <label className="form-label fw-bold text-warning">
                        Soyad
                      </label>
                      <input
                        type="text"
                        name="lastName"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.lastName}
                        onChange={handleInputChange}
                        required
                      />
                    </div>
                  </div>
                  <div className="row">
                    <div className="col-md-6 mb-3">
                      <label className="form-label fw-bold text-warning">
                        E-posta
                      </label>
                      <input
                        type="email"
                        name="email"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.email}
                        onChange={handleInputChange}
                        required
                      />
                    </div>
                    <div className="col-md-6 mb-3">
                      <label className="form-label fw-bold text-warning">
                        Telefon
                      </label>
                      <input
                        type="tel"
                        name="phone"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.phone}
                        onChange={handleInputChange}
                        required
                      />
                    </div>
                  </div>
                  <div className="mb-3">
                    <label className="form-label fw-bold text-warning">
                      Adres
                    </label>
                    <textarea
                      name="address"
                      className="form-control form-control-lg border-0 shadow-sm"
                      style={{
                        backgroundColor: "#fff8f0",
                        borderRadius: "15px",
                        padding: "1rem 1.5rem",
                      }}
                      rows="3"
                      value={formData.address}
                      onChange={handleInputChange}
                      required
                    />
                  </div>
                  <div className="row">
                    <div className="col-md-4 mb-3">
                      <label className="form-label fw-bold text-warning">
                        İl
                      </label>
                      <input
                        type="text"
                        name="city"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.city}
                        onChange={handleInputChange}
                        required
                      />
                    </div>
                    <div className="col-md-4 mb-3">
                      <label className="form-label fw-bold text-warning">
                        İlçe
                      </label>
                      <input
                        type="text"
                        name="district"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.district}
                        onChange={handleInputChange}
                      />
                    </div>
                    <div className="col-md-4 mb-3">
                      <label className="form-label fw-bold text-warning">
                        Posta Kodu
                      </label>
                      <input
                        type="text"
                        name="postalCode"
                        className="form-control form-control-lg border-0 shadow-sm"
                        style={{
                          backgroundColor: "#fff8f0",
                          borderRadius: "15px",
                          padding: "1rem 1.5rem",
                        }}
                        value={formData.postalCode}
                        onChange={handleInputChange}
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* Teslimat Zamanı */}
              <div
                className="card shadow-lg border-0 mb-4"
                style={{ borderRadius: "20px" }}
              >
                <div
                  className="card-header text-white border-0"
                  style={{
                    background: "linear-gradient(45deg, #20c997, #28a745)",
                    borderTopLeftRadius: "20px",
                    borderTopRightRadius: "20px",
                    padding: "1.5rem",
                  }}
                >
                  <h5 className="mb-0 fw-bold">
                    <i className="fas fa-clock me-2"></i>Teslimat Zamanı
                  </h5>
                </div>
                <div className="card-body" style={{ padding: "2rem" }}>
                  <div className="mb-3">
                    <div className="form-check">
                      <input
                        className="form-check-input"
                        type="radio"
                        name="deliverySlot"
                        id="slotStandard"
                        checked={deliverySlot === "standard"}
                        onChange={() => setDeliverySlot("standard")}
                      />
                      <label
                        className="form-check-label"
                        htmlFor="slotStandard"
                      >
                        Gün içinde (Standart)
                      </label>
                    </div>
                    <div className="form-check">
                      <input
                        className="form-check-input"
                        type="radio"
                        name="deliverySlot"
                        id="slotMorning"
                        checked={deliverySlot === "10:00-13:00"}
                        onChange={() => setDeliverySlot("10:00-13:00")}
                      />
                      <label className="form-check-label" htmlFor="slotMorning">
                        10:00 - 13:00
                      </label>
                    </div>
                    <div className="form-check">
                      <input
                        className="form-check-input"
                        type="radio"
                        name="deliverySlot"
                        id="slotAfternoon"
                        checked={deliverySlot === "13:00-16:00"}
                        onChange={() => setDeliverySlot("13:00-16:00")}
                      />
                      <label
                        className="form-check-label"
                        htmlFor="slotAfternoon"
                      >
                        13:00 - 16:00
                      </label>
                    </div>
                    <div className="form-check">
                      <input
                        className="form-check-input"
                        type="radio"
                        name="deliverySlot"
                        id="slotEvening"
                        checked={deliverySlot === "16:00-20:00"}
                        onChange={() => setDeliverySlot("16:00-20:00")}
                      />
                      <label className="form-check-label" htmlFor="slotEvening">
                        16:00 - 20:00
                      </label>
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="form-label fw-bold text-warning">
                      Teslimat Notu
                    </label>
                    <textarea
                      className="form-control form-control-lg border-0 shadow-sm"
                      style={{
                        backgroundColor: "#fff8f0",
                        borderRadius: 15,
                        padding: "1rem 1.5rem",
                      }}
                      rows="3"
                      placeholder="Örn. Kapı önüne bırakın, güvenlikte bırakın vb."
                      value={deliveryNote}
                      onChange={(e) => setDeliveryNote(e.target.value)}
                    />
                  </div>
                </div>
              </div>
            </div>

            {/* Sağ Taraf: Sipariş Özeti */}
            <div className="col-lg-4">
              <div
                className="card shadow-lg border-0 position-sticky"
                style={{ borderRadius: "20px", top: "20px" }}
              >
                <div
                  className="card-header text-white border-0"
                  style={{
                    background: "linear-gradient(45deg, #28a745, #20c997)",
                    borderTopLeftRadius: "20px",
                    borderTopRightRadius: "20px",
                    padding: "1.5rem",
                  }}
                >
                  <h5 className="mb-0 fw-bold">
                    <i className="fas fa-receipt me-2"></i>Sipariş Özeti
                  </h5>
                </div>
                <div className="card-body" style={{ padding: "2rem" }}>
                  {/* Ürünler */}
                  <div className="mb-4">
                    <h6 className="text-warning fw-bold mb-3">
                      Sepetinizdeki Ürünler
                    </h6>
                    {cartItems.map((item) => (
                      <div
                        key={item.id}
                        className="d-flex align-items-center mb-3 pb-3 border-bottom"
                      >
                        <div
                          className="me-3 d-flex align-items-center justify-content-center"
                          style={{
                            width: 50,
                            height: 50,
                            backgroundColor: "#fff8f0",
                            borderRadius: 10,
                          }}
                        >
                          {products[item.productId]?.imageUrl ? (
                            <img
                              src={products[item.productId].imageUrl}
                              alt={products[item.productId]?.name || "Ürün"}
                              style={{
                                maxWidth: "100%",
                                maxHeight: "100%",
                                borderRadius: 8,
                                objectFit: "contain",
                              }}
                              onError={(e) => {
                                e.currentTarget.style.display = "none";
                              }}
                            />
                          ) : (
                            <span role="img" aria-label="box">
                              📦
                            </span>
                          )}
                        </div>
                        <div className="flex-grow-1">
                          <h6 className="mb-1" style={{ fontSize: "0.9rem" }}>
                            {products[item.productId]?.name || "Ürün"}
                          </h6>
                          <small className="text-muted">
                            {item.quantity} x ₺
                            {Number(
                              products[item.productId]?.specialPrice ||
                                products[item.productId]?.price ||
                                0
                            ).toFixed(2)}
                          </small>
                        </div>
                        <strong className="text-warning">
                          ₺
                          {Number(
                            item.quantity *
                              (products[item.productId]?.specialPrice ||
                                products[item.productId]?.price ||
                                0)
                          ).toFixed(2)}
                        </strong>
                      </div>
                    ))}
                  </div>

                  {/* Teslimat Yöntemi */}
                  <div className="mb-4">
                    <h6 className="text-warning fw-bold mb-2">
                      Teslimat Yöntemi
                    </h6>
                    <div className="form-check">
                      <input
                        className="form-check-input"
                        type="radio"
                        name="shippingMethod"
                        id="shipStandard"
                        checked={shippingMethod === "standard"}
                        onChange={() => setShippingMethod("standard")}
                      />
                      <label
                        className="form-check-label"
                        htmlFor="shipStandard"
                      >
                        Standart Teslimat (₺15) — 2-3 gün
                      </label>
                    </div>
                    <div className="form-check">
                      <input
                        className="form-check-input"
                        type="radio"
                        name="shippingMethod"
                        id="shipExpress"
                        checked={shippingMethod === "express"}
                        onChange={() => setShippingMethod("express")}
                      />
                      <label className="form-check-label" htmlFor="shipExpress">
                        Hızlı Teslimat (₺30) — 24 saat
                      </label>
                    </div>
                  </div>

                  {/* Kupon Kodu */}
                  <div className="mb-4">
                    <h6 className="text-warning fw-bold mb-2">Kupon Kodu</h6>
                    <div className="input-group">
                      <input
                        type="text"
                        className="form-control"
                        placeholder="Kupon kodunuzu girin"
                        value={couponCode}
                        onChange={(e) => setCouponCode(e.target.value)}
                      />
                      <button
                        className="btn btn-warning fw-bold"
                        type="button"
                        disabled={pricingLoading || cartItems.length === 0}
                        onClick={handleApplyCoupon}
                      >
                        {pricingLoading ? "Uygulanıyor..." : "Kupon Uygula"}
                      </button>
                    </div>
                    {pricingError && (
                      <div className="alert alert-danger mt-2 py-2 mb-0">
                        {pricingError}
                      </div>
                    )}
                  </div>

                  {/* Toplam Hesapları */}
                  <div className="mb-4">
                    <div className="d-flex justify-content-between mb-2">
                      <span>Ara Toplam:</span>
                      <span>₺{subtotal.toFixed(2)}</span>
                    </div>
                    <div className="d-flex justify-content-between mb-2">
                      <span>Kargo:</span>
                      <span className={shipping === 0 ? "text-success" : ""}>
                        {shipping === 0
                          ? "Ücretsiz"
                          : `₺${shipping.toFixed(2)}`}
                      </span>
                    </div>
                    <div className="d-flex justify-content-between mb-2">
                      <span>KDV (%18):</span>
                      <span>₺{tax.toFixed(2)}</span>
                    </div>
                    <hr />
                    <div className="d-flex justify-content-between fw-bold fs-5">
                      <span>Toplam:</span>
                      <span className="text-warning">₺{total.toFixed(2)}</span>
                    </div>

                    {pricing && (
                      <div className="mt-3">
                        <div className="d-flex justify-content-between mb-1">
                          <span>Ara Toplam</span>
                          <span>
                            ₺{Number(pricing.subtotal || 0).toFixed(2)}
                          </span>
                        </div>
                        <div className="d-flex justify-content-between mb-1">
                          <span>Kampanya İndirimi</span>
                          <span className="text-success">
                            -₺
                            {Number(
                              pricing.campaignDiscountTotal || 0
                            ).toFixed(2)}
                          </span>
                        </div>
                        <div className="d-flex justify-content-between mb-1">
                          <span>Kupon İndirimi</span>
                          <span className="text-success">
                            -₺
                            {Number(
                              pricing.couponDiscountTotal || 0
                            ).toFixed(2)}
                          </span>
                        </div>
                        <div className="d-flex justify-content-between mb-1">
                          <span>Kargo Ücreti</span>
                          <span>
                            ₺{Number(pricing.deliveryFee || 0).toFixed(2)}
                          </span>
                        </div>
                        <hr />
                        <div className="d-flex justify-content-between fw-bold">
                          <span>Genel Toplam</span>
                          <span className="text-warning">
                            ₺{Number(pricing.grandTotal || 0).toFixed(2)}
                          </span>
                        </div>
                        {pricing.appliedCouponCode && (
                          <div className="small text-muted mt-1">
                            Uygulanan Kupon:{" "}
                            <strong>{pricing.appliedCouponCode}</strong>
                          </div>
                        )}
                        {Array.isArray(pricing.appliedCampaignNames) &&
                          pricing.appliedCampaignNames.length > 0 && (
                            <div className="small text-muted">
                              Kampanyalar:{" "}
                              {pricing.appliedCampaignNames.join(", ")}
                            </div>
                          )}
                      </div>
                    )}
                  </div>

                  {/* Ödeme Butonu */}
                  <button
                    type="submit"
                    disabled={processing}
                    className="btn btn-lg w-100 text-white fw-bold shadow-lg border-0"
                    style={{
                      background: processing
                        ? "linear-gradient(45deg, #6c757d, #adb5bd)"
                        : "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)",
                      borderRadius: "15px",
                      padding: "1rem",
                    }}
                  >
                    {processing ? (
                      <>
                        <span
                          className="spinner-border spinner-border-sm me-2"
                          role="status"
                        ></span>
                        İşleniyor...
                      </>
                    ) : (
                      <>
                        <i className="fas fa-lock me-2"></i>
                        Güvenli Ödeme Yap
                      </>
                    )}
                  </button>

                  {/* Güvenlik Rozeti */}
                  <div className="text-center mt-3">
                    <small className="text-muted d-flex align-items-center justify-content-center">
                      <i className="fas fa-shield-alt text-success me-2"></i>
                      256-bit SSL ile güvence altında
                    </small>
                  </div>
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
