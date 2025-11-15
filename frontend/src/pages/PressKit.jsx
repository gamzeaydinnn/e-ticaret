import React from "react";

export default function PressKit() {
  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        Basın Kiti
      </h1>
      <p className="text-muted mb-4">
        Medya ve iş ortaklarımız için kurumsal hikayemizi, logolarımızı ve basın
        bültenlerimizi düzenli olarak güncelliyoruz. Basın kitine erişim veya
        röportaj talepleriniz için bizimle iletişime geçebilirsiniz.
      </p>

      <div className="row g-4">
        <div className="col-md-6">
          <div className="p-4 rounded-3 border h-100">
            <h5 className="fw-bold mb-3">Basın İletişimi</h5>
            <p className="mb-2 text-muted">İdol Media Basın Merkezi</p>
            <a
              href="mailto:basin@golkoygurme.com.tr"
              className="text-decoration-none"
            >
              basin@golkoygurme.com.tr
            </a>
          </div>
        </div>
        <div className="col-md-6">
          <div className="p-4 rounded-3 border h-100">
            <h5 className="fw-bold mb-3">İçerik Talebi</h5>
            <p className="text-muted mb-3">
              Logolar, ürün görselleri, hikayemiz ve başarı hikayeleri için
              talep formunu doldurabilirsiniz.
            </p>
            <a
              href="mailto:medya@golkoygurme.com.tr"
              className="btn btn-outline-warning"
            >
              Talep Gönder
            </a>
          </div>
        </div>
      </div>
    </div>
  );
}
