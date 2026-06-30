// ==========================================================================
// CourierPendingWeight.jsx - Bekleyen Tartı Girişleri Listesi
// ==========================================================================
// Dashboard'daki "Tartı Girişi" kısayolu /courier/weight-entry rotasına gider.
// Backend: GET /api/weight-adjustment/courier/pending
// ==========================================================================

import React, { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useCourierAuth } from "../../contexts/CourierAuthContext";
import { CourierService } from "../../services/courierService";
import { WeightAdjustmentService } from "../../services/weightAdjustmentService";
import "./CourierOrders.css";

const buildFallbackPendingOrders = (orders = []) =>
  orders
    .filter((order) => {
      const status = (order.status || "").toLowerCase();
      const isClosed = ["delivered", "cancelled", "refunded"].includes(status);
      const hasWeightItems =
        order.hasWeightBasedItems ||
        order.HasWeightBasedItems ||
        order.hasWeightDifference;
      const allWeighed =
        order.allItemsWeighed === true || order.AllItemsWeighed === true;
      return hasWeightItems && !allWeighed && !isClosed;
    })
    .map((order) => ({
      orderId: order.id || order.orderId,
      orderNumber: order.orderNumber || `#${order.id}`,
      customerName: order.customerName || "Müşteri",
      customerAddress: order.address || order.deliveryAddress || "",
      customerPhone: order.customerPhone || "",
      orderDate: order.orderTime || order.orderDate,
      pendingItemCount: 1,
      weighedItemCount: order.allItemsWeighed ? 1 : 0,
      items: [],
    }));

export default function CourierPendingWeight() {
  const { courier, isAuthenticated, loading: authLoading } = useCourierAuth();
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [usingFallback, setUsingFallback] = useState(false);

  const loadPending = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      setUsingFallback(false);

      const list = await WeightAdjustmentService.getCourierPendingOrders();
      if (Array.isArray(list) && list.length > 0) {
        setOrders(list);
        return;
      }

      // API boş dönerse kurye siparişlerinden yedek liste oluştur
      const { orders: assignedOrders = [] } =
        await CourierService.getAssignedOrders();
      const fallback = buildFallbackPendingOrders(assignedOrders);
      setOrders(fallback);
      if (fallback.length > 0) {
        setUsingFallback(true);
      }
    } catch (err) {
      if (err?.isCourierAuthError) {
        setError("Oturum süresi doldu. Lütfen tekrar giriş yapın.");
        return;
      }

      console.error("[CourierPendingWeight] load error:", err);

      try {
        const { orders: assignedOrders = [] } =
          await CourierService.getAssignedOrders();
        const fallback = buildFallbackPendingOrders(assignedOrders);
        if (fallback.length > 0) {
          setOrders(fallback);
          setUsingFallback(true);
          setError(null);
          return;
        }
      } catch (fallbackErr) {
        console.error("[CourierPendingWeight] fallback error:", fallbackErr);
      }

      setError(
        err?.message ||
          "Bekleyen tartı listesi yüklenemedi. Bağlantınızı kontrol edip tekrar deneyin.",
      );
      setOrders([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (authLoading || !isAuthenticated || !courier?.id) return;
    loadPending();
  }, [authLoading, isAuthenticated, courier?.id, loadPending]);

  const formatDate = (value) => {
    if (!value) return "-";
    try {
      return new Date(value).toLocaleString("tr-TR", {
        day: "2-digit",
        month: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
      });
    } catch {
      return "-";
    }
  };

  return (
    <div className="min-vh-100 bg-light">
      <nav
        className="navbar navbar-dark sticky-top"
        style={{ background: "linear-gradient(135deg, #ff6b35, #ff8c00)" }}
      >
        <div className="container-fluid px-3">
          <span className="navbar-brand d-flex align-items-center">
            <i className="fas fa-balance-scale me-2"></i>
            Tartı Girişi
          </span>
          <div className="d-flex gap-2">
            <button
              className="btn btn-sm btn-outline-light"
              onClick={loadPending}
              disabled={loading}
              title="Yenile"
            >
              <i className={`fas fa-sync-alt ${loading ? "fa-spin" : ""}`}></i>
            </button>
            <Link to="/courier/dashboard" className="btn btn-sm btn-light">
              <i className="fas fa-arrow-left me-1"></i>
              Panel
            </Link>
          </div>
        </div>
      </nav>

      <div className="container-fluid p-3 p-md-4">
        <div className="mb-3">
          <h5 className="fw-bold mb-1">Tartılması Gereken Siparişler</h5>
          <p className="text-muted small mb-0">
            Ağırlık bazlı ürün içeren ve henüz tartılmamış siparişleriniz
          </p>
        </div>

        {loading && (
          <div className="text-center py-5">
            <div className="spinner-border text-warning" role="status"></div>
            <p className="text-muted mt-2 small">Yükleniyor...</p>
          </div>
        )}

        {!loading && error && (
          <div className="alert alert-danger">
            <i className="fas fa-exclamation-circle me-2"></i>
            {error}
            <button
              className="btn btn-sm btn-outline-danger ms-2"
              onClick={loadPending}
            >
              Tekrar dene
            </button>
          </div>
        )}

        {!loading && !error && usingFallback && orders.length > 0 && (
          <div className="courier-alert courier-alert-warning mb-3">
            <i className="fas fa-info-circle"></i>
            <span>
              Tartı listesi siparişlerinizden oluşturuldu. Detaylı tartım için
              siparişe girin.
            </span>
          </div>
        )}

        {!loading && !error && orders.length === 0 && (
          <div className="card border-0 shadow-sm text-center py-5">
            <div className="card-body">
              <i
                className="fas fa-check-circle text-success mb-3"
                style={{ fontSize: "2.5rem" }}
              ></i>
              <h6 className="fw-bold">Bekleyen tartı yok</h6>
              <p className="text-muted small mb-3">
                Şu an tartılması gereken sipariş bulunmuyor.
              </p>
              <Link to="/courier/orders" className="btn btn-outline-primary btn-sm">
                Siparişlere git
              </Link>
            </div>
          </div>
        )}

        {!loading && !error && orders.length > 0 && (
          <div className="d-flex flex-column gap-3">
            {orders.map((order) => {
              const orderId = order.orderId || order.OrderId;
              const orderNumber =
                order.orderNumber || order.OrderNumber || `#${orderId}`;
              const pendingCount =
                order.pendingItemCount ?? order.PendingItemCount ?? 0;
              const weighedCount =
                order.weighedItemCount ?? order.WeighedItemCount ?? 0;
              const customerName =
                order.customerName || order.CustomerName || "Müşteri";
              const address =
                order.customerAddress || order.CustomerAddress || "";

              return (
                <div key={orderId} className="card border-0 shadow-sm">
                  <div className="card-body">
                    <div className="d-flex justify-content-between align-items-start gap-2 flex-wrap">
                      <div>
                        <div className="d-flex align-items-center gap-2 mb-1">
                          <span className="fw-bold">{orderNumber}</span>
                          <span className="badge bg-warning text-dark">
                            {pendingCount} ürün bekliyor
                          </span>
                          {weighedCount > 0 && (
                            <span className="badge bg-success">
                              {weighedCount} tartıldı
                            </span>
                          )}
                        </div>
                        <div className="text-muted small">
                          <i className="fas fa-user me-1"></i>
                          {customerName}
                        </div>
                        {address && (
                          <div className="text-muted small text-truncate">
                            <i className="fas fa-map-marker-alt me-1"></i>
                            {address}
                          </div>
                        )}
                        <div className="text-muted small">
                          <i className="fas fa-clock me-1"></i>
                          {formatDate(order.orderDate || order.OrderDate)}
                        </div>
                      </div>
                      <Link
                        to={`/courier/orders/${orderId}/weight`}
                        className="btn btn-warning fw-semibold"
                        style={{ borderRadius: "10px", whiteSpace: "nowrap" }}
                      >
                        <i className="fas fa-balance-scale me-1"></i>
                        Tartı Gir
                      </Link>
                    </div>

                    {Array.isArray(order.items || order.Items) &&
                      (order.items || order.Items).length > 0 && (
                        <ul className="list-unstyled mt-3 mb-0 small border-top pt-2">
                          {(order.items || order.Items).map((item) => (
                            <li
                              key={item.orderItemId || item.OrderItemId}
                              className="d-flex justify-content-between py-1"
                            >
                              <span>
                                {item.productName || item.ProductName}
                              </span>
                              <span className="text-muted">
                                ~{item.estimatedWeight ?? item.EstimatedWeight}{" "}
                                {item.weightUnit || item.WeightUnit || "kg"}
                              </span>
                            </li>
                          ))}
                        </ul>
                      )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
