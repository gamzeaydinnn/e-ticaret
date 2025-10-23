import React, { useState } from "react";

// Small, dependency-free order tracking panel.
// Props:
// - initialOrderId (string) optional: prefilled order id
// Usage:
// <OrderTrackingPanel initialOrderId="12345" />

export default function OrderTrackingPanel({ initialOrderId = "" }) {
  const [orderId, setOrderId] = useState(initialOrderId);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [order, setOrder] = useState(null);

  async function fetchOrder(id) {
    setLoading(true);
    setError(null);
    setOrder(null);
    try {
      const res = await fetch(`/api/orders/${encodeURIComponent(id)}`, {
        credentials: "include",
      });
      if (!res.ok) {
        const text = await res.text();
        throw new Error(`${res.status} ${res.statusText} - ${text}`);
      }
      const json = await res.json();
      setOrder(json);
    } catch (err) {
      setError(err.message || "Beklenmeyen hata");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div
      style={{
        border: "1px solid #e1e1e1",
        padding: 12,
        borderRadius: 6,
        maxWidth: 480,
      }}
    >
      <h4 style={{ marginTop: 0 }}>Sipariş Takip</h4>
      <div style={{ display: "flex", gap: 8, marginBottom: 8 }}>
        <input
          value={orderId}
          onChange={(e) => setOrderId(e.target.value)}
          placeholder="Sipariş ID girin"
          style={{ flex: 1, padding: 8 }}
        />
        <button
          onClick={() => fetchOrder(orderId)}
          disabled={!orderId || loading}
          style={{ padding: "8px 12px" }}
        >
          {loading ? "Yükleniyor..." : "Getir"}
        </button>
      </div>

      {error && (
        <div style={{ color: "crimson", marginBottom: 8 }}>Hata: {error}</div>
      )}

      {order ? (
        <div>
          <div>
            <strong>Sipariş No:</strong> {order.orderNumber ?? order.id}
          </div>
          <div>
            <strong>Durum:</strong> {order.status}
          </div>
          {order.trackingNumber && (
            <div>
              <strong>Kargo No:</strong> {order.trackingNumber}
            </div>
          )}
          {order.deliveredAt && (
            <div>
              <strong>Teslim Tarihi:</strong>{" "}
              {new Date(order.deliveredAt).toLocaleString()}
            </div>
          )}
        </div>
      ) : (
        <div style={{ color: "#666" }}>Sipariş getirilmeyi bekliyor.</div>
      )}

      <div style={{ marginTop: 10, fontSize: 13, color: "#666" }}>
        Not: Bu panel sadece okunur. Test amaçlı e-posta tetiklemek için backend
        admin endpointlerini kullanın.
      </div>
    </div>
  );
}
