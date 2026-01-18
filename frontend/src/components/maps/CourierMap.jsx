/**
 * CourierMap.jsx - Canlı Kurye Konum Haritası
 *
 * Bu component aktif kuryeleri ve teslimat noktalarını haritada gösterir.
 *
 * Özellikler:
 * - Aktif kuryeleri marker ile gösterme
 * - Real-time konum güncellemesi (SignalR)
 * - Teslimat bölgelerini polygon ile gösterme
 * - Kurye bilgisi popup
 * - Harita kontrolleri (zoom, fullscreen)
 *
 * NEDEN Leaflet: Ücretsiz, hafif, açık kaynak, React ile kolay entegrasyon
 */

import React, { useState, useEffect, useCallback, useRef } from "react";
import {
  MapContainer,
  TileLayer,
  Marker,
  Popup,
  Circle,
  useMap,
} from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import CourierLocationService from "../../services/courierLocationService";
import signalRService, { SignalREvents } from "../../services/signalRService";

// ============================================================================
// KURYE İKONU OLUŞTUR
// ============================================================================
const courierIcon = (isOnline) => {
  const color = isOnline ? "#10b981" : "#6b7280"; // Yeşil: online, Gri: offline

  return L.divIcon({
    className: "custom-courier-marker",
    html: `
      <div style="
        background: ${color};
        width: 32px;
        height: 32px;
        border-radius: 50%;
        border: 3px solid white;
        box-shadow: 0 2px 8px rgba(0,0,0,0.3);
        display: flex;
        align-items: center;
        justify-content: center;
        position: relative;
      ">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="white">
          <path d="M12 4C14.76 4 17 6.24 17 9C17 11.76 14.76 14 12 14C9.24 14 7 11.76 7 9C7 6.24 9.24 4 12 4M12 2C8.13 2 5 5.13 5 9C5 12.87 8.13 16 12 16C15.87 16 19 12.87 19 9C19 5.13 15.87 2 12 2M12 18C7.58 18 4 19.79 4 22H20C20 19.79 16.42 18 12 18Z"/>
        </svg>
      </div>
    `,
    iconSize: [32, 32],
    iconAnchor: [16, 16],
    popupAnchor: [0, -16],
  });
};

// TESLİMAT NOKTASI İKONU
const deliveryIcon = L.divIcon({
  className: "custom-delivery-marker",
  html: `
    <div style="
      background: #ef4444;
      width: 28px;
      height: 28px;
      border-radius: 50%;
      border: 3px solid white;
      box-shadow: 0 2px 8px rgba(0,0,0,0.3);
      display: flex;
      align-items: center;
      justify-content: center;
    ">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="white">
        <path d="M12 2L2 7L12 12L22 7L12 2M2 17L12 22L22 17V11L12 16L2 11V17Z"/>
      </svg>
    </div>
  `,
  iconSize: [28, 28],
  iconAnchor: [14, 14],
  popupAnchor: [0, -14],
});

// ============================================================================
// HARİTA KONTROLÜ - Merkezi Güncelleme
// ============================================================================
function MapController({ center, zoom }) {
  const map = useMap();

  useEffect(() => {
    if (center) {
      map.setView(center, zoom || map.getZoom());
    }
  }, [center, zoom, map]);

  return null;
}

// ============================================================================
// ANA KOMPONENTö
// ============================================================================
export default function CourierMap({
  showDeliveryPoints = false,
  deliveryPoints = [],
  height = "500px",
  initialCenter = [41.0082, 28.9784], // İstanbul
  initialZoom = 12,
}) {
  const [couriers, setCouriers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [mapCenter, setMapCenter] = useState(initialCenter);
  const [mapZoom, setMapZoom] = useState(initialZoom);
  const [selectedCourier, setSelectedCourier] = useState(null);

  // ============================================================================
  // KURYE KONUMLARıNı YüKLE
  // ============================================================================
  const loadCourierLocations = useCallback(async () => {
    try {
      setLoading(true);
      const locations =
        await CourierLocationService.getActiveCourierLocations();
      setCouriers(locations);
    } catch (error) {
      console.error("Kurye konumları yüklenemedi:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadCourierLocations();
    // Her 30 saniyede konumları yenile
    const interval = setInterval(loadCourierLocations, 30000);
    return () => clearInterval(interval);
  }, [loadCourierLocations]);

  // ============================================================================
  // SIGNALR - REAL-TIME KONUM GüNCELLEMESİ
  // ============================================================================
  useEffect(() => {
    const unsubscribe = signalRService.onDeliveryEvent(
      SignalREvents.COURIER_LOCATION_UPDATED,
      (data) => {
        setCouriers((prev) => {
          const index = prev.findIndex((c) => c.courierId === data.courierId);
          if (index >= 0) {
            // Mevcut kurye konumunu güncelle
            const updated = [...prev];
            updated[index] = {
              ...updated[index],
              latitude: data.latitude,
              longitude: data.longitude,
              lastUpdatedAt: new Date().toISOString(),
              speed: data.speedKmh,
              heading: data.heading,
            };
            return updated;
          } else {
            // Yeni kurye ekle
            return [
              ...prev,
              {
                courierId: data.courierId,
                courierName: data.courierName,
                latitude: data.latitude,
                longitude: data.longitude,
                isOnline: true,
                lastUpdatedAt: new Date().toISOString(),
              },
            ];
          }
        });
      },
    );

    return () => unsubscribe && unsubscribe();
  }, []);

  // ============================================================================
  // KURYE SEÇİMİ - HARİTAYı MERKEZ AL
  // ============================================================================
  const handleCourierSelect = (courier) => {
    setSelectedCourier(courier);
    setMapCenter([courier.latitude, courier.longitude]);
    setMapZoom(15);
  };

  // ============================================================================
  // RENDER
  // ============================================================================
  return (
    <div style={{ position: "relative", height, width: "100%" }}>
      {/* YüKLENİYOR GöSTERGESİ */}
      {loading && (
        <div
          style={{
            position: "absolute",
            top: "10px",
            left: "50%",
            transform: "translateX(-50%)",
            zIndex: 1000,
            background: "white",
            padding: "8px 16px",
            borderRadius: "8px",
            boxShadow: "0 2px 8px rgba(0,0,0,0.2)",
          }}
        >
          <div className="d-flex align-items-center gap-2">
            <div className="spinner-border spinner-border-sm text-primary"></div>
            <span style={{ fontSize: "0.85rem" }}>Konumlar yükleniyor...</span>
          </div>
        </div>
      )}

      {/* YENİLE BUTONU */}
      <button
        onClick={loadCourierLocations}
        style={{
          position: "absolute",
          top: "10px",
          right: "10px",
          zIndex: 1000,
          background: "white",
          border: "none",
          borderRadius: "8px",
          padding: "8px 12px",
          boxShadow: "0 2px 8px rgba(0,0,0,0.2)",
          cursor: "pointer",
        }}
        title="Konumları yenile"
      >
        <i className="fas fa-sync-alt"></i>
      </button>

      {/* HARİTA KONTEYNERİ */}
      <MapContainer
        center={initialCenter}
        zoom={initialZoom}
        style={{ height: "100%", width: "100%", borderRadius: "8px" }}
        scrollWheelZoom={true}
      >
        {/* HARİTA KATMANI - OpenStreetMap */}
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />

        {/* HARİTA KONTROLÜ */}
        <MapController center={mapCenter} zoom={mapZoom} />

        {/* KURYE MARKERLARı */}
        {couriers.map((courier) => (
          <Marker
            key={courier.courierId}
            position={[courier.latitude, courier.longitude]}
            icon={courierIcon(courier.isOnline)}
            eventHandlers={{
              click: () => handleCourierSelect(courier),
            }}
          >
            <Popup>
              <div style={{ minWidth: "200px" }}>
                <div className="d-flex align-items-center mb-2">
                  <div
                    className="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center me-2"
                    style={{ width: "32px", height: "32px" }}
                  >
                    <i className="fas fa-user"></i>
                  </div>
                  <div>
                    <div className="fw-bold" style={{ fontSize: "0.9rem" }}>
                      {courier.courierName || `Kurye #${courier.courierId}`}
                    </div>
                    <span
                      className={`badge ${courier.isOnline ? "bg-success" : "bg-secondary"}`}
                      style={{ fontSize: "0.65rem" }}
                    >
                      {courier.isOnline ? "Çevrimiçi" : "Çevrimdışı"}
                    </span>
                  </div>
                </div>

                <div style={{ fontSize: "0.75rem" }}>
                  <div className="mb-1">
                    <i className="fas fa-map-marker-alt me-2 text-muted"></i>
                    <strong>Konum:</strong>
                    <div className="ms-4 text-muted">
                      {courier.latitude?.toFixed(6)},{" "}
                      {courier.longitude?.toFixed(6)}
                    </div>
                  </div>

                  {courier.speed && (
                    <div className="mb-1">
                      <i className="fas fa-tachometer-alt me-2 text-muted"></i>
                      <strong>Hız:</strong> {courier.speed.toFixed(1)} km/s
                    </div>
                  )}

                  {courier.lastUpdatedAt && (
                    <div className="text-muted" style={{ fontSize: "0.7rem" }}>
                      <i className="fas fa-clock me-1"></i>
                      {new Date(courier.lastUpdatedAt).toLocaleTimeString(
                        "tr-TR",
                      )}
                    </div>
                  )}
                </div>

                {courier.activeDeliveries > 0 && (
                  <div
                    className="mt-2 p-2 bg-light rounded"
                    style={{ fontSize: "0.75rem" }}
                  >
                    <i className="fas fa-box me-2 text-primary"></i>
                    <strong>{courier.activeDeliveries}</strong> aktif teslimat
                  </div>
                )}
              </div>
            </Popup>

            {/* KURYE ETRAFıNDA YARıÇAP (Aktif bölge göstergesi) */}
            {courier.isOnline && (
              <Circle
                center={[courier.latitude, courier.longitude]}
                radius={500} // 500 metre
                pathOptions={{
                  color: "#10b981",
                  fillColor: "#10b981",
                  fillOpacity: 0.1,
                  weight: 1,
                }}
              />
            )}
          </Marker>
        ))}

        {/* TESLİMAT NOKTALARı (Opsiyonel) */}
        {showDeliveryPoints &&
          deliveryPoints.map((point, index) => (
            <Marker
              key={`delivery-${index}`}
              position={[point.latitude, point.longitude]}
              icon={deliveryIcon}
            >
              <Popup>
                <div style={{ minWidth: "180px", fontSize: "0.8rem" }}>
                  <div className="fw-bold mb-2">
                    {point.address || "Teslimat Noktası"}
                  </div>
                  {point.customerName && (
                    <div className="mb-1">
                      <i className="fas fa-user me-2"></i>
                      {point.customerName}
                    </div>
                  )}
                  {point.phone && (
                    <div className="mb-1">
                      <i className="fas fa-phone me-2"></i>
                      {point.phone}
                    </div>
                  )}
                  {point.orderId && (
                    <div className="text-muted">Sipariş #{point.orderId}</div>
                  )}
                </div>
              </Popup>
            </Marker>
          ))}
      </MapContainer>

      {/* KURYE LİSTESİ (Yan Panel - Opsiyonel) */}
      {couriers.length > 0 && (
        <div
          style={{
            position: "absolute",
            bottom: "10px",
            left: "10px",
            background: "white",
            borderRadius: "8px",
            padding: "10px",
            boxShadow: "0 2px 8px rgba(0,0,0,0.2)",
            maxHeight: "200px",
            overflowY: "auto",
            zIndex: 1000,
            minWidth: "220px",
          }}
        >
          <div className="fw-bold mb-2" style={{ fontSize: "0.85rem" }}>
            <i className="fas fa-motorcycle me-2 text-primary"></i>
            Aktif Kuryeler ({couriers.length})
          </div>
          {couriers.map((courier) => (
            <div
              key={courier.courierId}
              onClick={() => handleCourierSelect(courier)}
              style={{
                padding: "6px 8px",
                marginBottom: "4px",
                borderRadius: "4px",
                cursor: "pointer",
                background:
                  selectedCourier?.courierId === courier.courierId
                    ? "#e0f2fe"
                    : "transparent",
                fontSize: "0.75rem",
              }}
              className="hover-bg-light"
            >
              <div className="d-flex justify-content-between align-items-center">
                <span>
                  <span
                    className={`badge ${courier.isOnline ? "bg-success" : "bg-secondary"} me-2`}
                    style={{
                      width: "8px",
                      height: "8px",
                      padding: 0,
                      borderRadius: "50%",
                    }}
                  ></span>
                  {courier.courierName || `Kurye #${courier.courierId}`}
                </span>
                {courier.activeDeliveries > 0 && (
                  <span
                    className="badge bg-primary"
                    style={{ fontSize: "0.65rem" }}
                  >
                    {courier.activeDeliveries}
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
