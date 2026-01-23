// ============================================================================
// CHECKOUT SAYFASI
// Adres, kargo, ödeme adımları - Hem misafir hem kayıtlı kullanıcı için
// Varyant bilgileri dahil sipariş oluşturma
// POSNET Kredi Kartı Entegrasyonu Dahil
// Ağırlık Bazlı Ürün Bilgilendirmesi Dahil
// ============================================================================
import React, { useEffect, useState, useMemo } from "react";
import api from "../services/api";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { useCart } from "../contexts/CartContext";
import { CartService } from "../services/cartService";
import { CampaignService } from "../services/campaignService";
import LoginModal from "../components/LoginModal";
import PosnetCreditCardForm from "../components/payment/PosnetCreditCardForm";
import { WeightBasedProductAlert } from "../components/weight";

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
  const [shippingCost, setShippingCost] = useState(30);
  const [showLoginModal, setShowLoginModal] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [showCardForm, setShowCardForm] = useState(false);
  const [pendingOrderId, setPendingOrderId] = useState(null);
  const [pricing, setPricing] = useState(null);
  const [pricingLoading, setPricingLoading] = useState(false);
  const [pricingError, setPricingError] = useState("");
  const [appliedCampaigns, setAppliedCampaigns] = useState([]);
  const [campaignDiscountTotal, setCampaignDiscountTotal] = useState(0);
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
  const navigate = useNavigate();
  const { user } = useAuth();
  const { cartItems, getCartTotal, clearCart } = useCart();

  useEffect(() => {
    // Kargo ücretini hesapla
    setShippingCost(shippingMethod === "motorcycle" ? 20 : 30);
  }, [shippingMethod]);

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

        // Başarı mesajı
        alert(
          `✅ Siparişiniz alındı!\n\nSipariş No: ${res.orderNumber || res.orderId}\nToplam: ₺${res.finalPrice?.toFixed(2) || payload.totalPrice.toFixed(2)}`,
        );

        // Siparişler sayfasına yönlendir
        if (user) {
          navigate("/orders");
        } else {
          navigate("/"); // Misafir kullanıcı ana sayfaya
        }
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
      <h1 className="text-2xl font-bold mb-4">Ödeme ve Adres</h1>
      <form onSubmit={submit} className="max-w-xl">
        <input
          required
          placeholder="Ad Soyad"
          value={form.name}
          onChange={(e) => setForm({ ...form, name: e.target.value })}
          className="w-full mb-2 border p-2"
        />
        <input
          required
          placeholder="Telefon"
          value={form.phone}
          onChange={(e) => setForm({ ...form, phone: e.target.value })}
          className="w-full mb-2 border p-2"
        />
        <input
          required
          placeholder="E-posta"
          value={form.email}
          onChange={(e) => setForm({ ...form, email: e.target.value })}
          className="w-full mb-2 border p-2"
        />
        <input
          required
          placeholder="İl"
          value={form.city || ""}
          onChange={(e) => setForm({ ...form, city: e.target.value })}
          className="w-full mb-2 border p-2"
        />
        <textarea
          required
          placeholder="Adres"
          value={form.address}
          onChange={(e) => setForm({ ...form, address: e.target.value })}
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
                <div className="text-muted small">30 ₺</div>
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
                <div className="text-muted small">20 ₺</div>
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

        <button
          type="submit"
          className="bg-green-600 text-white p-3 rounded w-full fw-bold"
          style={{
            background: submitting
              ? "#999"
              : "linear-gradient(135deg, #16a34a, #22c55e)",
            border: "none",
            cursor: submitting ? "not-allowed" : "pointer",
            fontSize: "1.1rem",
          }}
          disabled={submitting || cartItems.length === 0}
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
