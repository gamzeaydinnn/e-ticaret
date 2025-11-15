import React from "react";

export default function VisionMission() {
  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        Vizyon &amp; Misyon
      </h1>
      <div className="row g-4">
        <div className="col-md-6">
          <div className="p-4 rounded-3 border h-100">
            <h5 className="fw-bold mb-3">Vizyonumuz</h5>
            <p className="text-muted mb-0">
              Anadolu&apos;nun bereketli topraklarından çıkan ürünleri,
              dünyanın dört bir yanındaki sofralara ulaştıran güvenilir ve
              sürdürülebilir bir marka olmak.
            </p>
          </div>
        </div>
        <div className="col-md-6">
          <div className="p-4 rounded-3 border h-100">
            <h5 className="fw-bold mb-3">Misyonumuz</h5>
            <ul className="text-muted ps-3 mb-0">
              <li>Üretici ile tüketici arasında şeffaf bir köprü kurmak</li>
              <li>Sürekli gelişen lojistik ağıyla tazeliği korumak</li>
              <li>
                Müşteri ihtiyaçlarını veriye dayalı çözümlerle öngörmek ve
                karşılamak
              </li>
              <li>
                Doğa dostu paketleme ve operasyon süreçleriyle fark yaratmak
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
