// ==========================================================================
// CourierStatusBar.jsx - Kurye Durum Çubuğu Komponenti
// ==========================================================================
// Dashboard'un üst kısmında yatay olarak kuryeleri gösterir.
// Her kuryenin durumu ve aktif sipariş sayısı görünür.
// NEDEN: Hızlı bir bakışta tüm kuryelerin durumunu görmek için.
// ==========================================================================

import React from "react";

export default function CourierStatusBar({ couriers = [], onCourierClick }) {
  // =========================================================================
  // YARDIMCI FONKSİYONLAR
  // =========================================================================

  // Kurye durumu rengi
  const getStatusColor = (courier) => {
    if (!courier.isOnline) return "#dc3545"; // Offline - Kırmızı

    const activeCount = courier.activeOrderCount || 0;
    if (activeCount === 0) return "#28a745"; // Müsait - Yeşil
    if (activeCount < 3) return "#ffc107"; // Meşgul - Sarı
    return "#dc3545"; // Çok meşgul - Kırmızı
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

  // Online kurye sayısı
  const onlineCouriers = couriers.filter((c) => c.isOnline);
  const availableCouriers = couriers.filter(
    (c) => c.isOnline && (c.activeOrderCount || 0) === 0,
  );

  // =========================================================================
  // RENDER
  // =========================================================================
  return (
    <div
      className="py-3 px-4"
      style={{
        background: "rgba(0,0,0,0.2)",
        borderBottom: "1px solid rgba(255,255,255,0.05)",
      }}
    >
      <div className="container-fluid">
        {/* Başlık ve Özet */}
        <div className="d-flex justify-content-between align-items-center mb-3">
          <div className="d-flex align-items-center">
            <i className="fas fa-users text-info me-2"></i>
            <span className="text-white fw-semibold">Kuryeler</span>
            <span className="badge bg-info ms-2">
              {onlineCouriers.length} Online
            </span>
            {availableCouriers.length > 0 && (
              <span className="badge bg-success ms-1">
                {availableCouriers.length} Müsait
              </span>
            )}
          </div>

          {/* Toplam Kurye */}
          <small className="text-white-50">
            Toplam: {couriers.length} kurye
          </small>
        </div>

        {/* Kurye Listesi - Yatay Kaydırılabilir */}
        <div
          className="d-flex gap-3 pb-2"
          style={{
            overflowX: "auto",
            scrollbarWidth: "thin",
            scrollbarColor: "rgba(255,255,255,0.2) transparent",
          }}
        >
          {couriers.length === 0 ? (
            <div className="text-center py-3 w-100">
              <i
                className="fas fa-user-slash text-white-50 mb-2"
                style={{ fontSize: "1.5rem" }}
              ></i>
              <p className="text-white-50 mb-0">Henüz kurye eklenmemiş</p>
            </div>
          ) : (
            couriers.map((courier) => {
              const id = courier.id || courier.courierId;
              const name = courier.name || courier.courierName;
              const statusColor = getStatusColor(courier);
              const vehicleIcon = getVehicleIcon(courier.vehicleType);
              const activeCount = courier.activeOrderCount || 0;

              return (
                <button
                  key={id}
                  className="btn p-0 text-start"
                  onClick={() => onCourierClick?.(courier)}
                  style={{
                    background: "transparent",
                    border: "none",
                    minWidth: "120px",
                    flexShrink: 0,
                  }}
                >
                  <div
                    className="d-flex flex-column align-items-center p-3"
                    style={{
                      background: "rgba(255,255,255,0.05)",
                      borderRadius: "12px",
                      transition: "all 0.2s ease",
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.background =
                        "rgba(255,255,255,0.1)";
                      e.currentTarget.style.transform = "translateY(-2px)";
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.background =
                        "rgba(255,255,255,0.05)";
                      e.currentTarget.style.transform = "translateY(0)";
                    }}
                  >
                    {/* Avatar ve Durum Göstergesi */}
                    <div className="position-relative mb-2">
                      <div
                        className="d-flex align-items-center justify-content-center"
                        style={{
                          width: "48px",
                          height: "48px",
                          borderRadius: "50%",
                          background: `linear-gradient(135deg, ${statusColor}33 0%, ${statusColor}11 100%)`,
                          border: `2px solid ${statusColor}`,
                        }}
                      >
                        <i
                          className={`fas ${vehicleIcon}`}
                          style={{
                            fontSize: "1.1rem",
                            color: statusColor,
                          }}
                        ></i>
                      </div>

                      {/* Online/Offline Nokta */}
                      <span
                        className="position-absolute"
                        style={{
                          bottom: "2px",
                          right: "2px",
                          width: "14px",
                          height: "14px",
                          borderRadius: "50%",
                          background: statusColor,
                          border: "2px solid #1a1a2e",
                        }}
                      ></span>
                    </div>

                    {/* İsim */}
                    <span
                      className="text-white text-center fw-medium mb-1"
                      style={{
                        fontSize: "0.85rem",
                        whiteSpace: "nowrap",
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        maxWidth: "100px",
                      }}
                    >
                      {name?.split(" ")[0] || "Kurye"}
                    </span>

                    {/* Aktif Sipariş Sayısı */}
                    {courier.isOnline ? (
                      <span
                        className="badge"
                        style={{
                          background:
                            activeCount === 0
                              ? "rgba(40,167,69,0.2)"
                              : "rgba(255,193,7,0.2)",
                          color: activeCount === 0 ? "#28a745" : "#ffc107",
                          fontSize: "0.7rem",
                        }}
                      >
                        {activeCount === 0 ? (
                          <>
                            <i className="fas fa-check me-1"></i>
                            Müsait
                          </>
                        ) : (
                          <>
                            <i className="fas fa-box me-1"></i>
                            {activeCount} Sipariş
                          </>
                        )}
                      </span>
                    ) : (
                      <span
                        className="badge"
                        style={{
                          background: "rgba(220,53,69,0.2)",
                          color: "#dc3545",
                          fontSize: "0.7rem",
                        }}
                      >
                        <i className="fas fa-power-off me-1"></i>
                        Çevrimdışı
                      </span>
                    )}

                    {/* Bugünkü Teslimler */}
                    {courier.completedToday > 0 && (
                      <small
                        className="text-white-50 mt-1"
                        style={{ fontSize: "0.65rem" }}
                      >
                        Bugün: {courier.completedToday} teslim
                      </small>
                    )}
                  </div>
                </button>
              );
            })
          )}
        </div>
      </div>
    </div>
  );
}
