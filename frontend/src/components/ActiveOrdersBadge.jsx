// ==========================================================================
// ActiveOrdersBadge.jsx - Header "Siparişlerim" aktif sipariş rozeti
// ==========================================================================
// Giriş yapmış müşterinin devam eden (teslim edilmemiş/iptal olmamış) sipariş
// sayısını header'daki Siparişlerim ikonu üzerinde küçük bir rozet olarak
// gösterir. Sipariş durumu SignalR ile değiştiğinde otomatik tazelenir.
//
// Tasarım kararı: Bu bileşen tamamen kendi kendine yeter ve HER hata durumunda
// sessizce gizlenir (badge göstermez). Böylece App.js'in ana akışını riske atmaz.
// ==========================================================================

import React, { useEffect, useState, useCallback, useRef } from "react";
import { useAuth } from "../contexts/AuthContext";
import { OrderService } from "../services/orderService";

// Aktif sayılmayan (tamamlanmış/sonlanmış) durumlar
const TERMINAL_STATUSES = new Set([
  "delivered",
  "cancelled",
  "canceled",
  "refunded",
  "delivery_failed",
  "deliveryfailed",
  "completed",
  "returned",
]);

const isActiveStatus = (status) => {
  if (!status) return false;
  const normalized = String(status).toLowerCase().replace(/\s|-/g, "_");
  return !TERMINAL_STATUSES.has(normalized);
};

export default function ActiveOrdersBadge() {
  const { user, isAuthenticated } = useAuth();
  const [activeCount, setActiveCount] = useState(0);
  const isMountedRef = useRef(true);

  const refresh = useCallback(async () => {
    if (!user?.id) {
      setActiveCount(0);
      return;
    }
    try {
      const orders = await OrderService.list(user.id);
      if (!isMountedRef.current) return;
      const count = Array.isArray(orders)
        ? orders.filter((o) => isActiveStatus(o?.status)).length
        : 0;
      setActiveCount(count);
    } catch (err) {
      // Sessizce gizle — header akışını bozma
      if (isMountedRef.current) setActiveCount(0);
    }
  }, [user?.id]);

  useEffect(() => {
    isMountedRef.current = true;
    if (isAuthenticated && user?.id) {
      refresh();
    } else {
      setActiveCount(0);
    }
    return () => {
      isMountedRef.current = false;
    };
  }, [isAuthenticated, user?.id, refresh]);

  // Sipariş ile ilgili global olaylarda tazele
  useEffect(() => {
    if (!isAuthenticated || !user?.id) return;

    const handleChange = () => refresh();
    // OrderTracking / checkout gibi akışların yaydığı olaylar
    window.addEventListener("orderStatusChanged", handleChange);
    window.addEventListener("orderPlaced", handleChange);

    // Hafif yedek: 60 sn'de bir tazele (SignalR kaçaklarına karşı)
    const interval = setInterval(refresh, 60000);

    return () => {
      window.removeEventListener("orderStatusChanged", handleChange);
      window.removeEventListener("orderPlaced", handleChange);
      clearInterval(interval);
    };
  }, [isAuthenticated, user?.id, refresh]);

  if (!isAuthenticated || activeCount <= 0) return null;

  return (
    <span
      className="position-absolute top-0 start-100 translate-middle badge rounded-pill"
      style={{
        background: "linear-gradient(135deg, #e74c3c, #ec7063)",
        fontSize: "0.6rem",
        minWidth: "16px",
        height: "16px",
        animation: "pulse 2s infinite",
      }}
      title={`${activeCount} devam eden sipariş`}
    >
      {activeCount > 9 ? "9+" : activeCount}
    </span>
  );
}
