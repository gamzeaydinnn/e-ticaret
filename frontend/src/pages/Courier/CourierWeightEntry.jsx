// ==========================================================================
// CourierWeightEntry.jsx — KAPALI (Faz E)
// Tartı Preparing'de market görevlisi tarafından girilir.
// Kurye teslimatı CapturePaymentAsync ile Capt yapar; tartı formu yoktur.
// ==========================================================================

import { Navigate } from "react-router-dom";

export default function CourierWeightEntry() {
  return <Navigate to="/courier/dashboard" replace />;
}
