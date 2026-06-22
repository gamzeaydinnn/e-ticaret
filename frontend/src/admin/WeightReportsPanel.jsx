import React from "react";
import StoreAttendantDashboard from "../pages/StoreAttendant/StoreAttendantDashboard";

// Legacy admin sekmesindeki ağırlık ekranı artık demo veri kullanmamalı.
// Gerçek sipariş bazlı tartı akışıyla aynı bileşeni gösteriyoruz.
export default function WeightReportsPanel() {
  return <StoreAttendantDashboard mode="admin" weightOnly />;
}
