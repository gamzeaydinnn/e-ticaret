import React from "react";

export default function Contact() {
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

      <div className="row">
        <div className="col-md-6 mb-4">
          <h5 className="fw-bold mb-3">Müşteri Hizmetleri</h5>
          <p className="mb-2">
            <i
              className="fas fa-phone-alt me-2"
              style={{ color: "#ff8c00" }}
            ></i>
            <a href="tel:+905334783072" className="text-decoration-none">
              +90 533 478 30 72
            </a>
          </p>
          <p className="text-muted mb-4">Müşteri Hizmetleri</p>

          <p className="mb-2">
            <i
              className="fas fa-envelope me-2"
              style={{ color: "#ff8c00" }}
            ></i>
            <a
              href="mailto:golturkbuku@golkoygurme.com.tr"
              className="text-decoration-none"
            >
              golturkbuku@golkoygurme.com.tr
            </a>
          </p>
          <p className="text-muted mb-0">Genel bilgi ve destek</p>
        </div>

        <div className="col-md-6 mb-4">
          <h5 className="fw-bold mb-3">Merkez Ofis</h5>
          <p className="mb-2">
            <i
              className="fas fa-map-marker-alt me-2"
              style={{ color: "#ff8c00" }}
            ></i>
            Gölköy Mah. 67 Sokak
            <br />
            No: 1/A Bodrum / Muğla
          </p>
          <p className="text-muted mb-4">Merkez Ofis</p>
        </div>
      </div>
    </div>
  );
}

