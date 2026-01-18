// ==========================================================================
// CourierFailureModal.jsx - Teslimat Başarısızlık Raporu Modal
// ==========================================================================
// Teslimat başarısız olduğunda sebep seçimi ve açıklama girişi.
// Opsiyonel fotoğraf ekleme desteği.
// ==========================================================================

import React, { useState, useRef } from "react";

// Önceden tanımlı başarısızlık sebepleri
const FAILURE_REASONS = [
  {
    id: "customer_not_available",
    label: "Müşteri Mevcut Değil",
    icon: "fa-user-slash",
    description: "Müşteri evde/işyerinde bulunamadı",
  },
  {
    id: "wrong_address",
    label: "Yanlış Adres",
    icon: "fa-map-marker-alt",
    description: "Adres bilgisi yanlış veya eksik",
  },
  {
    id: "customer_rejected",
    label: "Müşteri Reddetti",
    icon: "fa-hand-paper",
    description: "Müşteri siparişi teslim almayı reddetti",
  },
  {
    id: "damaged_package",
    label: "Hasarlı Paket",
    icon: "fa-box-open",
    description: "Ürün veya ambalaj hasarlı",
  },
  {
    id: "access_denied",
    label: "Erişim Engeli",
    icon: "fa-lock",
    description: "Binaya/siteye giriş yapılamadı",
  },
  {
    id: "payment_issue",
    label: "Ödeme Sorunu",
    icon: "fa-credit-card",
    description: "Kapıda ödeme alınamadı",
  },
  {
    id: "weather_conditions",
    label: "Hava Koşulları",
    icon: "fa-cloud-rain",
    description: "Kötü hava koşulları nedeniyle teslim edilemedi",
  },
  {
    id: "vehicle_issue",
    label: "Araç Arızası",
    icon: "fa-motorcycle",
    description: "Araç arızası nedeniyle teslim edilemedi",
  },
  {
    id: "other",
    label: "Diğer",
    icon: "fa-ellipsis-h",
    description: "Başka bir sebep",
  },
];

export default function CourierFailureModal({
  task,
  onSubmit,
  onClose,
  loading,
}) {
  // State
  const [selectedReason, setSelectedReason] = useState(null);
  const [additionalNotes, setAdditionalNotes] = useState("");
  const [photoData, setPhotoData] = useState(null);
  const [showCamera, setShowCamera] = useState(false);
  const [attemptedDelivery, setAttemptedDelivery] = useState(true);

  // Refs
  const fileInputRef = useRef(null);
  const videoRef = useRef(null);
  const canvasRef = useRef(null);
  const cameraStreamRef = useRef(null);

  // =========================================================================
  // FOTOĞRAF İŞLEMLERİ
  // =========================================================================
  const handleFileSelect = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (event) => {
      setPhotoData(event.target.result);
    };
    reader.readAsDataURL(file);
  };

  const startCamera = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: "environment" },
      });

      if (videoRef.current) {
        videoRef.current.srcObject = stream;
        cameraStreamRef.current = stream;
        setShowCamera(true);
      }
    } catch (err) {
      console.error("Kamera hatası:", err);
      // Dosya seçiciye yönlendir
      fileInputRef.current?.click();
    }
  };

  const capturePhoto = () => {
    if (!videoRef.current || !canvasRef.current) return;

    const video = videoRef.current;
    const canvas = canvasRef.current;
    const ctx = canvas.getContext("2d");

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    ctx.drawImage(video, 0, 0);

    const imageData = canvas.toDataURL("image/jpeg", 0.8);
    setPhotoData(imageData);
    stopCamera();
  };

  const stopCamera = () => {
    if (cameraStreamRef.current) {
      cameraStreamRef.current.getTracks().forEach((track) => track.stop());
      cameraStreamRef.current = null;
    }
    setShowCamera(false);
  };

  const removePhoto = () => {
    setPhotoData(null);
    stopCamera();
  };

  // =========================================================================
  // FORM GÖNDERME
  // =========================================================================
  const handleSubmit = () => {
    if (!selectedReason) return;

    onSubmit({
      reasonCode: selectedReason.id,
      reasonLabel: selectedReason.label,
      additionalNotes: additionalNotes.trim(),
      photoBase64: photoData?.split(",")[1],
      attemptedDelivery,
      timestamp: new Date().toISOString(),
    });
  };

  const isValid = selectedReason !== null;

  // =========================================================================
  // RENDER
  // =========================================================================
  return (
    <div
      className="position-fixed top-0 start-0 w-100 h-100 d-flex flex-column"
      style={{ backgroundColor: "#fff", zIndex: 2000 }}
    >
      {/* Header */}
      <div
        className="d-flex align-items-center justify-content-between p-3"
        style={{
          background: "linear-gradient(135deg, #dc3545, #c82333)",
        }}
      >
        <button className="btn btn-link text-white p-0" onClick={onClose}>
          <i className="fas fa-times fs-5"></i>
        </button>
        <h6 className="text-white mb-0 fw-bold">
          <i className="fas fa-exclamation-triangle me-2"></i>
          Teslimat Başarısız
        </h6>
        <div style={{ width: "24px" }}></div>
      </div>

      {/* Order Info */}
      <div className="bg-light p-3 border-bottom">
        <div className="d-flex align-items-center">
          <div
            className="rounded-circle bg-danger bg-opacity-10 p-2 me-3 d-flex align-items-center justify-content-center"
            style={{ width: "40px", height: "40px" }}
          >
            <i className="fas fa-box text-danger"></i>
          </div>
          <div>
            <h6 className="mb-0 fw-bold">
              Sipariş #{task?.orderId || task?.id}
            </h6>
            <small className="text-muted">{task?.customerName}</small>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="flex-grow-1 overflow-auto p-3">
        {/* Kamera Modal */}
        {showCamera && (
          <div
            className="position-fixed top-0 start-0 w-100 h-100 bg-black d-flex flex-column"
            style={{ zIndex: 2100 }}
          >
            <div className="flex-grow-1 position-relative">
              <video
                ref={videoRef}
                autoPlay
                playsInline
                muted
                className="w-100 h-100"
                style={{ objectFit: "cover" }}
              />
              <canvas ref={canvasRef} className="d-none"></canvas>
            </div>
            <div className="bg-black p-4 d-flex justify-content-center gap-4">
              <button
                className="btn btn-outline-light rounded-circle"
                style={{ width: "60px", height: "60px" }}
                onClick={stopCamera}
              >
                <i className="fas fa-times"></i>
              </button>
              <button
                className="btn btn-danger rounded-circle"
                style={{ width: "70px", height: "70px" }}
                onClick={capturePhoto}
              >
                <i className="fas fa-camera fs-4"></i>
              </button>
            </div>
          </div>
        )}

        {/* Sebep Seçimi */}
        <div className="mb-4">
          <h6 className="fw-bold mb-3">
            <i className="fas fa-question-circle me-2 text-muted"></i>
            Başarısızlık Sebebi
          </h6>
          <div className="row g-2">
            {FAILURE_REASONS.map((reason) => (
              <div key={reason.id} className="col-6">
                <div
                  className={`card h-100 border-2 ${
                    selectedReason?.id === reason.id
                      ? "border-danger bg-danger bg-opacity-10"
                      : "border-light"
                  }`}
                  style={{
                    borderRadius: "12px",
                    cursor: "pointer",
                    transition: "all 0.2s ease",
                  }}
                  onClick={() => setSelectedReason(reason)}
                >
                  <div className="card-body p-3 text-center">
                    <i
                      className={`fas ${reason.icon} mb-2 ${
                        selectedReason?.id === reason.id
                          ? "text-danger"
                          : "text-muted"
                      }`}
                      style={{ fontSize: "24px" }}
                    ></i>
                    <p className="mb-0 small fw-semibold">{reason.label}</p>
                  </div>
                  {selectedReason?.id === reason.id && (
                    <div
                      className="position-absolute top-0 end-0 m-1 badge bg-danger rounded-circle"
                      style={{ width: "20px", height: "20px" }}
                    >
                      <i
                        className="fas fa-check"
                        style={{ fontSize: "10px" }}
                      ></i>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Seçilen sebebin açıklaması */}
        {selectedReason && (
          <div
            className="alert alert-secondary mb-4"
            style={{ borderRadius: "12px" }}
          >
            <i className="fas fa-info-circle me-2"></i>
            {selectedReason.description}
          </div>
        )}

        {/* Ek Notlar */}
        <div className="mb-4">
          <label className="form-label fw-bold">
            <i className="fas fa-pencil-alt me-2 text-muted"></i>
            Ek Açıklama
          </label>
          <textarea
            className="form-control"
            rows={3}
            placeholder="Detaylı açıklama ekleyin..."
            value={additionalNotes}
            onChange={(e) => setAdditionalNotes(e.target.value)}
            style={{ borderRadius: "12px", resize: "none" }}
          />
        </div>

        {/* Fotoğraf Ekleme */}
        <div className="mb-4">
          <label className="form-label fw-bold">
            <i className="fas fa-camera me-2 text-muted"></i>
            Kanıt Fotoğrafı (Opsiyonel)
          </label>

          {photoData ? (
            <div className="position-relative d-inline-block w-100">
              <img
                src={photoData}
                alt="Kanıt"
                className="rounded shadow-sm w-100"
                style={{ maxHeight: "200px", objectFit: "cover" }}
              />
              <button
                className="btn btn-danger btn-sm position-absolute top-0 end-0 m-2 rounded-circle"
                style={{ width: "32px", height: "32px" }}
                onClick={removePhoto}
              >
                <i className="fas fa-times"></i>
              </button>
            </div>
          ) : (
            <div className="d-flex gap-2">
              <button
                className="btn btn-outline-secondary flex-grow-1"
                style={{ borderRadius: "12px", padding: "16px" }}
                onClick={startCamera}
              >
                <i className="fas fa-camera me-2"></i>
                Fotoğraf Çek
              </button>
              <button
                className="btn btn-outline-secondary"
                style={{ borderRadius: "12px", padding: "16px" }}
                onClick={() => fileInputRef.current?.click()}
              >
                <i className="fas fa-image"></i>
              </button>
              <input
                type="file"
                ref={fileInputRef}
                className="d-none"
                accept="image/*"
                onChange={handleFileSelect}
              />
            </div>
          )}
        </div>

        {/* Teslimat Denendi mi? */}
        <div className="mb-4">
          <div className="form-check form-switch">
            <input
              className="form-check-input"
              type="checkbox"
              id="attemptedDelivery"
              checked={attemptedDelivery}
              onChange={(e) => setAttemptedDelivery(e.target.checked)}
              style={{ width: "3em", height: "1.5em" }}
            />
            <label
              className="form-check-label fw-semibold"
              htmlFor="attemptedDelivery"
            >
              Teslimat denenmiş olarak işaretle
            </label>
          </div>
          <small className="text-muted">
            Adrese gittiniz ancak teslim edemediyseniz bu seçeneği işaretli
            bırakın
          </small>
        </div>
      </div>

      {/* Footer */}
      <div className="p-3 border-top bg-white">
        <div className="d-flex gap-2">
          <button
            className="btn btn-outline-secondary px-4"
            onClick={onClose}
            disabled={loading}
            style={{ borderRadius: "12px" }}
          >
            İptal
          </button>
          <button
            className="btn btn-lg flex-grow-1 text-white fw-bold"
            style={{
              background: isValid
                ? "linear-gradient(135deg, #dc3545, #c82333)"
                : "#ccc",
              border: "none",
              borderRadius: "12px",
            }}
            onClick={handleSubmit}
            disabled={!isValid || loading}
          >
            {loading ? (
              <>
                <span className="spinner-border spinner-border-sm me-2"></span>
                Gönderiliyor...
              </>
            ) : (
              <>
                <i className="fas fa-exclamation-triangle me-2"></i>
                Başarısız Olarak İşaretle
              </>
            )}
          </button>
        </div>

        <p className="text-center text-muted small mt-3 mb-0">
          <i className="fas fa-info-circle me-1"></i>
          Bu işlem geri alınamaz, dikkatli olun
        </p>
      </div>
    </div>
  );
}
