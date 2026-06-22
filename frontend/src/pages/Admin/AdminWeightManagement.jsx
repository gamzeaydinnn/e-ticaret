import React from "react";
import StoreAttendantDashboard from "../StoreAttendant/StoreAttendantDashboard";

// NEDEN: Admin ve market görevlisi aynı sipariş hazırlama/tartı ekranını
// kullanmalı. Ayrı admin ekranı mock veriye düşüyordu ve iş akışı ayrışıyordu.
export default function AdminWeightManagement() {
  return <StoreAttendantDashboard mode="admin" weightOnly />;
}
