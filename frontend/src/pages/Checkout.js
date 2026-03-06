// ============================================================================
// CHECKOUT SAYFASI
// Adres, kargo, ödeme adımları - Hem misafir hem kayıtlı kullanıcı için
// Varyant bilgileri dahil sipariş oluşturma
// POSNET Kredi Kartı Entegrasyonu Dahil
// Ağırlık Bazlı Ürün Bilgilendirmesi Dahil
// SAYFA YENİLEME / GERİ BUTONU KORUMASI DAHİL
// ============================================================================
import React, {
  useEffect,
  useState,
  useMemo,
  useCallback,
  useRef,
} from "react";
import api from "../services/api";
import { useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { useCart } from "../contexts/CartContext";
import { CartService } from "../services/cartService";
import { CampaignService } from "../services/campaignService";
import LoginModal from "../components/LoginModal";
import PosnetCreditCardForm from "../components/payment/PosnetCreditCardForm";
import { WeightBasedProductAlert } from "../components/weight";
import shippingService from "../services/shippingService";
import cartSettingsService from "../services/cartSettingsService";

export default function Checkout() {
  const [form, setForm] = useState({
    name: "",
    phone: "",
    email: "",
    address: "",
    city: "",
  });
  const [paymentMethod, setPaymentMethod] = useState("cash"); // Varsayılan: Kapıda ödeme (banka API sonra gelecek)
  const [shippingMethod, setShippingMethod] = useState("car");
  // Kargo fiyatları: API'den çekilecek, varsayılan değerler fallback olarak kullanılır
  const [shippingPrices, setShippingPrices] = useState({
    motorcycle: 40, // API'den gelene kadar varsayılan (eski 15 TL hatalıydı)
    car: 60, // API'den gelene kadar varsayılan (eski 30 TL hatalıydı)
  });
  const [shippingCost, setShippingCost] = useState(60); // Varsayılan: car fiyatı
  const [showLoginModal, setShowLoginModal] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [showCardForm, setShowCardForm] = useState(false);
  const [pendingOrderId, setPendingOrderId] = useState(null);
  const [pricing, setPricing] = useState(null);
  const [pricingLoading, setPricingLoading] = useState(false);
  const [pricingError, setPricingError] = useState("");
  const [appliedCampaigns, setAppliedCampaigns] = useState([]);
  const [campaignDiscountTotal, setCampaignDiscountTotal] = useState(0);

  // Minimum sepet tutarı state'leri
  const [cartSettings, setCartSettings] = useState(null);
  const [isCartAmountValid, setIsCartAmountValid] = useState(true);
  const [appliedCoupon, setAppliedCoupon] = useState(() => {
    try {
      return CartService.getAppliedCoupon();
    } catch {
      return null;
    }
  });
  const [clientOrderId] = useState(() => {
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
    return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  });

  // ============================================================================
  // SESSION TIMEOUT UYARISI STATE'LERİ
  // Kullanıcı checkout sayfasında uzun süre beklerse uyarı göster
  // ============================================================================
  const SESSION_TIMEOUT_MINUTES = 15; // 15 dakika session timeout
  const WARNING_BEFORE_MINUTES = 2; // Son 2 dakikada uyarı göster
  const [sessionTimeRemaining, setSessionTimeRemaining] = useState(
    SESSION_TIMEOUT_MINUTES * 60,
  );
  const [showSessionWarning, setShowSessionWarning] = useState(false);
  const [sessionExpired, setSessionExpired] = useState(false);
  const sessionTimerRef = useRef(null);

  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useAuth();
  const { cartItems, getCartTotal, clearCart } = useCart();

  // ============================================================================
  // SAYFA TERK ETME KORUMASI (beforeunload + popstate)
  // Kullanıcının ödeme sırasında yanlışlıkla sayfadan çıkmasını engelle
  // ============================================================================

  // Ödeme işlemi devam ediyor mu? (koruma aktif olmalı mı?)
  const isPaymentInProgress = useRef(false);

  // isPaymentInProgress güncelle
  useEffect(() => {
    isPaymentInProgress.current = submitting || showCardForm;
  }, [submitting, showCardForm]);

  // ============================================================================
  // 1. BEFOREUNLOAD - F5, sekme kapatma, sayfa yenileme koruması
  // ============================================================================
  useEffect(() => {
    const handleBeforeUnload = (e) => {
      // Sadece ödeme sırasında veya kart formu açıkken uyarı göster
      if (isPaymentInProgress.current) {
        const message =
          "Ödeme işlemi devam ediyor! Sayfadan ayrılırsanız işlem iptal edilebilir. Devam etmek istiyor musunuz?";
        e.preventDefault();
        e.returnValue = message; // Chrome için gerekli
        return message; // Diğer tarayıcılar için
      }
    };

    window.addEventListener("beforeunload", handleBeforeUnload);

    return () => {
      window.removeEventListener("beforeunload", handleBeforeUnload);
    };
  }, []);

  // ============================================================================
  // 2. POPSTATE - Geri butonu koruması (Browser back button)
  // Kullanıcı geri tuşuna basarsa uyarı göster
  // ============================================================================
  useEffect(() => {
    // Sayfa yüklendiğinde history state'e bir entry ekle
    // Bu sayede geri tuşuna basıldığında popstate event'i tetiklenir
    window.history.pushState({ checkout: true }, "", location.pathname);

    const handlePopState = (e) => {
      if (isPaymentInProgress.current) {
        // Ödeme devam ediyorsa uyarı göster
        const confirmLeave = window.confirm(
          "⚠️ Ödeme işlemi devam ediyor!\n\n" +
            "Geri dönerseniz ödeme işlemi iptal edilebilir ve sepetiniz kaybolabilir.\n\n" +
            "Devam etmek istiyor musunuz?",
        );

        if (!confirmLeave) {
          // Kullanıcı vazgeçti, sayfada kal
          window.history.pushState({ checkout: true }, "", location.pathname);
          return;
        }

        // Kullanıcı onayladı, ödeme formunu kapat ve geri git
        setShowCardForm(false);
        setPendingOrderId(null);
        setSubmitting(false);
      }

      // Geri gitmeye izin ver
      navigate(-1);
    };

    window.addEventListener("popstate", handlePopState);

    return () => {
      window.removeEventListener("popstate", handlePopState);
    };
  }, [location.pathname, navigate]);

  // ============================================================================
  // 3. SESSION TIMEOUT UYARISI
  // Kullanıcı checkout sayfasında 15 dakikadan fazla beklerse oturum sona erer
  // ============================================================================
  useEffect(() => {
    // Session timer başlat
    sessionTimerRef.current = setInterval(() => {
      setSessionTimeRemaining((prev) => {
        const newTime = prev - 1;

        // Son 2 dakikada uyarı göster
        if (newTime <= WARNING_BEFORE_MINUTES * 60 && newTime > 0) {
          setShowSessionWarning(true);
        }

        // Süre doldu
        if (newTime <= 0) {
          setSessionExpired(true);
          setShowSessionWarning(false);
          clearInterval(sessionTimerRef.current);
          return 0;
        }

        return newTime;
      });
    }, 1000);

    return () => {
      if (sessionTimerRef.current) {
        clearInterval(sessionTimerRef.current);
      }
    };
  }, []);

  // Session'ı yenile (kullanıcı aktif olduğunda)
  const refreshSession = useCallback(() => {
    setSessionTimeRemaining(SESSION_TIMEOUT_MINUTES * 60);
    setShowSessionWarning(false);
    setSessionExpired(false);
    console.log("[Checkout] ✅ Session yenilendi");
  }, []);

  // Formatlanmış kalan süre
  const formatTimeRemaining = useCallback((seconds) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, "0")}`;
  }, []);

  // ===========================================================================
  // KARGO FİYATLARINI API'DEN ÇEK (Component mount olduğunda)
  // Neden: Varsayılan hardcoded değerler yerine veritabanındaki güncel fiyatları kullan
  // ===========================================================================
  useEffect(() => {
    let mounted = true;

    const loadShippingPrices = async () => {
      try {
        const settings = await shippingService.getActiveSettings();
        if (!mounted || !settings || settings.length === 0) return;

        // API'den gelen fiyatları state'e yaz
        const motoSetting = settings.find(
          (s) => s.vehicleType?.toLowerCase() === "motorcycle",
        );
        const carSetting = settings.find(
          (s) => s.vehicleType?.toLowerCase() === "car",
        );

        const newPrices = {
          motorcycle: motoSetting?.price ?? 40, // API'den gelmezse fallback
          car: carSetting?.price ?? 60, // API'den gelmezse fallback
        };

        setShippingPrices(newPrices);

        // Mevcut seçime göre kargo ücretini güncelle
        setShippingCost(
          shippingMethod === "motorcycle"
            ? newPrices.motorcycle
            : newPrices.car,
        );

        console.log("[Checkout] ✅ Kargo fiyatları yüklendi:", newPrices);
      } catch (error) {
        console.warn(
          "[Checkout] ⚠️ Kargo fiyatları yüklenemedi, varsayılan kullanılıyor:",
          error,
        );
        // Hata durumunda varsayılan değerler zaten state'te mevcut
      }
    };

    loadShippingPrices();

    return () => {
      mounted = false;
    };
  }, []); // Sadece mount'ta çalış

  // ===========================================================================
  // MİNİMUM SEPET TUTARI AYARLARINI YÜKLEME
  // ===========================================================================
  useEffect(() => {
    let mounted = true;
    const loadCartSettings = async () => {
      try {
        const settings = await cartSettingsService.getCartSettings();
        if (mounted) setCartSettings(settings);
      } catch (error) {
        console.warn("[Checkout] Sepet ayarları yüklenemedi:", error.message);
      }
    };
    loadCartSettings();
    return () => {
      mounted = false;
    };
  }, []);

  // Sepet toplamı değiştiğinde minimum tutar kontrolü
  useEffect(() => {
    if (cartSettings?.isMinimumCartAmountActive) {
      const total = getCartTotal();
      setIsCartAmountValid(total >= cartSettings.minimumCartAmount);
    } else {
      setIsCartAmountValid(true);
    }
  }, [cartItems, cartSettings, getCartTotal]);

  // ===========================================================================
  // KARGO YÖNTEMİ DEĞİŞTİĞİNDE FİYATI GÜNCELLE
  // ===========================================================================
  useEffect(() => {
    // Seçilen kargo yöntemine göre ücreti güncelle (API'den çekilen fiyatlardan)
    setShippingCost(
      shippingMethod === "motorcycle"
        ? shippingPrices.motorcycle
        : shippingPrices.car,
    );
  }, [shippingMethod, shippingPrices]);

  // Kampanya/kupon özetini çek (checkout görünümü için)
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
          shippingMethod: shippingMethod,
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
  }, [cartItems, appliedCoupon?.code, shippingMethod]);

  useEffect(() => {
    if (user) {
      setForm((prev) => ({
        ...prev,
        name: user.fullName || `${user.firstName} ${user.lastName}`,
        email: user.email,
      }));
    }
  }, [user]);

  // ============================================================================
  // AĞIRLIK BAZLI ÜRÜNLERİ TESPİT ET
  // Sepetteki ağırlık bazlı ürünleri filtrele ve checkout alert'inde göster
  // ============================================================================
  const weightBasedItems = useMemo(() => {
    return cartItems
      .filter((item) => {
        // Product bilgisi varsa oradan, yoksa item'dan kontrol et
        return (
          item.isWeightBased ||
          item.product?.isWeightBased ||
          item.weightUnit === "Kilogram" ||
          item.weightUnit === "Gram" ||
          item.weightUnit === 2 ||
          item.weightUnit === 1 ||
          item.product?.weightUnit === "Kilogram" ||
          item.product?.weightUnit === "Gram"
        );
      })
      .map((item) => ({
        ...item,
        name: item.productName || item.product?.name || "Ürün",
        weightUnit: item.weightUnit || item.product?.weightUnit,
        estimatedPrice: item.unitPrice || item.product?.price || 0,
        isWeightBased: true,
      }));
  }, [cartItems]);

  // ============================================================================
  // SİPARİŞ GÖNDERME
  // Sepet verileri + varyant bilgileri + teslimat bilgileri
  // Banka API entegrasyonu sonra eklenecek (şimdilik kapıda ödeme/havale)
  // ============================================================================
  // ============================================================================
  const submit = async (e) => {
    e.preventDefault();

    // Sepet boş kontrolü
    if (!cartItems || cartItems.length === 0) {
      alert("❌ Sepetiniz boş! Sipariş veremezsiniz.");
      navigate("/");
      return;
    }

    // Minimum sepet tutarı kontrolü
    if (!isCartAmountValid) {
      alert("❌ Minimum sepet tutarına ulaşılamadı. Lütfen sepetinize daha fazla ürün ekleyin.");
      return;
    }

    // Form validasyonu
    if (!form.name?.trim() || !form.phone?.trim() || !form.address?.trim()) {
      alert("❌ Lütfen tüm zorunlu alanları doldurun.");
      return;
    }

    // Telefon format kontrolü (basit)
    const phoneRegex = /^[0-9]{10,11}$/;
    if (!phoneRegex.test(form.phone.replace(/\s/g, ""))) {
      alert("❌ Geçerli bir telefon numarası girin (10-11 haneli).");
      return;
    }

    setSubmitting(true);

    try {
      // ================================================================
      // SİPARİŞ PAYLOAD - VARYANT BİLGİLERİ DAHİL
      // Backend'e gönderilecek sipariş verisi
      // ================================================================
      const orderItems = cartItems.map((item) => ({
        productId: item.productId || item.id,
        quantity: item.quantity,
        unitPrice: item.unitPrice || item.product?.price || 0,
        // Varyant bilgileri
        variantId: item.variantId || null,
        sku: item.sku || null,
        variantTitle: item.variantTitle || null,
      }));

      const payload = {
        // Müşteri bilgileri
        customerName: form.name.trim(),
        customerPhone: form.phone.trim(),
        customerEmail: form.email?.trim() || null,

        // Teslimat bilgileri
        shippingAddress: form.address.trim(),
        shippingCity: form.city?.trim() || "",
        shippingMethod,
        shippingCost,

        // Ödeme bilgileri
        paymentMethod, // "cash" (kapıda) veya "bank_transfer" (havale) veya "card" (banka API sonra)

        // Sipariş detayları
        items: orderItems,
        subtotal: getCartTotal(),
        totalPrice: getCartTotal() + shippingCost,

        // Tekrar sipariş engelleme
        clientOrderId,
      };

      // ================================================================
      // API ÇAĞRISI - /api/orders/checkout endpoint'i
      // Hem misafir hem kayıtlı kullanıcı için çalışır
      // ================================================================
      const res = await api.post("/api/orders/checkout", payload);

      if (res.success || res.orderId) {
        // ================================================================
        // KREDİ KARTI ÖDEMESİ - POSNET FORMU GÖSTER
        // Sipariş oluşturuldu, şimdi ödeme alınacak
        // ================================================================
        if (paymentMethod === "credit_card") {
          setPendingOrderId(res.orderId || res.orderNumber);
          setShowCardForm(true);
          setSubmitting(false);
          return; // Ödeme tamamlanana kadar bekle
        }

        // Diğer ödeme yöntemleri için standart akış
        clearCart(); // Sepeti temizle

        // ================================================================
        // MİSAFİR KULLANICI İÇİN SİPARİŞ BİLGİSİNİ KAYDET
        // Sipariş geçmişinde görüntülenebilmesi için localStorage'a kaydet
        // NEDEN: Misafir kullanıcının token'ı yok, siparişlerini takip edebilmesi için
        // ================================================================
        if (!user) {
          try {
            const guestOrders = JSON.parse(
              localStorage.getItem("guestOrders") || "[]",
            );
            guestOrders.push({
              orderNumber: res.orderNumber || res.orderId,
              orderId: res.orderId,
              email: form.email?.trim(),
              totalPrice: res.finalPrice || payload.totalPrice,
              createdAt: new Date().toISOString(),
              status: "pending",
            });
            // Son 20 siparişi tut (eski siparişleri temizle)
            localStorage.setItem(
              "guestOrders",
              JSON.stringify(guestOrders.slice(-20)),
            );
            console.log(
              "[Checkout] ✅ Misafir siparişi localStorage'a kaydedildi",
            );
          } catch (e) {
            console.warn("[Checkout] ⚠️ Misafir siparişi kaydedilemedi:", e);
          }
        }

        // Başarı mesajı
        alert(
          `✅ Siparişiniz alındı!\n\nSipariş No: ${res.orderNumber || res.orderId}\nToplam: ₺${res.finalPrice?.toFixed(2) || payload.totalPrice.toFixed(2)}`,
        );

        // Siparişler sayfasına yönlendir - misafir de siparişlerini görebilir
        navigate("/orders");
      } else {
        throw new Error(res.message || "Sipariş oluşturulamadı");
      }
    } catch (err) {
      console.error("Sipariş hatası:", err);
      alert(
        "❌ Hata: " +
          (err.response?.data?.message ||
            err.message ||
            "Sipariş oluşturulurken bir hata oluştu"),
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="container mx-auto px-4 py-8">
      {/* ================================================================
          SESSION EXPIRED - Oturum sona erdi
          ================================================================ */}
      {sessionExpired && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md mx-4 shadow-xl">
            <div className="text-center">
              <div className="text-6xl mb-4">⏰</div>
              <h2 className="text-xl font-bold mb-2 text-red-600">
                Oturum Süresi Doldu
              </h2>
              <p className="text-gray-600 mb-4">
                Güvenliğiniz için checkout oturumunuz sona erdi. Lütfen sayfayı
                yenileyerek tekrar başlayın.
              </p>
              <button
                onClick={() => window.location.reload()}
                className="bg-orange-500 text-white px-6 py-2 rounded-lg font-semibold hover:bg-orange-600 transition-colors"
              >
                🔄 Sayfayı Yenile
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ================================================================
          SESSION WARNING - Oturum süresi dolmak üzere
          ================================================================ */}
      {showSessionWarning && !sessionExpired && (
        <div className="fixed top-4 right-4 bg-yellow-100 border-l-4 border-yellow-500 p-4 rounded shadow-lg z-40 max-w-sm animate-pulse">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <span className="text-2xl">⚠️</span>
            </div>
            <div className="ml-3">
              <p className="text-sm text-yellow-700 font-semibold">
                Oturum süresi dolmak üzere!
              </p>
              <p className="text-xs text-yellow-600 mt-1">
                Kalan süre: {formatTimeRemaining(sessionTimeRemaining)}
              </p>
              <button
                onClick={refreshSession}
                className="mt-2 text-xs bg-yellow-500 text-white px-3 py-1 rounded hover:bg-yellow-600 transition-colors"
              >
                ⏱️ Süreyi Uzat
              </button>
            </div>
          </div>
        </div>
      )}

      <h1 className="text-2xl font-bold mb-4">Ödeme ve Adres</h1>
      <form onSubmit={submit} className="max-w-xl">
        <input
          required
          placeholder="Ad Soyad"
          value={form.name}
          onChange={(e) => {
            setForm({ ...form, name: e.target.value });
            refreshSession();
          }}
          className="w-full mb-2 border p-2"
        />
        <input
          required
          placeholder="Telefon"
          value={form.phone}
          onChange={(e) => {
            setForm({ ...form, phone: e.target.value });
            refreshSession();
          }}
          className="w-full mb-2 border p-2"
        />
        <input
          required
          placeholder="E-posta"
          value={form.email}
          onChange={(e) => {
            setForm({ ...form, email: e.target.value });
            refreshSession();
          }}
          className="w-full mb-2 border p-2"
        />
        <input
          required
          placeholder="İl"
          value={form.city || ""}
          onChange={(e) => {
            setForm({ ...form, city: e.target.value });
            refreshSession();
          }}
          className="w-full mb-2 border p-2"
        />
        <textarea
          required
          placeholder="Adres"
          value={form.address}
          onChange={(e) => {
            setForm({ ...form, address: e.target.value });
            refreshSession();
          }}
          className="w-full mb-2 border p-2"
        />

        {/* Kargo Seçeneği */}
        <div className="mb-4">
          <label
            className="block mb-2 font-semibold"
            style={{ color: "#FF8C00" }}
          >
            🚚 Kargo Tipi Seçin
          </label>
          <div className="d-flex gap-3">
            <div
              onClick={() => setShippingMethod("car")}
              className="flex-fill p-3 border rounded cursor-pointer"
              style={{
                borderColor: shippingMethod === "car" ? "#FF8C00" : "#ddd",
                borderWidth: shippingMethod === "car" ? "3px" : "1px",
                backgroundColor: shippingMethod === "car" ? "#FFF5E6" : "white",
                borderRadius: "15px",
                cursor: "pointer",
                transition: "all 0.3s",
              }}
            >
              <div className="text-center">
                <div style={{ fontSize: "2rem" }}>🚗</div>
                <div className="fw-bold mt-2">Araç</div>
                <div className="text-muted small">{shippingPrices.car} ₺</div>
              </div>
            </div>
            <div
              onClick={() => setShippingMethod("motorcycle")}
              className="flex-fill p-3 border rounded cursor-pointer"
              style={{
                borderColor:
                  shippingMethod === "motorcycle" ? "#FF8C00" : "#ddd",
                borderWidth: shippingMethod === "motorcycle" ? "3px" : "1px",
                backgroundColor:
                  shippingMethod === "motorcycle" ? "#FFF5E6" : "white",
                borderRadius: "15px",
                cursor: "pointer",
                transition: "all 0.3s",
              }}
            >
              <div className="text-center">
                <div style={{ fontSize: "2rem" }}>🏍️</div>
                <div className="fw-bold mt-2">Motosiklet</div>
                <div className="text-muted small">
                  {shippingPrices.motorcycle} ₺
                </div>
              </div>
            </div>
          </div>
          <div className="mt-2 text-end">
            <strong style={{ color: "#FF8C00" }}>
              Kargo Ücreti: {shippingCost} ₺
            </strong>
          </div>
        </div>

        {/* Ağırlık Bazlı Ürün Bilgilendirmesi (Checkout Variant) */}
        <WeightBasedProductAlert
          weightBasedItems={weightBasedItems}
          variant="checkout"
          showDetails={true}
        />

        <div className="mb-4">
          <label
            className="block mb-1 font-semibold"
            style={{ color: "#FF8C00" }}
          >
            💳 Ödeme Yöntemi
          </label>
          <select
            value={paymentMethod}
            onChange={(e) => setPaymentMethod(e.target.value)}
            className="border p-2 w-full rounded"
            style={{ borderColor: "#FF8C00" }}
          >
            <option value="cash">💵 Kapıda Nakit Ödeme</option>
            <option value="cash_card">💳 Kapıda Kart ile Ödeme</option>
            <option value="bank_transfer">🏦 Havale / EFT</option>
            {/* POSNET Kredi Kartı ile Online Ödeme */}
            <option value="credit_card">
              💳 Kredi Kartı ile Online Öde (3D Secure)
            </option>
          </select>
          {paymentMethod === "bank_transfer" && (
            <div
              className="mt-2 p-3 rounded"
              style={{
                background: "#FFF5E6",
                border: "1px solid #FFE0B2",
                fontSize: "0.85rem",
              }}
            >
              <p className="mb-1 fw-bold" style={{ color: "#FF8C00" }}>
                <i className="fas fa-info-circle me-1"></i>
                Havale Bilgileri:
              </p>
              <p className="mb-0 small">
                Siparişiniz, ödemeniz onaylandıktan sonra hazırlanacaktır.
                <br />
                Banka bilgileri sipariş onay ekranında gösterilecektir.
              </p>
            </div>
          )}
          {paymentMethod === "credit_card" && (
            <div
              className="mt-2 p-3 rounded"
              style={{
                background: "#E3F2FD",
                border: "1px solid #90CAF9",
                fontSize: "0.85rem",
              }}
            >
              <p className="mb-1 fw-bold" style={{ color: "#1976D2" }}>
                <i className="fas fa-shield-alt me-1"></i>
                🔒 3D Secure Güvenli Ödeme
              </p>
              <p className="mb-0 small">
                Yapı Kredi POSNET altyapısı ile güvenli online ödeme.
                <br />
                Taksit seçenekleri ve World Puan kullanımı mevcuttur.
              </p>
            </div>
          )}
        </div>

        {/* Kampanya & Kupon Özeti */}
        <div
          className="mb-4 p-3 rounded"
          style={{
            background: "#FFF8F0",
            border: "1px solid #FFE0B2",
            borderRadius: "14px",
          }}
        >
          <div className="d-flex align-items-center justify-content-between mb-2">
            <strong style={{ color: "#FF8C00" }}>🎁 Kampanya Özeti</strong>
            {pricingLoading && (
              <span className="small text-muted">Hesaplanıyor...</span>
            )}
          </div>

          {pricingError && (
            <div className="alert alert-warning py-2 mb-2">
              <i className="fas fa-exclamation-triangle me-2"></i>
              {pricingError}
            </div>
          )}

          {appliedCampaigns.length > 0 ? (
            <div className="mb-2">
              {appliedCampaigns.map((campaign, index) => (
                <div
                  key={`${campaign.id || campaign.name || index}`}
                  className="d-flex align-items-center justify-content-between"
                  style={{ fontSize: "0.85rem", padding: "4px 0" }}
                >
                  <span className="text-muted d-flex align-items-center gap-2">
                    <i
                      className={`fas ${CampaignService.getCampaignBadge(campaign.type).icon}`}
                      style={{ color: "#FF8C00" }}
                    ></i>
                    {campaign.displayText ||
                      campaign.campaignName ||
                      campaign.name ||
                      CampaignService.getDiscountText(campaign)}
                  </span>
                  {campaign.discountAmount > 0 && (
                    <span className="text-success fw-semibold">
                      -₺{campaign.discountAmount.toFixed(2)}
                    </span>
                  )}
                </div>
              ))}
            </div>
          ) : (
            <div className="small text-muted mb-2">
              Şu an için uygulanabilir kampanya bulunamadı.
            </div>
          )}

          <div className="border-top pt-2">
            <div className="d-flex justify-content-between small">
              <span>Ara Toplam</span>
              <span>₺{getCartTotal().toFixed(2)}</span>
            </div>

            {(campaignDiscountTotal || pricing?.campaignDiscountTotal) > 0 && (
              <div className="d-flex justify-content-between small text-success">
                <span>Kampanya İndirimi</span>
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

            {(pricing?.couponDiscountTotal || appliedCoupon?.discountAmount) >
              0 && (
              <div className="d-flex justify-content-between small text-success">
                <span>Kupon İndirimi</span>
                <span>
                  -₺
                  {(
                    pricing?.couponDiscountTotal ||
                    appliedCoupon?.discountAmount ||
                    0
                  ).toFixed(2)}
                </span>
              </div>
            )}

            <div className="d-flex justify-content-between small">
              <span>Kargo</span>
              <span>₺{shippingCost.toFixed(2)}</span>
            </div>

            <div className="d-flex justify-content-between fw-bold mt-2">
              <span>Toplam</span>
              <span>
                ₺
                {Math.max(
                  0,
                  getCartTotal() +
                    shippingCost -
                    (campaignDiscountTotal ||
                      pricing?.campaignDiscountTotal ||
                      0) -
                    (pricing?.couponDiscountTotal ||
                      appliedCoupon?.discountAmount ||
                      0),
                ).toFixed(2)}
              </span>
            </div>
          </div>
        </div>

        {/* POSNET Kredi Kartı Formu */}
        {paymentMethod === "credit_card" && showCardForm && pendingOrderId && (
          <div className="mb-4">
            <PosnetCreditCardForm
              amount={getCartTotal() + shippingCost}
              orderId={pendingOrderId}
              showInstallments={true}
              onSuccess={(result) => {
                // Başarılı ödeme
                clearCart();

                // ================================================================
                // MİSAFİR KULLANICI İÇİN SİPARİŞ BİLGİSİNİ KAYDET (Kredi Kartı)
                // Ödeme başarılı olduğunda localStorage'a kaydet
                // NEDEN: 3D Secure sonrası sipariş görünmeme sorunu için
                // ================================================================
                if (!user) {
                  try {
                    const guestOrders = JSON.parse(
                      localStorage.getItem("guestOrders") || "[]",
                    );
                    guestOrders.push({
                      orderNumber: result.orderNumber || pendingOrderId,
                      orderId: result.orderId || pendingOrderId,
                      email: form.email?.trim(),
                      totalPrice: getCartTotal() + shippingCost,
                      createdAt: new Date().toISOString(),
                      status: "paid",
                      transactionId: result.transactionId,
                    });
                    localStorage.setItem(
                      "guestOrders",
                      JSON.stringify(guestOrders.slice(-20)),
                    );
                    console.log(
                      "[Checkout] ✅ Misafir kredi kartı siparişi localStorage'a kaydedildi",
                    );
                  } catch (e) {
                    console.warn(
                      "[Checkout] ⚠️ Misafir siparişi kaydedilemedi:",
                      e,
                    );
                  }
                }

                navigate(
                  `/checkout/success?orderId=${pendingOrderId}&transactionId=${result.transactionId || ""}`,
                );
              }}
              onError={(error) => {
                // Başarısız ödeme - formu gizleme, tekrar deneme imkanı
                alert(`❌ Ödeme hatası: ${error.message || "Bilinmeyen hata"}`);
                setShowCardForm(false);
                setPendingOrderId(null);
              }}
              onCancel={() => {
                setShowCardForm(false);
                setPendingOrderId(null);
              }}
            />
          </div>
        )}

        {/* Minimum Sepet Tutarı Uyarısı */}
        {!isCartAmountValid && cartSettings && (
          <div
            className="alert alert-warning d-flex align-items-start gap-2 mb-3"
            role="alert"
          >
            <i className="fas fa-exclamation-triangle mt-1"></i>
            <div>
              <strong>Minimum Sepet Tutarı</strong>
              <p className="mb-1 small">
                {(cartSettings.minimumCartAmountMessage || "").replace(
                  "{amount}",
                  (cartSettings.minimumCartAmount || 0).toLocaleString("tr-TR", {
                    minimumFractionDigits: 2,
                  })
                )}
              </p>
              <span className="fw-bold small">
                Kalan:{" "}
                {(cartSettings.minimumCartAmount - getCartTotal()).toLocaleString(
                  "tr-TR",
                  { minimumFractionDigits: 2 }
                )}{" "}
                ₺
              </span>
            </div>
          </div>
        )}

        <button
          type="submit"
          className="bg-green-600 text-white p-3 rounded w-full fw-bold"
          style={{
            background:
              submitting || !isCartAmountValid
                ? "#999"
                : "linear-gradient(135deg, #16a34a, #22c55e)",
            border: "none",
            cursor:
              submitting || !isCartAmountValid ? "not-allowed" : "pointer",
            fontSize: "1.1rem",
          }}
          disabled={submitting || cartItems.length === 0 || !isCartAmountValid}
        >
          {submitting ? (
            <>
              <span className="spinner-border spinner-border-sm me-2"></span>
              Sipariş Gönderiliyor...
            </>
          ) : (
            <>
              <i className="fas fa-check-circle me-2"></i>
              Siparişi Onayla (₺{(getCartTotal() + shippingCost).toFixed(2)})
            </>
          )}
        </button>
      </form>
      <LoginModal
        show={showLoginModal}
        onClose={() => setShowLoginModal(false)}
      />
    </div>
  );
}
