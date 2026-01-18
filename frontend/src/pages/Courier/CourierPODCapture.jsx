// ==========================================================================
// CourierPODCapture.jsx - Proof of Delivery (Teslimat Onayı) Modal
// ==========================================================================
// Teslimat onayı için fotoğraf çekme, OTP doğrulama ve imza alma.
// Gerekli onay yöntemleri task'tan alınır.
// ==========================================================================

import React, { useState, useRef, useEffect, useCallback } from "react";

// Varsayılan gerekli onay yöntemleri
const DEFAULT_PROOF_METHODS = ["Photo"];

export default function CourierPODCapture({
  task,
  onComplete,
  onClose,
  loading,
}) {
  // Gerekli yöntemler
  const requiredMethods = task?.requiredProofMethods || DEFAULT_PROOF_METHODS;

  // State
  const [activeStep, setActiveStep] = useState(0);
  const [photoData, setPhotoData] = useState(null);
  const [otpCode, setOtpCode] = useState(["", "", "", "", "", ""]);
  const [otpError, setOtpError] = useState("");
  const [signatureData, setSignatureData] = useState(null);
  const [notes, setNotes] = useState("");
  const [isDrawing, setIsDrawing] = useState(false);
  const [cameraError, setCameraError] = useState(null);
  const [cameraStream, setCameraStream] = useState(null);

  // Refs
  const videoRef = useRef(null);
  const canvasRef = useRef(null);
  const signatureCanvasRef = useRef(null);
  const otpInputRefs = useRef([]);
  const signatureContext = useRef(null);
  const lastPoint = useRef(null);

  // =========================================================================
  // KAMERA YÖNETİMİ
  // =========================================================================
  const startCamera = useCallback(async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: {
          facingMode: "environment", // Arka kamera
          width: { ideal: 1280 },
          height: { ideal: 720 },
        },
      });

      if (videoRef.current) {
        videoRef.current.srcObject = stream;
        setCameraStream(stream);
        setCameraError(null);
      }
    } catch (err) {
      console.error("Kamera erişim hatası:", err);
      setCameraError("Kamera erişimi sağlanamadı. Lütfen izin verin.");
    }
  }, []);

  const stopCamera = useCallback(() => {
    if (cameraStream) {
      cameraStream.getTracks().forEach((track) => track.stop());
      setCameraStream(null);
    }
  }, [cameraStream]);

  const capturePhoto = useCallback(() => {
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
  }, [stopCamera]);

  const retakePhoto = useCallback(() => {
    setPhotoData(null);
    startCamera();
  }, [startCamera]);

  // Kamera başlat/durdur
  useEffect(() => {
    if (
      requiredMethods.includes("Photo") &&
      activeStep === getStepIndex("Photo") &&
      !photoData
    ) {
      startCamera();
    }
    return () => stopCamera();
  }, [activeStep, photoData, requiredMethods, startCamera, stopCamera]);

  // =========================================================================
  // OTP YÖNETİMİ
  // =========================================================================
  const handleOtpChange = (index, value) => {
    if (!/^\d*$/.test(value)) return; // Sadece rakam

    const newOtp = [...otpCode];
    newOtp[index] = value;
    setOtpCode(newOtp);
    setOtpError("");

    // Otomatik sonraki inputa geç
    if (value && index < 5) {
      otpInputRefs.current[index + 1]?.focus();
    }
  };

  const handleOtpKeyDown = (index, e) => {
    if (e.key === "Backspace" && !otpCode[index] && index > 0) {
      otpInputRefs.current[index - 1]?.focus();
    }
  };

  const handleOtpPaste = (e) => {
    e.preventDefault();
    const pastedData = e.clipboardData
      .getData("text")
      .replace(/\D/g, "")
      .slice(0, 6);
    const newOtp = pastedData.split("").concat(Array(6).fill("")).slice(0, 6);
    setOtpCode(newOtp);

    // Son dolu inputa focus
    const lastFilledIndex = newOtp.findIndex((v, i) => !v || i === 5);
    otpInputRefs.current[Math.min(lastFilledIndex, 5)]?.focus();
  };

  // =========================================================================
  // İMZA YÖNETİMİ
  // =========================================================================
  useEffect(() => {
    if (
      requiredMethods.includes("Signature") &&
      activeStep === getStepIndex("Signature")
    ) {
      initSignatureCanvas();
    }
  }, [activeStep, requiredMethods]);

  const initSignatureCanvas = () => {
    const canvas = signatureCanvasRef.current;
    if (!canvas) return;

    // Canvas boyutlarını ayarla
    const rect = canvas.getBoundingClientRect();
    canvas.width = rect.width * 2;
    canvas.height = rect.height * 2;
    canvas.style.width = rect.width + "px";
    canvas.style.height = rect.height + "px";

    const ctx = canvas.getContext("2d");
    ctx.scale(2, 2);
    ctx.strokeStyle = "#000";
    ctx.lineWidth = 2;
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    signatureContext.current = ctx;

    // Arkaplan
    ctx.fillStyle = "#fff";
    ctx.fillRect(0, 0, canvas.width, canvas.height);
  };

  const getPointerPosition = (e) => {
    const canvas = signatureCanvasRef.current;
    const rect = canvas.getBoundingClientRect();
    const clientX = e.touches ? e.touches[0].clientX : e.clientX;
    const clientY = e.touches ? e.touches[0].clientY : e.clientY;
    return {
      x: clientX - rect.left,
      y: clientY - rect.top,
    };
  };

  const handleSignatureStart = (e) => {
    e.preventDefault();
    setIsDrawing(true);
    lastPoint.current = getPointerPosition(e);
  };

  const handleSignatureMove = (e) => {
    if (!isDrawing || !signatureContext.current) return;
    e.preventDefault();

    const currentPoint = getPointerPosition(e);
    const ctx = signatureContext.current;

    ctx.beginPath();
    ctx.moveTo(lastPoint.current.x, lastPoint.current.y);
    ctx.lineTo(currentPoint.x, currentPoint.y);
    ctx.stroke();

    lastPoint.current = currentPoint;
  };

  const handleSignatureEnd = () => {
    setIsDrawing(false);
    lastPoint.current = null;

    // İmza verisini kaydet
    if (signatureCanvasRef.current) {
      const data = signatureCanvasRef.current.toDataURL("image/png");
      setSignatureData(data);
    }
  };

  const clearSignature = () => {
    const canvas = signatureCanvasRef.current;
    if (canvas && signatureContext.current) {
      const ctx = signatureContext.current;
      ctx.fillStyle = "#fff";
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      setSignatureData(null);
    }
  };

  // =========================================================================
  // ADIM YÖNETİMİ
  // =========================================================================
  const getSteps = () => {
    const steps = [];
    if (requiredMethods.includes("Photo"))
      steps.push({ key: "Photo", label: "Fotoğraf", icon: "fa-camera" });
    if (requiredMethods.includes("Otp"))
      steps.push({ key: "Otp", label: "OTP Kodu", icon: "fa-key" });
    if (requiredMethods.includes("Signature"))
      steps.push({ key: "Signature", label: "İmza", icon: "fa-signature" });
    steps.push({ key: "Notes", label: "Notlar", icon: "fa-sticky-note" });
    return steps;
  };

  const getStepIndex = (key) => {
    return getSteps().findIndex((s) => s.key === key);
  };

  const steps = getSteps();
  const currentStep = steps[activeStep];
  const isLastStep = activeStep === steps.length - 1;

  const canProceed = () => {
    switch (currentStep?.key) {
      case "Photo":
        return !!photoData;
      case "Otp":
        return otpCode.every((d) => d !== "");
      case "Signature":
        return !!signatureData;
      case "Notes":
        return true;
      default:
        return true;
    }
  };

  const handleNext = () => {
    if (isLastStep) {
      // Tamamla
      onComplete({
        photoBase64: photoData?.split(",")[1],
        otpCode: otpCode.join(""),
        signatureBase64: signatureData?.split(",")[1],
        notes: notes.trim(),
        timestamp: new Date().toISOString(),
      });
    } else {
      setActiveStep((prev) => prev + 1);
    }
  };

  const handleBack = () => {
    if (activeStep > 0) {
      setActiveStep((prev) => prev - 1);
    }
  };

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
        className="d-flex align-items-center justify-content-between p-3 border-bottom"
        style={{
          background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
        }}
      >
        <button className="btn btn-link text-white p-0" onClick={onClose}>
          <i className="fas fa-times fs-5"></i>
        </button>
        <h6 className="text-white mb-0 fw-bold">Teslimat Onayı</h6>
        <div style={{ width: "24px" }}></div>
      </div>

      {/* Progress Steps */}
      <div className="bg-light p-3 border-bottom">
        <div className="d-flex justify-content-center gap-2">
          {steps.map((step, index) => (
            <div
              key={step.key}
              className="text-center"
              style={{ flex: "0 0 auto", minWidth: "60px" }}
            >
              <div
                className={`rounded-circle mx-auto mb-1 d-flex align-items-center justify-content-center ${
                  index < activeStep
                    ? "bg-success"
                    : index === activeStep
                      ? "bg-primary"
                      : "bg-secondary bg-opacity-25"
                }`}
                style={{ width: "32px", height: "32px" }}
              >
                {index < activeStep ? (
                  <i
                    className="fas fa-check text-white"
                    style={{ fontSize: "12px" }}
                  ></i>
                ) : (
                  <i
                    className={`fas ${step.icon} ${index === activeStep ? "text-white" : "text-muted"}`}
                    style={{ fontSize: "12px" }}
                  ></i>
                )}
              </div>
              <small
                className={`${index === activeStep ? "text-primary fw-bold" : "text-muted"}`}
                style={{ fontSize: "10px" }}
              >
                {step.label}
              </small>
            </div>
          ))}
        </div>
      </div>

      {/* Content */}
      <div className="flex-grow-1 overflow-auto p-3">
        {/* FOTOĞRAF ADIMI */}
        {currentStep?.key === "Photo" && (
          <div className="h-100 d-flex flex-column">
            {photoData ? (
              // Çekilen fotoğraf
              <div className="flex-grow-1 d-flex flex-column align-items-center justify-content-center">
                <div
                  className="position-relative mb-3"
                  style={{ maxWidth: "100%" }}
                >
                  <img
                    src={photoData}
                    alt="Teslimat fotoğrafı"
                    className="rounded shadow"
                    style={{
                      maxWidth: "100%",
                      maxHeight: "400px",
                      objectFit: "contain",
                    }}
                  />
                  <div className="position-absolute top-0 end-0 m-2">
                    <span className="badge bg-success">
                      <i className="fas fa-check me-1"></i>
                      Fotoğraf alındı
                    </span>
                  </div>
                </div>
                <button
                  className="btn btn-outline-secondary"
                  onClick={retakePhoto}
                >
                  <i className="fas fa-redo me-2"></i>
                  Tekrar Çek
                </button>
              </div>
            ) : (
              // Kamera görüntüsü
              <div className="flex-grow-1 d-flex flex-column">
                {cameraError ? (
                  <div className="flex-grow-1 d-flex flex-column align-items-center justify-content-center text-center p-4">
                    <i
                      className="fas fa-camera-slash text-muted mb-3"
                      style={{ fontSize: "64px" }}
                    ></i>
                    <p className="text-danger mb-3">{cameraError}</p>
                    <button className="btn btn-primary" onClick={startCamera}>
                      <i className="fas fa-redo me-2"></i>
                      Tekrar Dene
                    </button>
                  </div>
                ) : (
                  <>
                    <div
                      className="flex-grow-1 position-relative bg-dark rounded overflow-hidden"
                      style={{ minHeight: "300px" }}
                    >
                      <video
                        ref={videoRef}
                        autoPlay
                        playsInline
                        muted
                        className="w-100 h-100"
                        style={{ objectFit: "cover" }}
                      />
                      {/* Çerçeve overlay */}
                      <div
                        className="position-absolute top-50 start-50 translate-middle border border-white border-2 rounded"
                        style={{ width: "80%", height: "60%", opacity: 0.5 }}
                      ></div>
                    </div>
                    <canvas ref={canvasRef} className="d-none"></canvas>
                    <button
                      className="btn btn-lg w-100 text-white mt-3"
                      style={{
                        background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                        borderRadius: "12px",
                        padding: "16px",
                      }}
                      onClick={capturePhoto}
                    >
                      <i className="fas fa-camera me-2"></i>
                      Fotoğraf Çek
                    </button>
                  </>
                )}
              </div>
            )}
          </div>
        )}

        {/* OTP ADIMI */}
        {currentStep?.key === "Otp" && (
          <div className="text-center py-4">
            <div className="mb-4">
              <div
                className="rounded-circle mx-auto mb-3 d-flex align-items-center justify-content-center"
                style={{
                  width: "80px",
                  height: "80px",
                  backgroundColor: "#fff3e0",
                }}
              >
                <i
                  className="fas fa-key"
                  style={{ fontSize: "32px", color: "#ff6b35" }}
                ></i>
              </div>
              <h5 className="fw-bold">OTP Doğrulama</h5>
              <p className="text-muted">
                Müşteriden aldığınız 6 haneli kodu girin
              </p>
            </div>

            {/* OTP Input */}
            <div className="d-flex justify-content-center gap-2 mb-3">
              {otpCode.map((digit, index) => (
                <input
                  key={index}
                  ref={(el) => (otpInputRefs.current[index] = el)}
                  type="text"
                  inputMode="numeric"
                  maxLength={1}
                  className="form-control text-center fw-bold"
                  style={{
                    width: "48px",
                    height: "56px",
                    fontSize: "24px",
                    borderRadius: "12px",
                    border: otpError
                      ? "2px solid #dc3545"
                      : "2px solid #e9ecef",
                  }}
                  value={digit}
                  onChange={(e) => handleOtpChange(index, e.target.value)}
                  onKeyDown={(e) => handleOtpKeyDown(index, e)}
                  onPaste={handleOtpPaste}
                />
              ))}
            </div>

            {otpError && (
              <p className="text-danger small mb-3">
                <i className="fas fa-exclamation-circle me-1"></i>
                {otpError}
              </p>
            )}

            <p className="text-muted small">
              <i className="fas fa-info-circle me-1"></i>
              Müşterinin telefonuna gönderilen kodu isteyin
            </p>
          </div>
        )}

        {/* İMZA ADIMI */}
        {currentStep?.key === "Signature" && (
          <div className="d-flex flex-column h-100">
            <div className="text-center mb-3">
              <h5 className="fw-bold">Müşteri İmzası</h5>
              <p className="text-muted small mb-0">
                Müşteriden aşağıdaki alana imza atmasını isteyin
              </p>
            </div>

            {/* Signature Canvas */}
            <div
              className="flex-grow-1 border rounded position-relative bg-white"
              style={{ minHeight: "200px", touchAction: "none" }}
            >
              <canvas
                ref={signatureCanvasRef}
                className="w-100 h-100"
                style={{ cursor: "crosshair" }}
                onMouseDown={handleSignatureStart}
                onMouseMove={handleSignatureMove}
                onMouseUp={handleSignatureEnd}
                onMouseLeave={handleSignatureEnd}
                onTouchStart={handleSignatureStart}
                onTouchMove={handleSignatureMove}
                onTouchEnd={handleSignatureEnd}
              />
              {!signatureData && (
                <div
                  className="position-absolute top-50 start-50 translate-middle text-muted"
                  style={{ pointerEvents: "none" }}
                >
                  <i className="fas fa-signature me-2"></i>
                  Buraya imza atın
                </div>
              )}
            </div>

            <button
              className="btn btn-outline-secondary mt-3"
              onClick={clearSignature}
            >
              <i className="fas fa-eraser me-2"></i>
              Temizle
            </button>
          </div>
        )}

        {/* NOTLAR ADIMI */}
        {currentStep?.key === "Notes" && (
          <div>
            <div className="text-center mb-4">
              <div
                className="rounded-circle mx-auto mb-3 d-flex align-items-center justify-content-center"
                style={{
                  width: "80px",
                  height: "80px",
                  backgroundColor: "#e8f5e9",
                }}
              >
                <i
                  className="fas fa-check-circle text-success"
                  style={{ fontSize: "40px" }}
                ></i>
              </div>
              <h5 className="fw-bold">Neredeyse Tamamlandı!</h5>
              <p className="text-muted">
                İsterseniz teslimatla ilgili not ekleyebilirsiniz
              </p>
            </div>

            <div className="mb-3">
              <label className="form-label fw-semibold">
                <i className="fas fa-sticky-note me-2 text-muted"></i>
                Teslimat Notu (Opsiyonel)
              </label>
              <textarea
                className="form-control"
                rows={4}
                placeholder="Örn: Kapıya bırakıldı, komşuya teslim edildi..."
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                style={{ borderRadius: "12px", resize: "none" }}
              />
            </div>

            {/* Özet */}
            <div className="bg-light rounded p-3">
              <h6 className="fw-bold mb-3">
                <i className="fas fa-clipboard-check me-2"></i>
                Teslimat Özeti
              </h6>
              <div className="d-flex flex-wrap gap-2">
                {photoData && (
                  <span className="badge bg-success">
                    <i className="fas fa-check me-1"></i>
                    Fotoğraf
                  </span>
                )}
                {otpCode.every((d) => d) && requiredMethods.includes("Otp") && (
                  <span className="badge bg-success">
                    <i className="fas fa-check me-1"></i>
                    OTP
                  </span>
                )}
                {signatureData && (
                  <span className="badge bg-success">
                    <i className="fas fa-check me-1"></i>
                    İmza
                  </span>
                )}
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Footer Actions */}
      <div className="p-3 border-top bg-white">
        <div className="d-flex gap-2">
          {activeStep > 0 && (
            <button
              className="btn btn-outline-secondary px-4"
              onClick={handleBack}
              disabled={loading}
              style={{ borderRadius: "12px" }}
            >
              <i className="fas fa-arrow-left me-2"></i>
              Geri
            </button>
          )}
          <button
            className="btn btn-lg flex-grow-1 text-white fw-bold"
            style={{
              background: canProceed()
                ? "linear-gradient(135deg, #ff6b35, #ff8c00)"
                : "#ccc",
              border: "none",
              borderRadius: "12px",
            }}
            onClick={handleNext}
            disabled={!canProceed() || loading}
          >
            {loading ? (
              <>
                <span className="spinner-border spinner-border-sm me-2"></span>
                Gönderiliyor...
              </>
            ) : isLastStep ? (
              <>
                <i className="fas fa-check-circle me-2"></i>
                Teslimatı Onayla
              </>
            ) : (
              <>
                Devam Et
                <i className="fas fa-arrow-right ms-2"></i>
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
