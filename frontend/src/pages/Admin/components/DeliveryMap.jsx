/**
 * DeliveryMap.jsx - Teslimat Haritası Componenti
 * 
 * Bu component, teslimat görevlerini ve kurye konumlarını harita üzerinde gösterir.
 * Özellikler:
 * - Teslimat noktalarını gösterme
 * - Kurye konumlarını real-time gösterme
 * - Tıklama ile görev detayına gitme
 * - Kurye atama
 * 
 * NOT: Bu component Leaflet veya Google Maps ile çalışır.
 * Leaflet kurulumu için: npm install leaflet react-leaflet
 * 
 * NEDEN Leaflet: Açık kaynak, ücretsiz, OpenStreetMap ile çalışır
 */

import { useEffect, useState, useRef } from "react";
import { DeliveryStatus, DeliveryStatusColors } from "../../../services/deliveryTaskService";

// Leaflet CSS'i public/index.html'e eklenecek veya import edilecek:
// <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />

export default function DeliveryMap({ 
  tasks = [], 
  couriers = [],
  onTaskClick, 
  onAssignClick,
  center = [41.0082, 28.9784], // İstanbul varsayılan
  zoom = 12,
  height = "500px"
}) {
  // =========================================================================
  // STATE
  // =========================================================================
  const mapRef = useRef(null);
  const [mapReady, setMapReady] = useState(false);
  const [selectedTask, setSelectedTask] = useState(null);
  const [leafletLoaded, setLeafletLoaded] = useState(false);

  // =========================================================================
  // LEAFLET YÜKLEME
  // =========================================================================

  useEffect(() => {
    // Leaflet'in yüklü olup olmadığını kontrol et
    if (window.L) {
      setLeafletLoaded(true);
      return;
    }

    // Leaflet CSS'i yükle
    const cssLink = document.createElement("link");
    cssLink.rel = "stylesheet";
    cssLink.href = "https://unpkg.com/leaflet@1.9.4/dist/leaflet.css";
    cssLink.integrity = "sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY=";
    cssLink.crossOrigin = "";
    document.head.appendChild(cssLink);

    // Leaflet JS'i yükle
    const script = document.createElement("script");
    script.src = "https://unpkg.com/leaflet@1.9.4/dist/leaflet.js";
    script.integrity = "sha256-20nQCchB9co0qIjJZRGuk2/Z9VM+kNiyxNV1lvTlZBo=";
    script.crossOrigin = "";
    script.onload = () => setLeafletLoaded(true);
    document.body.appendChild(script);

    return () => {
      // Cleanup (optional)
    };
  }, []);

  // =========================================================================
  // HARİTA OLUŞTURMA
  // =========================================================================

  useEffect(() => {
    if (!leafletLoaded || !mapRef.current || mapReady) return;

    const L = window.L;
    
    // Haritayı oluştur
    const map = L.map(mapRef.current).setView(center, zoom);
    
    // OpenStreetMap tile layer ekle
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    // Haritayı ref'e kaydet
    mapRef.current._leafletMap = map;
    setMapReady(true);

    return () => {
      map.remove();
    };
  }, [leafletLoaded, center, zoom]);

  // =========================================================================
  // MARKER'LARI GÜNCELLEME
  // =========================================================================

  useEffect(() => {
    if (!mapReady || !mapRef.current?._leafletMap) return;

    const L = window.L;
    const map = mapRef.current._leafletMap;

    // Önceki marker'ları temizle
    map.eachLayer((layer) => {
      if (layer instanceof L.Marker) {
        map.removeLayer(layer);
      }
    });

    // Teslimat noktaları için marker'lar
    tasks.forEach((task) => {
      if (!task.dropoffLatitude || !task.dropoffLongitude) return;

      // Durum rengine göre ikon
      const statusColor = DeliveryStatusColors[task.status] || "secondary";
      const iconHtml = `
        <div style="
          background: var(--bs-${statusColor}, #6c757d);
          width: 30px;
          height: 30px;
          border-radius: 50% 50% 50% 0;
          transform: rotate(-45deg);
          display: flex;
          align-items: center;
          justify-content: center;
          border: 2px solid white;
          box-shadow: 0 2px 5px rgba(0,0,0,0.3);
        ">
          <i class="fas fa-box" style="
            transform: rotate(45deg);
            color: white;
            font-size: 12px;
          "></i>
        </div>
      `;

      const icon = L.divIcon({
        html: iconHtml,
        className: "custom-marker",
        iconSize: [30, 40],
        iconAnchor: [15, 40],
        popupAnchor: [0, -40]
      });

      const marker = L.marker([task.dropoffLatitude, task.dropoffLongitude], { icon })
        .addTo(map);

      // Popup içeriği
      const popupContent = `
        <div style="min-width: 200px; font-size: 12px;">
          <strong>Görev #${task.id}</strong>
          <hr style="margin: 5px 0;" />
          <p style="margin: 0;"><strong>Müşteri:</strong> ${task.customerName}</p>
          <p style="margin: 0;"><strong>Adres:</strong> ${task.dropoffAddress}</p>
          ${task.courierName ? `<p style="margin: 0;"><strong>Kurye:</strong> ${task.courierName}</p>` : ""}
          <div style="margin-top: 10px;">
            <button 
              onclick="window.__mapTaskClick && window.__mapTaskClick(${task.id})"
              style="
                background: #0d6efd;
                color: white;
                border: none;
                padding: 5px 10px;
                border-radius: 4px;
                cursor: pointer;
                font-size: 11px;
                margin-right: 5px;
              "
            >
              <i class="fas fa-eye"></i> Detay
            </button>
            ${task.status === 0 ? `
              <button 
                onclick="window.__mapAssignClick && window.__mapAssignClick(${task.id})"
                style="
                  background: #198754;
                  color: white;
                  border: none;
                  padding: 5px 10px;
                  border-radius: 4px;
                  cursor: pointer;
                  font-size: 11px;
                "
              >
                <i class="fas fa-user-plus"></i> Kurye Ata
              </button>
            ` : ""}
          </div>
        </div>
      `;

      marker.bindPopup(popupContent);
    });

    // Kurye konumları için marker'lar
    const courierLocations = tasks
      .filter(t => t.courierLatitude && t.courierLongitude && t.courierId)
      .reduce((acc, task) => {
        // Aynı kuryeyi birden fazla ekleme
        if (!acc.find(c => c.courierId === task.courierId)) {
          acc.push({
            courierId: task.courierId,
            courierName: task.courierName,
            latitude: task.courierLatitude,
            longitude: task.courierLongitude
          });
        }
        return acc;
      }, []);

    courierLocations.forEach((courier) => {
      const iconHtml = `
        <div style="
          background: #10b981;
          width: 35px;
          height: 35px;
          border-radius: 50%;
          display: flex;
          align-items: center;
          justify-content: center;
          border: 3px solid white;
          box-shadow: 0 2px 5px rgba(0,0,0,0.3);
          animation: pulse 2s infinite;
        ">
          <i class="fas fa-motorcycle" style="color: white; font-size: 14px;"></i>
        </div>
      `;

      const icon = L.divIcon({
        html: iconHtml,
        className: "courier-marker",
        iconSize: [35, 35],
        iconAnchor: [17, 17]
      });

      L.marker([courier.latitude, courier.longitude], { icon })
        .addTo(map)
        .bindPopup(`<strong>${courier.courierName}</strong><br/>Kurye`);
    });

  }, [mapReady, tasks]);

  // =========================================================================
  // GLOBAL CALLBACK'LER (Popup içinden erişim için)
  // =========================================================================

  useEffect(() => {
    window.__mapTaskClick = (taskId) => {
      const task = tasks.find(t => t.id === taskId);
      if (task && onTaskClick) {
        onTaskClick(taskId);
      }
    };

    window.__mapAssignClick = (taskId) => {
      const task = tasks.find(t => t.id === taskId);
      if (task && onAssignClick) {
        onAssignClick(task);
      }
    };

    return () => {
      delete window.__mapTaskClick;
      delete window.__mapAssignClick;
    };
  }, [tasks, onTaskClick, onAssignClick]);

  // =========================================================================
  // RENDER
  // =========================================================================

  return (
    <div className="delivery-map-container">
      {/* Harita Container */}
      <div 
        ref={mapRef} 
        style={{ 
          height, 
          width: "100%", 
          borderRadius: "0 0 10px 10px",
          backgroundColor: "#e9ecef"
        }}
      >
        {!leafletLoaded && (
          <div className="d-flex justify-content-center align-items-center h-100">
            <div className="text-center">
              <div className="spinner-border text-success mb-2"></div>
              <p className="text-muted mb-0">Harita yükleniyor...</p>
            </div>
          </div>
        )}
      </div>

      {/* Harita Lejant */}
      <div 
        className="position-absolute bg-white rounded shadow-sm p-2"
        style={{ 
          bottom: "20px", 
          right: "20px", 
          fontSize: "0.7rem",
          zIndex: 1000
        }}
      >
        <div className="fw-bold mb-1">Lejant</div>
        <div className="d-flex align-items-center mb-1">
          <span className="badge bg-secondary me-2" style={{ width: "12px", height: "12px", padding: 0 }}></span>
          Bekliyor
        </div>
        <div className="d-flex align-items-center mb-1">
          <span className="badge bg-info me-2" style={{ width: "12px", height: "12px", padding: 0 }}></span>
          Atandı
        </div>
        <div className="d-flex align-items-center mb-1">
          <span className="badge bg-warning me-2" style={{ width: "12px", height: "12px", padding: 0 }}></span>
          Yolda
        </div>
        <div className="d-flex align-items-center mb-1">
          <span className="badge bg-success me-2" style={{ width: "12px", height: "12px", padding: 0 }}></span>
          Teslim Edildi
        </div>
        <div className="d-flex align-items-center">
          <i className="fas fa-motorcycle text-success me-2"></i>
          Kurye
        </div>
      </div>

      {/* Harita İstatistikleri */}
      <div 
        className="position-absolute bg-white rounded shadow-sm p-2"
        style={{ 
          top: "10px", 
          left: "10px", 
          fontSize: "0.75rem",
          zIndex: 1000
        }}
      >
        <span className="badge bg-primary me-2">{tasks.length} Görev</span>
        <span className="badge bg-success">
          {tasks.filter(t => t.courierLatitude).length} Kurye
        </span>
      </div>

      {/* CSS Animasyonları */}
      <style>{`
        @keyframes pulse {
          0% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.4); }
          70% { box-shadow: 0 0 0 10px rgba(16, 185, 129, 0); }
          100% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
        }
        .custom-marker, .courier-marker {
          background: transparent !important;
          border: none !important;
        }
      `}</style>
    </div>
  );
}
