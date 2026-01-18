// ==========================================================================
// CourierActionButtons.jsx - Teslimat Durum Aksiyon Butonları
// ==========================================================================
// State machine mantığıyla çalışan aksiyon butonları.
// Her durum için uygun bir sonraki adım gösterilir.
// ==========================================================================

import React from "react";

// Durum geçiş kuralları (State Machine)
const STATE_TRANSITIONS = {
  Pending: ["Assigned"], // Bekliyor → Kabul Et
  Assigned: ["PickedUp"], // Atandı → Teslim Al
  PickedUp: ["InTransit"], // Alındı → Yola Çık
  InTransit: ["Delivered", "Failed"], // Yolda → Teslim Et / Başarısız
  Delivered: [], // Final state
  Failed: [], // Final state
  Cancelled: [], // Final state
};

// Buton konfigürasyonları
const BUTTON_CONFIG = {
  Assigned: {
    label: "Görevi Kabul Et",
    icon: "fa-check",
    color: "success",
    gradient: "linear-gradient(135deg, #28a745, #20c997)",
  },
  PickedUp: {
    label: "Siparişi Aldım",
    icon: "fa-box",
    color: "primary",
    gradient: "linear-gradient(135deg, #667eea, #764ba2)",
  },
  InTransit: {
    label: "Yola Çıktım",
    icon: "fa-truck",
    color: "info",
    gradient: "linear-gradient(135deg, #11998e, #38ef7d)",
  },
  Delivered: {
    label: "Teslim Ettim",
    icon: "fa-check-circle",
    color: "success",
    gradient: "linear-gradient(135deg, #ff6b35, #ff8c00)",
  },
  Failed: {
    label: "Teslim Edilemedi",
    icon: "fa-times-circle",
    color: "danger",
    gradient: "linear-gradient(135deg, #dc3545, #c82333)",
  },
};

export default function CourierActionButtons({
  task,
  onStatusChange,
  loading,
}) {
  if (!task) return null;

  const currentStatus = task.status;
  const nextStates = STATE_TRANSITIONS[currentStatus] || [];

  // Final state ise buton gösterme
  if (nextStates.length === 0) {
    return null;
  }

  return (
    <div className="d-flex flex-column gap-2">
      {/* Ana Aksiyon Butonu */}
      {nextStates.length > 0 && (
        <div className="d-flex gap-2">
          {/* Primary Action (Delivered hariç ilk durum) */}
          {nextStates
            .filter((s) => s !== "Failed")
            .map((nextStatus) => {
              const config = BUTTON_CONFIG[nextStatus];
              if (!config) return null;

              return (
                <button
                  key={nextStatus}
                  className="btn btn-lg flex-grow-1 text-white fw-bold shadow"
                  style={{
                    background: config.gradient,
                    border: "none",
                    borderRadius: "14px",
                    padding: "14px 20px",
                  }}
                  onClick={() => onStatusChange(nextStatus)}
                  disabled={loading}
                >
                  {loading ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-2"></span>
                      İşleniyor...
                    </>
                  ) : (
                    <>
                      <i className={`fas ${config.icon} me-2`}></i>
                      {config.label}
                    </>
                  )}
                </button>
              );
            })}
        </div>
      )}

      {/* Başarısız Butonu (Sadece InTransit durumunda) */}
      {nextStates.includes("Failed") && (
        <button
          className="btn btn-outline-danger w-100 fw-semibold"
          style={{
            borderRadius: "14px",
            padding: "12px 20px",
            borderWidth: "2px",
          }}
          onClick={() => onStatusChange("Failed")}
          disabled={loading}
        >
          <i className="fas fa-times-circle me-2"></i>
          Teslim Edilemedi
        </button>
      )}

      {/* Durum Bilgisi */}
      <div className="text-center mt-1">
        <small className="text-muted">
          <i className="fas fa-info-circle me-1"></i>
          {getStatusHint(currentStatus)}
        </small>
      </div>
    </div>
  );
}

// Durum için ipucu metni
function getStatusHint(status) {
  switch (status) {
    case "Pending":
      return "Bu görevi kabul etmek için butona tıklayın";
    case "Assigned":
      return "Siparişi teslim aldığınızda butona tıklayın";
    case "PickedUp":
      return "Teslimat adresine yola çıktığınızda butona tıklayın";
    case "InTransit":
      return "Teslimatı tamamladığınızda onay butonuna tıklayın";
    default:
      return "";
  }
}

// =========================================================================
// ALT COMPONENTLER
// =========================================================================

// Swipe to confirm butonu (opsiyonel - gelişmiş versiyon için)
export function SwipeToConfirmButton({ label, onConfirm, color = "success" }) {
  return (
    <div
      className={`position-relative bg-${color} rounded-pill overflow-hidden`}
      style={{ height: "56px" }}
    >
      <div className="position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center text-white fw-bold">
        <i className="fas fa-chevron-right me-2"></i>
        Kaydırarak {label}
      </div>
      {/* Kaydırılabilir thumb eklenebilir */}
    </div>
  );
}

// Durum progress bar'ı
export function DeliveryProgressBar({ status }) {
  const stages = ["Pending", "Assigned", "PickedUp", "InTransit", "Delivered"];
  const currentIndex = stages.indexOf(status);
  const progress = ((currentIndex + 1) / stages.length) * 100;

  return (
    <div className="mb-3">
      <div className="d-flex justify-content-between mb-2">
        {stages.map((stage, index) => (
          <div
            key={stage}
            className={`text-center flex-grow-1 ${index <= currentIndex ? "text-primary" : "text-muted"}`}
            style={{ fontSize: "10px" }}
          >
            <div
              className={`rounded-circle mx-auto mb-1 d-flex align-items-center justify-content-center ${
                index < currentIndex
                  ? "bg-success"
                  : index === currentIndex
                    ? "bg-primary"
                    : "bg-secondary bg-opacity-25"
              }`}
              style={{ width: "24px", height: "24px" }}
            >
              {index < currentIndex ? (
                <i
                  className="fas fa-check text-white"
                  style={{ fontSize: "10px" }}
                ></i>
              ) : (
                <span className="text-white fw-bold">{index + 1}</span>
              )}
            </div>
            {getStageLabel(stage)}
          </div>
        ))}
      </div>
      <div className="progress" style={{ height: "4px", borderRadius: "2px" }}>
        <div
          className="progress-bar bg-primary"
          style={{
            width: `${progress}%`,
            transition: "width 0.3s ease",
          }}
        ></div>
      </div>
    </div>
  );
}

function getStageLabel(status) {
  const labels = {
    Pending: "Bekliyor",
    Assigned: "Atandı",
    PickedUp: "Alındı",
    InTransit: "Yolda",
    Delivered: "Teslim",
  };
  return labels[status] || status;
}
