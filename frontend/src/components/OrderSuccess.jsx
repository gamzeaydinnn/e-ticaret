import React, { useEffect, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { OrderService } from "../services/orderService";

const OrderSuccess = () => {
  const location = useLocation();
  const params = new URLSearchParams(location.search);
  const [orderNumber, setOrderNumber] = useState(params.get("orderNumber"));
  const orderId = params.get("orderId");

  useEffect(() => {
    (async () => {
      try {
        if (!orderNumber && orderId) {
          const order = await OrderService.getById(orderId);
          if (order?.orderNumber) setOrderNumber(order.orderNumber);
        }
      } catch {
        // sessiz geç
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default OrderSuccess;
