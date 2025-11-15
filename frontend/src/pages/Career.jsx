import React from "react";

export default function Career() {
  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        Kariyer
      </h1>
      <p className="text-muted mb-4">
        Büyüyen lojistik ve perakende operasyonlarımız için yeni ekip
        arkadaşları aramaya devam ediyoruz. Kariyer fırsatlarımız çok yakında
        bu sayfada yer alacak.
      </p>

      <div className="p-4 rounded-3 border">
        <h5 className="fw-bold mb-2">Bize Katıl</h5>
        <p className="text-muted mb-3">
          Açık pozisyonlar yayınlanana kadar özgeçmişinizi bizimle paylaşarak
          talent havuzumuza katılabilirsiniz.
        </p>
        <div className="d-flex align-items-center">
          <i className="fas fa-envelope-open-text me-2 text-warning"></i>
          <a
            href="mailto:insankaynaklari@golkoygurme.com.tr"
            className="text-decoration-none"
          >
            insankaynaklari@golkoygurme.com.tr
          </a>
        </div>
      </div>
    </div>
  );
}
