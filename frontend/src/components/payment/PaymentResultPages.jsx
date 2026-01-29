// ═══════════════════════════════════════════════════════════════════════════════════════════════
// 3D SECURE CALLBACK SAYFALARI
// POSNET 3D Secure işlemleri için başarı ve başarısız callback sayfaları
// ═══════════════════════════════════════════════════════════════════════════════════════════════

import React, { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { PaymentService } from "../../services/paymentService";
import { OrderService } from "../../services/orderService";
import "./PaymentResult.css";

// ═══════════════════════════════════════════════════════════════════════════
// BAŞARILI ÖDEME SAYFASI
// ═══════════════════════════════════════════════════════════════════════════
export const PaymentSuccessPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [loading, setLoading] = useState(true);
  const [paymentDetails, setPaymentDetails] = useState(null);
  const [error, setError] = useState(null);
  const [orderSummary, setOrderSummary] = useState(null);
  const [orderSummaryError, setOrderSummaryError] = useState("");

  useEffect(() => {
    const verifyPayment = async () => {
      try {
        // URL parametrelerinden ödeme bilgilerini al
        const orderId = searchParams.get("orderId");
        const paymentId = searchParams.get("paymentId");
        const transactionId = searchParams.get("transactionId");

        if (paymentId) {
          // Ödeme durumunu doğrula
          const status = await PaymentService.checkPaymentStatus(paymentId);

          if (status.success) {
            setPaymentDetails({
              orderId: orderId || status.orderId,
              paymentId,
              transactionId: transactionId || status.transactionId,
              amount: status.amount,
              date: new Date().toLocaleString("tr-TR"),
            });
          } else {
            setError("Ödeme doğrulanamadı");
          }
        } else {
          // Sadece başarı mesajı göster
          setPaymentDetails({
            orderId,
            transactionId,
            date: new Date().toLocaleString("tr-TR"),
          });
        }
      } catch (err) {
        console.error("Ödeme doğrulama hatası:", err);
        setError("Ödeme durumu kontrol edilirken bir hata oluştu");
      } finally {
        setLoading(false);
      }
    };

    verifyPayment();
  }, [searchParams]);

  useEffect(() => {
    const orderId = searchParams.get("orderId");
    if (!orderId) return;

    let mounted = true;
    const fetchSummary = async () => {
      try {
        const order = await OrderService.getById(orderId);
        if (!mounted) return;
        setOrderSummary(order);
        setOrderSummaryError("");
      } catch {
        if (!mounted) return;
        setOrderSummaryError("Sipariş özeti yüklenemedi.");
      }
    };

    fetchSummary();
    return () => {
      mounted = false;
    };
  }, [searchParams]);

  if (loading) {
    return (
      <div className="payment-result-page">
        <div className="result-container loading">
          <div className="spinner-large"></div>
          <p>Ödeme doğrulanıyor...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="payment-result-page">
        <div className="result-container warning">
          <div className="result-icon">⚠️</div>
          <h1>Ödeme Durumu Belirsiz</h1>
          <p>{error}</p>
          <p className="info-text">
            Ödemeniz işlenmiş olabilir. Lütfen siparişlerinizi kontrol edin.
          </p>
          <div className="result-actions">
            <button onClick={() => navigate("/orders")} className="primary-btn">
              Siparişlerime Git
            </button>
            <button onClick={() => navigate("/")} className="secondary-btn">
              Ana Sayfa
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="payment-result-page">
      <div className="result-container success">
        <div className="result-icon success-icon">✓</div>
        <h1>Ödeme Başarılı!</h1>
        <p>Siparişiniz başarıyla oluşturuldu.</p>

        {paymentDetails && (
          <div className="payment-details">
            {paymentDetails.orderId && (
              <div className="detail-row">
                <span>Sipariş No:</span>
                <strong>#{paymentDetails.orderId}</strong>
              </div>
            )}
            {paymentDetails.transactionId && (
              <div className="detail-row">
                <span>İşlem No:</span>
                <strong>{paymentDetails.transactionId}</strong>
              </div>
            )}
            {paymentDetails.amount && (
              <div className="detail-row">
                <span>Ödenen Tutar:</span>
                <strong>{paymentDetails.amount.toFixed(2)} ₺</strong>
              </div>
            )}
            <div className="detail-row">
              <span>Tarih:</span>
              <strong>{paymentDetails.date}</strong>
            </div>
          </div>
        )}

        {orderSummaryError && (
          <div className="info-text">{orderSummaryError}</div>
        )}

        {orderSummary && (
          <div className="payment-details">
            {/* NEDEN: 3D Secure sonucundan sonra özet görünmeli. */}
            <div className="detail-row">
              <span>Ara Toplam:</span>
              <strong>{(Number(orderSummary.totalPrice || 0) - Number(orderSummary.shippingCost || 0)).toFixed(2)} ₺</strong>
            </div>
            <div className="detail-row">
              <span>Kargo:</span>
              <strong>{Number(orderSummary.shippingCost || 0).toFixed(2)} ₺</strong>
            </div>
            <div className="detail-row">
              <span>İndirim:</span>
              <strong>-{Number(orderSummary.discountAmount || 0).toFixed(2)} ₺</strong>
            </div>
            <div className="detail-row">
              <span>Toplam:</span>
              <strong>{Number(orderSummary.finalPrice || orderSummary.totalPrice || 0).toFixed(2)} ₺</strong>
            </div>
          </div>
        )}

        <div className="result-message">
          <p>📧 Sipariş onay e-postası gönderildi.</p>
          <p>📦 Siparişiniz en kısa sürede hazırlanacaktır.</p>
        </div>

        <div className="result-actions">
          <button onClick={() => navigate("/orders")} className="primary-btn">
            Siparişlerime Git
          </button>
          <button onClick={() => navigate("/")} className="secondary-btn">
            Alışverişe Devam Et
          </button>
        </div>
      </div>
    </div>
  );
};

// ═══════════════════════════════════════════════════════════════════════════
// BAŞARISIZ ÖDEME SAYFASI
// ═══════════════════════════════════════════════════════════════════════════
export const PaymentFailurePage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const errorCode = searchParams.get("errorCode");
  const errorMessage =
    searchParams.get("errorMessage") ||
    searchParams.get("message") ||
    "Ödeme işlemi tamamlanamadı";
  const orderId = searchParams.get("orderId");

  // Hata kodlarına göre kullanıcı dostu mesajlar
  const getErrorDescription = (code) => {
    const errorDescriptions = {
      "0001": "Kart bilgilerinizi kontrol edin ve tekrar deneyin.",
      "0002": "Kartınızın limiti yetersiz.",
      "0003": "Kart son kullanma tarihi geçmiş.",
      "0004": "CVV kodu hatalı.",
      "0005": "Bu kart ile online işlem yapılamaz.",
      "0006": "3D Secure doğrulaması başarısız.",
      "0007": "İşlem bankanız tarafından reddedildi.",
      "0012": "İşlem zaman aşımına uğradı.",
      "0099": "Teknik bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
      TIMEOUT: "Banka ile bağlantı zaman aşımına uğradı.",
      CANCEL: "İşlem iptal edildi.",
    };

    return (
      errorDescriptions[code] ||
      "Lütfen kart bilgilerinizi kontrol edip tekrar deneyin."
    );
  };

  return (
    <div className="payment-result-page">
      <div className="result-container failure">
        <div className="result-icon failure-icon">✗</div>
        <h1>Ödeme Başarısız</h1>
        <p className="error-message">{decodeURIComponent(errorMessage)}</p>

        {errorCode && (
          <p className="error-description">{getErrorDescription(errorCode)}</p>
        )}

        <div className="failure-tips">
          <h4>Olası Çözümler:</h4>
          <ul>
            <li>Kart numaranızı kontrol edin</li>
            <li>Son kullanma tarihini doğru girdiğinizden emin olun</li>
            <li>CVV kodunu kontrol edin (kartın arkasındaki 3 haneli kod)</li>
            <li>Kartınızda yeterli bakiye olduğundan emin olun</li>
            <li>Kartınızın online alışverişe açık olduğunu kontrol edin</li>
            <li>Farklı bir kart ile deneyebilirsiniz</li>
          </ul>
        </div>

        <div className="result-actions">
          <button
            onClick={() =>
              navigate(orderId ? `/checkout?orderId=${orderId}` : "/checkout")
            }
            className="primary-btn"
          >
            Tekrar Dene
          </button>
          <button onClick={() => navigate("/cart")} className="secondary-btn">
            Sepete Dön
          </button>
        </div>

        <div className="support-info">
          <p>Sorun devam ederse müşteri hizmetlerimize ulaşabilirsiniz.</p>
          <a href="/contact" className="support-link">
            📞 Destek Al
          </a>
        </div>
      </div>
    </div>
  );
};

// ═══════════════════════════════════════════════════════════════════════════
// 3D SECURE CALLBACK HANDLER
// Banka sayfasından dönen POST verilerini işler
// ═══════════════════════════════════════════════════════════════════════════
export const ThreeDSecureCallbackPage = () => {
  const navigate = useNavigate();
  const [processing, setProcessing] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const processCallback = async () => {
      try {
        // URL'den parametreleri al
        const urlParams = new URLSearchParams(window.location.search);
        const status = urlParams.get("status");
        const orderId = urlParams.get("orderId");
        const paymentId = urlParams.get("paymentId");
        const transactionId = urlParams.get("transactionId");
        const errorCode = urlParams.get("errorCode");
        const errorMessage = urlParams.get("errorMessage");

        // POST body'den gelen veriler (hidden form aracılığıyla)
        // Bu veriler backend tarafından işlenir ve redirect yapılır

        if (status === "success") {
          navigate(
            `/checkout/success?orderId=${orderId}&paymentId=${paymentId}&transactionId=${transactionId}`,
          );
        } else if (status === "fail") {
          navigate(
            `/checkout/fail?orderId=${orderId}&errorCode=${errorCode}&errorMessage=${encodeURIComponent(errorMessage || "")}`,
          );
        } else {
          // Beklenmedik durum
          setError("Ödeme durumu belirlenemedi");
          setProcessing(false);
        }
      } catch (err) {
        console.error("3D Secure callback işleme hatası:", err);
        setError("Ödeme işlemi sırasında bir hata oluştu");
        setProcessing(false);
      }
    };

    processCallback();
  }, [navigate]);

  if (processing) {
    return (
      <div className="payment-result-page">
        <div className="result-container loading">
          <div className="spinner-large"></div>
          <h2>Ödemeniz İşleniyor</h2>
          <p>Lütfen bekleyiniz, sayfayı kapatmayınız...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="payment-result-page">
        <div className="result-container warning">
          <div className="result-icon">⚠️</div>
          <h1>Bir Sorun Oluştu</h1>
          <p>{error}</p>
          <div className="result-actions">
            <button
              onClick={() => navigate("/checkout")}
              className="primary-btn"
            >
              Ödemeyi Tekrarla
            </button>
            <button onClick={() => navigate("/")} className="secondary-btn">
              Ana Sayfa
            </button>
          </div>
        </div>
      </div>
    );
  }

  return null;
};

export default {
  PaymentSuccessPage,
  PaymentFailurePage,
  ThreeDSecureCallbackPage,
};
