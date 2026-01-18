// ==========================================================================
// CourierLocationTracker.jsx - Kurye Konum Takip Bileşeni
// ==========================================================================
// Kuryenin gerçek zamanlı konumunu izler ve sunucuya gönderir.
// Geolocation API kullanır, arka plan takibi için Service Worker desteği.
// ==========================================================================

import React, { useState, useEffect, useCallback, useRef } from "react";
import { useCourierAuth } from "../../contexts/CourierAuthContext";
import { useCourierSignalR } from "../../contexts/CourierSignalRContext";

// Varsayılan ayarlar
const DEFAULT_CONFIG = {
  enableHighAccuracy: true,
  timeout: 10000,
  maximumAge: 5000,
  updateInterval: 30000, // 30 saniye
  minDistance: 50, // Minimum 50 metre hareket
};

export default function CourierLocationTracker({
  enabled = true,
  onLocationUpdate,
  showStatus = true,
  config = {},
}) {
  const { courier, isOnline } = useCourierAuth();
  const { sendLocationUpdate, isConnected } = useCourierSignalR();

  // State
  const [tracking, setTracking] = useState(false);
  const [currentLocation, setCurrentLocation] = useState(null);
  const [locationError, setLocationError] = useState(null);
  const [lastUpdateTime, setLastUpdateTime] = useState(null);
  const [accuracy, setAccuracy] = useState(null);
  const [permissionStatus, setPermissionStatus] = useState("prompt"); // prompt, granted, denied

  // Refs
  const watchIdRef = useRef(null);
  const lastPositionRef = useRef(null);
  const updateIntervalRef = useRef(null);

  // Konfigürasyon
  const settings = { ...DEFAULT_CONFIG, ...config };

  // =========================================================================
  // İZİN KONTROLÜ
  // =========================================================================
  useEffect(() => {
    checkPermission();
  }, []);

  const checkPermission = async () => {
    if (!("geolocation" in navigator)) {
      setPermissionStatus("denied");
      setLocationError("Cihazınız konum servislerini desteklemiyor");
      return;
    }

    try {
      const permission = await navigator.permissions?.query({
        name: "geolocation",
      });
      if (permission) {
        setPermissionStatus(permission.state);
        permission.addEventListener("change", () => {
          setPermissionStatus(permission.state);
        });
      }
    } catch (e) {
      // Safari gibi bazı tarayıcılar permissions API'yi desteklemez
      console.log("Permission API not supported");
    }
  };

  // =========================================================================
  // KONUM GÜNCELLEME
  // =========================================================================
  const handlePositionUpdate = useCallback(
    (position) => {
      const {
        latitude,
        longitude,
        accuracy: acc,
        speed,
        heading,
      } = position.coords;
      const timestamp = new Date().toISOString();

      const newLocation = {
        latitude,
        longitude,
        accuracy: acc,
        speed,
        heading,
        timestamp,
      };

      setCurrentLocation(newLocation);
      setAccuracy(acc);
      setLocationError(null);
      setLastUpdateTime(new Date());

      // Minimum mesafe kontrolü
      const lastPos = lastPositionRef.current;
      const shouldSend =
        !lastPos ||
        calculateDistance(
          lastPos.latitude,
          lastPos.longitude,
          latitude,
          longitude,
        ) >= settings.minDistance;

      if (shouldSend && isConnected && isOnline) {
        sendLocationUpdate(latitude, longitude, acc);
        lastPositionRef.current = { latitude, longitude };

        // Callback
        onLocationUpdate?.(newLocation);
      }
    },
    [
      isConnected,
      isOnline,
      sendLocationUpdate,
      onLocationUpdate,
      settings.minDistance,
    ],
  );

  const handlePositionError = useCallback((error) => {
    let errorMessage = "Konum alınamadı";

    switch (error.code) {
      case error.PERMISSION_DENIED:
        errorMessage = "Konum izni reddedildi";
        setPermissionStatus("denied");
        break;
      case error.POSITION_UNAVAILABLE:
        errorMessage = "Konum bilgisi kullanılamıyor";
        break;
      case error.TIMEOUT:
        errorMessage = "Konum alma zaman aşımı";
        break;
      default:
        errorMessage = "Bilinmeyen konum hatası";
    }

    setLocationError(errorMessage);
    console.error("Konum hatası:", error);
  }, []);

  // =========================================================================
  // TAKİP BAŞLAT/DURDUR
  // =========================================================================
  const startTracking = useCallback(() => {
    if (!("geolocation" in navigator)) {
      setLocationError("Geolocation desteklenmiyor");
      return;
    }

    if (watchIdRef.current) {
      navigator.geolocation.clearWatch(watchIdRef.current);
    }

    // Sürekli izleme
    watchIdRef.current = navigator.geolocation.watchPosition(
      handlePositionUpdate,
      handlePositionError,
      {
        enableHighAccuracy: settings.enableHighAccuracy,
        timeout: settings.timeout,
        maximumAge: settings.maximumAge,
      },
    );

    setTracking(true);
    setLocationError(null);

    // Periyodik güncelleme (watchPosition çalışmasa bile)
    updateIntervalRef.current = setInterval(() => {
      navigator.geolocation.getCurrentPosition(
        handlePositionUpdate,
        handlePositionError,
        {
          enableHighAccuracy: settings.enableHighAccuracy,
          timeout: settings.timeout,
          maximumAge: 0,
        },
      );
    }, settings.updateInterval);
  }, [handlePositionUpdate, handlePositionError, settings]);

  const stopTracking = useCallback(() => {
    if (watchIdRef.current) {
      navigator.geolocation.clearWatch(watchIdRef.current);
      watchIdRef.current = null;
    }

    if (updateIntervalRef.current) {
      clearInterval(updateIntervalRef.current);
      updateIntervalRef.current = null;
    }

    setTracking(false);
  }, []);

  // =========================================================================
  // LIFECYCLE
  // =========================================================================
  useEffect(() => {
    if (enabled && isOnline && permissionStatus !== "denied") {
      startTracking();
    } else {
      stopTracking();
    }

    return () => stopTracking();
  }, [enabled, isOnline, permissionStatus, startTracking, stopTracking]);

  // =========================================================================
  // HELPER FUNCTIONS
  // =========================================================================
  // İki nokta arası mesafe (metre cinsinden)
  const calculateDistance = (lat1, lon1, lat2, lon2) => {
    const R = 6371e3; // Dünya yarıçapı (metre)
    const φ1 = (lat1 * Math.PI) / 180;
    const φ2 = (lat2 * Math.PI) / 180;
    const Δφ = ((lat2 - lat1) * Math.PI) / 180;
    const Δλ = ((lon2 - lon1) * Math.PI) / 180;

    const a =
      Math.sin(Δφ / 2) * Math.sin(Δφ / 2) +
      Math.cos(φ1) * Math.cos(φ2) * Math.sin(Δλ / 2) * Math.sin(Δλ / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

    return R * c;
  };

  const formatAccuracy = (acc) => {
    if (!acc) return "--";
    if (acc < 10) return "< 10m";
    if (acc < 50) return `~${Math.round(acc)}m`;
    return `${Math.round(acc)}m`;
  };

  const getAccuracyColor = (acc) => {
    if (!acc) return "#6c757d";
    if (acc < 15) return "#28a745";
    if (acc < 50) return "#ffc107";
    return "#dc3545";
  };

  const formatLastUpdate = () => {
    if (!lastUpdateTime) return "Henüz güncellenmedi";
    const seconds = Math.floor((new Date() - lastUpdateTime) / 1000);
    if (seconds < 60) return `${seconds} sn önce`;
    const minutes = Math.floor(seconds / 60);
    return `${minutes} dk önce`;
  };

  // İzin isteme
  const requestPermission = async () => {
    try {
      await navigator.geolocation.getCurrentPosition(
        handlePositionUpdate,
        handlePositionError,
        { enableHighAccuracy: true },
      );
      setPermissionStatus("granted");
    } catch (e) {
      console.error("Permission request failed:", e);
    }
  };

  // =========================================================================
  // RENDER
  // =========================================================================
  if (!showStatus) {
    return null; // Sadece arka planda çalış
  }

  // İzin reddedilmişse
  if (permissionStatus === "denied") {
    return (
      <div
        className="alert alert-warning d-flex align-items-center"
        style={{ borderRadius: "12px" }}
      >
        <i className="fas fa-exclamation-triangle me-3 fs-4"></i>
        <div className="flex-grow-1">
          <strong>Konum İzni Gerekli</strong>
          <p className="mb-0 small">
            Konum takibi için tarayıcı ayarlarından izin verin
          </p>
        </div>
      </div>
    );
  }

  // İzin bekleniyorsa
  if (permissionStatus === "prompt") {
    return (
      <div className="card border-0 shadow-sm" style={{ borderRadius: "16px" }}>
        <div className="card-body text-center p-4">
          <div
            className="rounded-circle mx-auto mb-3 d-flex align-items-center justify-content-center"
            style={{
              width: "60px",
              height: "60px",
              backgroundColor: "#e3f2fd",
            }}
          >
            <i className="fas fa-map-marker-alt text-primary fs-4"></i>
          </div>
          <h6 className="fw-bold mb-2">Konum İzni Gerekli</h6>
          <p className="text-muted small mb-3">
            Teslimat takibi için konumunuzu paylaşmanız gerekiyor
          </p>
          <button
            className="btn btn-primary"
            onClick={requestPermission}
            style={{ borderRadius: "10px" }}
          >
            <i className="fas fa-check me-2"></i>
            İzin Ver
          </button>
        </div>
      </div>
    );
  }

  // Normal durum
  return (
    <div className="card border-0 shadow-sm" style={{ borderRadius: "16px" }}>
      <div className="card-body p-3">
        <div className="d-flex align-items-center justify-content-between mb-3">
          <h6 className="fw-bold mb-0">
            <i
              className="fas fa-satellite me-2"
              style={{ color: "#ff6b35" }}
            ></i>
            Konum Takibi
          </h6>
          <div className="form-check form-switch">
            <input
              className="form-check-input"
              type="checkbox"
              id="trackingToggle"
              checked={tracking && isOnline}
              onChange={(e) => {
                if (e.target.checked) {
                  startTracking();
                } else {
                  stopTracking();
                }
              }}
              disabled={!isOnline}
              style={{ width: "2.5em", height: "1.25em" }}
            />
          </div>
        </div>

        {/* Durum Göstergesi */}
        <div className="d-flex align-items-center gap-3 mb-3">
          {/* Takip Durumu */}
          <div className="d-flex align-items-center">
            <div
              className={`rounded-circle me-2 ${tracking ? "pulse-dot" : ""}`}
              style={{
                width: "10px",
                height: "10px",
                backgroundColor: tracking ? "#28a745" : "#6c757d",
              }}
            ></div>
            <small className={tracking ? "text-success" : "text-muted"}>
              {tracking ? "Aktif" : "Pasif"}
            </small>
          </div>

          {/* Doğruluk */}
          {accuracy && (
            <div className="d-flex align-items-center">
              <i
                className="fas fa-crosshairs me-1"
                style={{ color: getAccuracyColor(accuracy), fontSize: "12px" }}
              ></i>
              <small style={{ color: getAccuracyColor(accuracy) }}>
                {formatAccuracy(accuracy)}
              </small>
            </div>
          )}

          {/* Son Güncelleme */}
          <div className="d-flex align-items-center text-muted">
            <i className="fas fa-clock me-1" style={{ fontSize: "12px" }}></i>
            <small>{formatLastUpdate()}</small>
          </div>
        </div>

        {/* Hata Mesajı */}
        {locationError && (
          <div
            className="alert alert-danger py-2 mb-0"
            style={{ borderRadius: "10px" }}
          >
            <small>
              <i className="fas fa-exclamation-circle me-2"></i>
              {locationError}
            </small>
          </div>
        )}

        {/* Koordinatlar (Debug için) */}
        {currentLocation && process.env.NODE_ENV === "development" && (
          <div className="mt-2 p-2 bg-light rounded small">
            <code className="text-muted">
              {currentLocation.latitude.toFixed(6)},{" "}
              {currentLocation.longitude.toFixed(6)}
            </code>
          </div>
        )}
      </div>

      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }
        .pulse-dot {
          animation: pulse 1.5s infinite;
        }
      `}</style>
    </div>
  );
}

// =========================================================================
// HOOK VERSİYONU
// =========================================================================
export function useCourierLocation(config = {}) {
  const [location, setLocation] = useState(null);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!("geolocation" in navigator)) {
      setError("Geolocation desteklenmiyor");
      setLoading(false);
      return;
    }

    const watchId = navigator.geolocation.watchPosition(
      (position) => {
        setLocation({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          accuracy: position.coords.accuracy,
          timestamp: new Date().toISOString(),
        });
        setError(null);
        setLoading(false);
      },
      (err) => {
        setError(err.message);
        setLoading(false);
      },
      {
        enableHighAccuracy: config.enableHighAccuracy ?? true,
        timeout: config.timeout ?? 10000,
        maximumAge: config.maximumAge ?? 5000,
      },
    );

    return () => navigator.geolocation.clearWatch(watchId);
  }, [config.enableHighAccuracy, config.timeout, config.maximumAge]);

  return { location, error, loading };
}

// =========================================================================
// UTILITY: Mesafe Hesaplama
// =========================================================================
export function calculateDistanceBetween(lat1, lon1, lat2, lon2) {
  const R = 6371e3;
  const φ1 = (lat1 * Math.PI) / 180;
  const φ2 = (lat2 * Math.PI) / 180;
  const Δφ = ((lat2 - lat1) * Math.PI) / 180;
  const Δλ = ((lon2 - lon1) * Math.PI) / 180;

  const a =
    Math.sin(Δφ / 2) * Math.sin(Δφ / 2) +
    Math.cos(φ1) * Math.cos(φ2) * Math.sin(Δλ / 2) * Math.sin(Δλ / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

  return R * c;
}
