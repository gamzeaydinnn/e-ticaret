import React from "react";

// ===========================================================================
// İLETİŞİM SAYFASI
// Müşteri hizmetleri iletişim bilgileri ve WhatsApp desteği
// ===========================================================================

// WhatsApp ve iletişim sabitleri
const CONTACT_INFO = {
  whatsappNumber: "905334783072",
  phoneDisplay: "+90 533 478 30 72",
  email: "golturkbuku@golkoygurme.com.tr",
  address: "Gölköy Mah. 67 Sokak No: 1/A Bodrum / Muğla",
};

// WhatsApp mesaj şablonu
const getWhatsAppUrl = (message = "Merhaba, destek almak istiyorum.") => {
  return `https://wa.me/${CONTACT_INFO.whatsappNumber}?text=${encodeURIComponent(message)}`;
};

export default function Contact() {
  // WhatsApp butonuna tıklama
  const openWhatsApp = () => {
    window.open(getWhatsAppUrl(), "_blank");
  };

  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        İletişim
      </h1>
      <p className="text-muted mb-4">
        Gölköy Gourmet Market ve Doğadan Sofranza ile ilgili her türlü soru,
        öneri ve talepleriniz için bize aşağıdaki iletişim kanallarından
        ulaşabilirsiniz.
      </p>

      {/* ================================================================
          WHATSAPP HIZLI ERİŞİM BÖLÜMÜ
          Sipariş iptal/destek için öne çıkarılmış buton
          ================================================================ */}
      <div
        className="card border-0 shadow-sm mb-4"
        style={{
          background: "linear-gradient(135deg, #25D366 0%, #128C7E 100%)",
          borderRadius: "16px",
        }}
      >
        <div className="card-body p-4">
          <div className="row align-items-center">
            <div className="col-md-8">
              <h4 className="text-white fw-bold mb-2">
                <i className="fab fa-whatsapp me-2"></i>
                WhatsApp ile Hızlı Destek
              </h4>
              <p className="text-white mb-0" style={{ opacity: 0.9 }}>
                Sipariş iptali, değişiklik veya herhangi bir sorunuz için
                WhatsApp üzerinden anında bize ulaşabilirsiniz.
              </p>
            </div>
            <div className="col-md-4 text-md-end mt-3 mt-md-0">
              <button
                onClick={openWhatsApp}
                className="btn btn-light btn-lg fw-bold"
                style={{
                  borderRadius: "25px",
                  padding: "12px 32px",
                  color: "#25D366",
                }}
              >
                <i className="fab fa-whatsapp me-2"></i>
                Mesaj Gönder
              </button>
            </div>
          </div>
        </div>
      </div>

      <div className="row">
        <div className="col-md-6 mb-4">
          <div
            className="card border-0 shadow-sm h-100"
            style={{ borderRadius: "16px" }}
          >
            <div className="card-body p-4">
              <h5 className="fw-bold mb-4">
                <i
                  className="fas fa-headset me-2"
                  style={{ color: "#ff8c00" }}
                ></i>
                Müşteri Hizmetleri
              </h5>

              {/* WhatsApp */}
              <div
                className="d-flex align-items-center mb-3 p-3 rounded"
                style={{ background: "#f0fdf4" }}
              >
                <div
                  className="d-flex align-items-center justify-content-center me-3"
                  style={{
                    width: "48px",
                    height: "48px",
                    background: "#25D366",
                    borderRadius: "12px",
                  }}
                >
                  <i
                    className="fab fa-whatsapp text-white"
                    style={{ fontSize: "24px" }}
                  ></i>
                </div>
                <div>
                  <a
                    href={getWhatsAppUrl()}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-decoration-none fw-bold d-block"
                    style={{ color: "#25D366" }}
                  >
                    WhatsApp Destek
                  </a>
                  <small className="text-muted">En hızlı yanıt için</small>
                </div>
              </div>

              {/* Telefon */}
              <div
                className="d-flex align-items-center mb-3 p-3 rounded"
                style={{ background: "#fff7ed" }}
              >
                <div
                  className="d-flex align-items-center justify-content-center me-3"
                  style={{
                    width: "48px",
                    height: "48px",
                    background: "#ff8c00",
                    borderRadius: "12px",
                  }}
                >
                  <i
                    className="fas fa-phone-alt text-white"
                    style={{ fontSize: "20px" }}
                  ></i>
                </div>
                <div>
                  <a
                    href={`tel:+${CONTACT_INFO.whatsappNumber}`}
                    className="text-decoration-none fw-bold d-block"
                    style={{ color: "#ff8c00" }}
                  >
                    {CONTACT_INFO.phoneDisplay}
                  </a>
                  <small className="text-muted">Hafta içi 09:00 - 18:00</small>
                </div>
              </div>

              {/* E-posta */}
              <div
                className="d-flex align-items-center p-3 rounded"
                style={{ background: "#eff6ff" }}
              >
                <div
                  className="d-flex align-items-center justify-content-center me-3"
                  style={{
                    width: "48px",
                    height: "48px",
                    background: "#3b82f6",
                    borderRadius: "12px",
                  }}
                >
                  <i
                    className="fas fa-envelope text-white"
                    style={{ fontSize: "20px" }}
                  ></i>
                </div>
                <div>
                  <a
                    href={`mailto:${CONTACT_INFO.email}`}
                    className="text-decoration-none fw-bold d-block"
                    style={{ color: "#3b82f6" }}
                  >
                    {CONTACT_INFO.email}
                  </a>
                  <small className="text-muted">Genel bilgi ve destek</small>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="col-md-6 mb-4">
          <div
            className="card border-0 shadow-sm h-100"
            style={{ borderRadius: "16px" }}
          >
            <div className="card-body p-4">
              <h5 className="fw-bold mb-4">
                <i
                  className="fas fa-building me-2"
                  style={{ color: "#ff8c00" }}
                ></i>
                Merkez Ofis
              </h5>

              {/* Adres */}
              <div
                className="d-flex align-items-start mb-4 p-3 rounded"
                style={{ background: "#faf5ff" }}
              >
                <div
                  className="d-flex align-items-center justify-content-center me-3"
                  style={{
                    width: "48px",
                    height: "48px",
                    background: "#8b5cf6",
                    borderRadius: "12px",
                    flexShrink: 0,
                  }}
                >
                  <i
                    className="fas fa-map-marker-alt text-white"
                    style={{ fontSize: "20px" }}
                  ></i>
                </div>
                <div>
                  <span
                    className="fw-bold d-block mb-1"
                    style={{ color: "#8b5cf6" }}
                  >
                    Adres
                  </span>
                  <span className="text-muted">{CONTACT_INFO.address}</span>
                </div>
              </div>

              {/* Çalışma Saatleri */}
              <div
                className="d-flex align-items-start p-3 rounded"
                style={{ background: "#fef3c7" }}
              >
                <div
                  className="d-flex align-items-center justify-content-center me-3"
                  style={{
                    width: "48px",
                    height: "48px",
                    background: "#f59e0b",
                    borderRadius: "12px",
                    flexShrink: 0,
                  }}
                >
                  <i
                    className="fas fa-clock text-white"
                    style={{ fontSize: "20px" }}
                  ></i>
                </div>
                <div>
                  <span
                    className="fw-bold d-block mb-1"
                    style={{ color: "#f59e0b" }}
                  >
                    Çalışma Saatleri
                  </span>
                  <span className="text-muted">
                    Pazartesi - Cumartesi: 09:00 - 20:00
                    <br />
                    Pazar: 10:00 - 18:00
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Sipariş İptal Bilgilendirmesi */}
      <div
        className="alert border-0 mt-2"
        style={{
          background: "#fff7ed",
          borderRadius: "12px",
          borderLeft: "4px solid #ff8c00",
        }}
      >
        <div className="d-flex align-items-start">
          <i
            className="fas fa-info-circle me-3 mt-1"
            style={{ color: "#ff8c00", fontSize: "20px" }}
          ></i>
          <div>
            <strong style={{ color: "#ff8c00" }}>
              Sipariş İptali Hakkında
            </strong>
            <p className="mb-0 mt-1 text-muted">
              Siparişlerinizi <strong>aynı gün içinde</strong> ve{" "}
              <strong>hazırlanmaya başlamadan önce</strong> iptal edebilirsiniz.
              Siparişiniz hazırlanmaya başladıysa veya ertesi güne kaldıysa,
              iptal için WhatsApp üzerinden bizimle iletişime geçmenizi rica
              ederiz.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
