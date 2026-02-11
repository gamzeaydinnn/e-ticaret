import React from "react";

export default function ShippingInfo() {
  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        Teslimat Bilgileri
      </h1>
      <p className="text-muted mb-4">
        Gölköy Gurme olarak siparişlerinizi kendi kuryelerimizle, ürünlerin
        tazeliğini koruyarak teslim ediyoruz.
      </p>

      <div className="row">
        <div className="col-md-6 mb-4">
          <div className="card h-100 border-0 shadow-sm">
            <div className="card-body">
              <h5 className="card-title fw-bold" style={{ color: "#f57c00" }}>
                <i className="fas fa-motorcycle me-2"></i>
                Bodrum ve Çevresi
              </h5>
              <ul className="list-unstyled mt-3">
                <li className="mb-2">
                  <i className="fas fa-check text-success me-2"></i>
                  Kendi kuryelerimizle teslimat
                </li>
                <li className="mb-2">
                  <i className="fas fa-check text-success me-2"></i>
                  Aynı gün veya ertesi gün teslimat
                </li>
                <li className="mb-2">
                  <i className="fas fa-check text-success me-2"></i>
                  Soğuk zincir ile taze teslimat
                </li>
                <li className="mb-2">
                  <i className="fas fa-clock text-warning me-2"></i>
                  Teslimat saatleri: 09:00 - 20:00
                </li>
              </ul>
            </div>
          </div>
        </div>

        <div className="col-md-6 mb-4">
          <div className="card h-100 border-0 shadow-sm">
            <div className="card-body">
              <h5 className="card-title fw-bold" style={{ color: "#f57c00" }}>
                <i className="fas fa-map-marker-alt me-2"></i>
                Teslimat Bölgeleri
              </h5>
              <ul className="list-unstyled mt-3">
                <li className="mb-2">
                  <i className="fas fa-map-pin text-info me-2"></i>
                  Gölköy ve yakın çevresi
                </li>
                <li className="mb-2">
                  <i className="fas fa-map-pin text-info me-2"></i>
                  Türk Bükü
                </li>
                <li className="mb-2">
                  <i className="fas fa-map-pin text-info me-2"></i>
                  Bodrum Merkez
                </li>
                <li className="mb-2">
                  <i className="fas fa-map-pin text-info me-2"></i>
                  Yarımada geneli
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>

      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body">
          <h5 className="card-title fw-bold" style={{ color: "#f57c00" }}>
            <i className="fas fa-truck me-2"></i>
            Teslimat Ücreti
          </h5>
          <ul className="list-unstyled mt-3 mb-0">
            <li className="mb-2">
              <i className="fas fa-gift text-success me-2"></i>
              <strong>150 TL ve üzeri siparişlerde ücretsiz teslimat</strong>
            </li>
            <li className="mb-2">
              <i className="fas fa-info-circle text-info me-2"></i>
              150 TL altı siparişlerde teslimat ücreti ödeme sayfasında
              gösterilir
            </li>
            <li className="mb-2">
              <i className="fas fa-percentage text-warning me-2"></i>
              Kampanya dönemlerinde teslimat ücretleri değişebilir
            </li>
          </ul>
        </div>
      </div>

      <div className="alert alert-warning">
        <i className="fas fa-exclamation-triangle me-2"></i>
        <strong>Önemli:</strong> Teslimat saatleri sipariş yoğunluğuna göre
        değişebilir. Kuryemiz teslimat öncesinde sizi arayacaktır.
      </div>
    </div>
  );
}
