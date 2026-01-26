// ==========================================================================
// CourierSelect.jsx - Kurye Seçim Dropdown Komponenti
// ==========================================================================
// Müsait kuryeleri listeler ve seçim yapılmasını sağlar.
// NEDEN: Kurye atama işlemlerinde tekrar kullanılabilir dropdown.
// ==========================================================================

import React, { useState, useMemo } from "react";

export default function CourierSelect({
  couriers = [],
  selectedId = null,
  onSelect,
  compact = false,
  excludeIds = [],
}) {
  // =========================================================================
  // STATE
  // =========================================================================
  const [searchQuery, setSearchQuery] = useState("");

  // =========================================================================
  // FİLTRELENMİŞ KURYE LİSTESİ
  // NEDEN: Arama ve hariç tutulan kuryeleri filtreler
  // =========================================================================
  const filteredCouriers = useMemo(() => {
    return couriers.filter((courier) => {
      // Hariç tutulan kuryeleri filtrele
      if (excludeIds.includes(courier.id || courier.courierId)) {
        return false;
      }

      // Arama filtresi
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        const name = (courier.name || courier.courierName || "").toLowerCase();
        return name.includes(query);
      }

      return true;
    });
  }, [couriers, searchQuery, excludeIds]);

  // =========================================================================
  // YARDIMCI FONKSİYONLAR
  // =========================================================================

  // Kurye durumu badge'i
  const getStatusBadge = (courier) => {
    if (!courier.isOnline) {
      return {
        color: "#dc3545",
        bg: "rgba(220,53,69,0.2)",
        text: "Çevrimdışı",
        icon: "circle",
      };
    }

    const activeCount = courier.activeOrderCount || 0;

    if (activeCount === 0) {
      return {
        color: "#28a745",
        bg: "rgba(40,167,69,0.2)",
        text: "Müsait",
        icon: "circle",
      };
    } else if (activeCount < 3) {
      return {
        color: "#ffc107",
        bg: "rgba(255,193,7,0.2)",
        text: `${activeCount} Sipariş`,
        icon: "circle",
      };
    } else {
      return {
        color: "#dc3545",
        bg: "rgba(220,53,69,0.2)",
        text: "Meşgul",
        icon: "circle",
      };
    }
  };

  // Araç tipi ikonu
  const getVehicleIcon = (vehicleType) => {
    switch (vehicleType) {
      case "Motorcycle":
        return "fa-motorcycle";
      case "Bicycle":
        return "fa-bicycle";
      case "Car":
        return "fa-car";
      case "OnFoot":
        return "fa-walking";
      default:
        return "fa-motorcycle";
    }
  };

  // =========================================================================
  // RENDER
  // =========================================================================
  return (
    <div className={compact ? "" : "courier-select-container"}>
      {/* Arama Kutusu - Sadece compact değilse */}
      {!compact && couriers.length > 5 && (
        <div className="mb-3">
          <div className="input-group">
            <span
              className="input-group-text border-0"
              style={{ background: "rgba(255,255,255,0.1)" }}
            >
              <i
                className="fas fa-search text-white-50"
                style={{ fontSize: "0.8rem" }}
              ></i>
            </span>
            <input
              type="text"
              className="form-control border-0 text-white"
              placeholder="Kurye ara..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              style={{
                background: "rgba(255,255,255,0.1)",
                fontSize: "0.9rem",
              }}
            />
          </div>
        </div>
      )}

      {/* Kurye Listesi */}
      <div
        className="courier-list"
        style={{
          maxHeight: compact ? "200px" : "300px",
          overflowY: "auto",
        }}
      >
        {filteredCouriers.length === 0 ? (
          <div
            className="text-center py-4"
            style={{
              background: "rgba(255,255,255,0.05)",
              borderRadius: "8px",
            }}
          >
            <i
              className="fas fa-user-slash text-white-50 mb-2"
              style={{ fontSize: "1.5rem" }}
            ></i>
            <p className="text-white-50 mb-0 small">
              {searchQuery ? "Kurye bulunamadı" : "Müsait kurye yok"}
            </p>
          </div>
        ) : (
          <div className="d-flex flex-column gap-2">
            {filteredCouriers.map((courier) => {
              const id = courier.id || courier.courierId;
              const name = courier.name || courier.courierName;
              const isSelected = selectedId === id;
              const status = getStatusBadge(courier);
              const vehicleIcon = getVehicleIcon(courier.vehicleType);

              return (
                <button
                  key={id}
                  className={`btn text-start p-2 d-flex align-items-center ${isSelected ? "border-primary" : ""}`}
                  onClick={() => onSelect(id)}
                  style={{
                    background: isSelected
                      ? "rgba(102,126,234,0.2)"
                      : "rgba(255,255,255,0.05)",
                    border: isSelected
                      ? "2px solid #667eea"
                      : "2px solid transparent",
                    borderRadius: "10px",
                    transition: "all 0.2s ease",
                  }}
                  onMouseEnter={(e) => {
                    if (!isSelected) {
                      e.currentTarget.style.background =
                        "rgba(255,255,255,0.1)";
                    }
                  }}
                  onMouseLeave={(e) => {
                    if (!isSelected) {
                      e.currentTarget.style.background =
                        "rgba(255,255,255,0.05)";
                    }
                  }}
                >
                  {/* Kurye Avatarı */}
                  <div
                    className="d-flex align-items-center justify-content-center me-2"
                    style={{
                      width: compact ? "36px" : "40px",
                      height: compact ? "36px" : "40px",
                      borderRadius: "50%",
                      background: isSelected
                        ? "linear-gradient(135deg, #667eea 0%, #764ba2 100%)"
                        : "rgba(255,255,255,0.1)",
                      position: "relative",
                    }}
                  >
                    <i
                      className={`fas ${vehicleIcon} ${isSelected ? "text-white" : "text-white-50"}`}
                      style={{ fontSize: compact ? "0.8rem" : "0.9rem" }}
                    ></i>

                    {/* Online/Offline Göstergesi */}
                    <span
                      className="position-absolute"
                      style={{
                        bottom: "0",
                        right: "0",
                        width: "10px",
                        height: "10px",
                        borderRadius: "50%",
                        background: status.color,
                        border: "2px solid #2d2d44",
                      }}
                    ></span>
                  </div>

                  {/* Kurye Bilgileri */}
                  <div className="flex-grow-1 min-width-0">
                    <div className="d-flex align-items-center">
                      <span
                        className={`fw-semibold ${isSelected ? "text-white" : "text-white"}`}
                        style={{ fontSize: compact ? "0.85rem" : "0.9rem" }}
                      >
                        {name}
                      </span>
                      {isSelected && (
                        <i
                          className="fas fa-check-circle text-success ms-2"
                          style={{ fontSize: "0.8rem" }}
                        ></i>
                      )}
                    </div>

                    <div className="d-flex align-items-center gap-2 mt-1">
                      {/* Durum Badge */}
                      <span
                        className="badge"
                        style={{
                          background: status.bg,
                          color: status.color,
                          fontSize: "0.65rem",
                          fontWeight: "500",
                        }}
                      >
                        <i
                          className={`fas fa-${status.icon} me-1`}
                          style={{ fontSize: "0.5rem" }}
                        ></i>
                        {status.text}
                      </span>

                      {/* Bugünkü Teslimler */}
                      {courier.completedToday > 0 && (
                        <span
                          className="text-white-50"
                          style={{ fontSize: "0.7rem" }}
                        >
                          <i className="fas fa-check me-1"></i>
                          {courier.completedToday} teslim
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Seçim İkonu */}
                  <div className="ms-2">
                    <div
                      className="d-flex align-items-center justify-content-center"
                      style={{
                        width: "24px",
                        height: "24px",
                        borderRadius: "50%",
                        border: isSelected
                          ? "2px solid #667eea"
                          : "2px solid rgba(255,255,255,0.2)",
                        background: isSelected ? "#667eea" : "transparent",
                      }}
                    >
                      {isSelected && (
                        <i
                          className="fas fa-check text-white"
                          style={{ fontSize: "0.7rem" }}
                        ></i>
                      )}
                    </div>
                  </div>
                </button>
              );
            })}
          </div>
        )}
      </div>

      {/* Alt Bilgi */}
      {!compact && filteredCouriers.length > 0 && (
        <div className="mt-3 text-center">
          <small className="text-white-50">
            {filteredCouriers.filter((c) => c.isOnline).length} müsait /{" "}
            {filteredCouriers.length} toplam kurye
          </small>
        </div>
      )}
    </div>
  );
}
