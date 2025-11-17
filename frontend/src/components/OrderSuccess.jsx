import React, { useEffect, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { OrderService } from "../services/orderService";

const OrderSuccess = () => {
  const location = useLocation();
  const params = new URLSearchParams(location.search);
  const [orderNumber, setOrderNumber] = useState(params.get("orderNumber"));
  const [orderSummary, setOrderSummary] = useState(null);
  const [loadingOrder, setLoadingOrder] = useState(Boolean(params.get("orderId")));
  const [loadError, setLoadError] = useState("");
  const orderId = params.get("orderId");

  useEffect(() => {
    let mounted = true;
    if (!orderId) {
      setLoadingOrder(false);
      return undefined;
    }

    (async () => {
      try {
        setLoadingOrder(true);
        const order = await OrderService.getById(orderId);
        if (!mounted) return;
        if (order?.orderNumber) setOrderNumber(order.orderNumber);
        setOrderSummary(order);
        setLoadError("");
      } catch {
        if (!mounted) return;
        setLoadError("Sipariş özeti yüklenemedi.");
      } finally {
        if (mounted) setLoadingOrder(false);
      }
    })();

    return () => {
      mounted = false;
    };
  }, [orderId]);

  const toCurrency = (value) => `₺${Number(value || 0).toFixed(2)}`;
  const summaryData = orderSummary
    ? {
        subtotal:
          Number(orderSummary.totalPrice || 0) -
          Number(orderSummary.shippingCost || 0),
        shipping: Number(orderSummary.shippingCost || 0),
        coupon: Number(orderSummary.couponDiscountAmount || 0),
        campaign: Number(orderSummary.campaignDiscountAmount || 0),
        discount: Number(
          orderSummary.discountAmount ??
            Number(orderSummary.couponDiscountAmount || 0) +
              Number(orderSummary.campaignDiscountAmount || 0)
        ),
        finalTotal: Number(
          orderSummary.finalPrice || orderSummary.totalPrice || 0
        ),
      }
    : null;

  return (
    <div
      style={{
        minHeight: "100vh",
        background:
          "linear-gradient(135deg, #e8f5e9 0%, #d0f0d6 50%, #b2dfdb 100%)",
        paddingTop: "2rem",
        paddingBottom: "2rem",
      }}
    >
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-lg-8">
            <div className="card shadow-lg border-0" style={{ borderRadius: 20 }}>
              <div
                className="card-body text-center p-5"
                style={{ backgroundColor: "#ffffff", borderRadius: 20 }}
              >
                <div
                  className="mx-auto d-flex align-items-center justify-content-center mb-4"
                  style={{
                    width: 80,
                    height: 80,
                    borderRadius: "50%",
                    background:
                      "linear-gradient(135deg, #28a745, #20c997, #17a2b8)",
                    boxShadow: "0 10px 30px rgba(40,167,69,0.25)",
                    color: "white",
                  }}
                >
                  <i className="fas fa-check fa-2x"></i>
                </div>

                <h2 className="fw-bold mb-2" style={{ color: "#28a745" }}>
                  Siparişiniz Alındı!
                </h2>
                <p className="text-muted mb-4">
                  Teşekkür ederiz. Siparişiniz başarıyla oluşturuldu.
                </p>

                {orderNumber && (
                  <div className="mb-4">
                    <div className="text-muted">Sipariş Numaranız</div>
                    <div
                      className="fw-bold"
                      style={{ fontSize: "1.25rem", color: "#ff6f00" }}
                    >
                      {orderNumber}
                    </div>
                  </div>
                )}

                <div className="row justify-content-center mt-4">
                  <div className="col-md-8">
                    <div
                      className="d-flex flex-column flex-sm-row gap-3 justify-content-center"
                    >
                      <Link
                        to="/orders"
                        className="btn btn-success btn-lg fw-bold border-0"
                        style={{ borderRadius: 12 }}
                      >
                        <i className="fas fa-truck me-2"></i>
                        Siparişlerim’e Git
                      </Link>
                      <Link
                        to="/"
                        className="btn btn-outline-success btn-lg fw-bold"
                        style={{ borderRadius: 12 }}
                      >
                        <i className="fas fa-home me-2"></i>
                        Alışverişe Devam Et
                      </Link>
                    </div>
                  </div>
                </div>

                <div className="text-muted mt-4" style={{ fontSize: "0.9rem" }}>
                  Siparişinizle ilgili bilgilendirmeleri e‑posta veya telefon
                  yoluyla ileteceğiz.
                </div>

                {loadingOrder && (
                  <div className="mt-4 text-muted">
                    <i className="fas fa-spinner fa-spin me-2"></i>
                    Sipariş özeti yükleniyor...
                  </div>
                )}

                {loadError && (
                  <div className="alert alert-warning mt-4 mb-0">
                    {loadError}
                  </div>
                )}

                {summaryData && (
                  <div className="card shadow-sm border-0 mt-4">
                    <div className="card-body text-start">
                      <h5 className="fw-bold text-success mb-3">
                        <i className="fas fa-receipt me-2"></i>Ödeme Özeti
                      </h5>
                      <div className="d-flex justify-content-between mb-1">
                        <span>Ara Toplam</span>
                        <strong>{toCurrency(summaryData.subtotal)}</strong>
                      </div>
                      <div className="d-flex justify-content-between mb-1">
                        <span>Kargo</span>
                        <strong>{toCurrency(summaryData.shipping)}</strong>
                      </div>
                      <div className="d-flex justify-content-between text-success mb-1">
                        <span>Kampanya İndirimi</span>
                        <strong>-{toCurrency(summaryData.campaign)}</strong>
                      </div>
                      <div className="d-flex justify-content-between text-success mb-1">
                        <span>Kupon İndirimi</span>
                        <strong>-{toCurrency(summaryData.coupon)}</strong>
                      </div>
                      <div className="d-flex justify-content-between text-success mb-2">
                        <span>Toplam İndirim</span>
                        <strong>-{toCurrency(summaryData.discount)}</strong>
                      </div>
                      <hr />
                      <div className="d-flex justify-content-between fw-bold">
                        <span>Ödenecek Tutar</span>
                        <span className="text-warning">
                          {toCurrency(summaryData.finalTotal)}
                        </span>
                      </div>
                      {orderSummary?.couponCode && (
                        <div className="small text-muted mt-2">
                          Uygulanan Kupon:{" "}
                          <strong>{orderSummary.couponCode}</strong>
                        </div>
                      )}
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default OrderSuccess;
