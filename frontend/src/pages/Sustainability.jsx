import React from "react";

export default function Sustainability() {
  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        Sürdürülebilirlik
      </h1>
      <p className="text-muted mb-4">
        Doğaya duyarlı üretim ve lojistik süreçleri, Gölköy Gurme kültürünün
        en önemli parçasıdır. Tedarik zincirimizin her aşamasında izlenebilirlik
        ve çevresel etkiyi azaltma hedefiyle hareket ediyoruz.
      </p>

      <div className="row g-4">
        <div className="col-md-4">
          <div className="p-4 rounded-3 border h-100">
            <h6 className="fw-bold mb-2">Enerji</h6>
            <p className="text-muted mb-0">
              Depolarımızda yenilenebilir enerjiye geçiş ve enerji verimliliği
              yatırımlarını hızlandırıyoruz.
            </p>
          </div>
        </div>
        <div className="col-md-4">
          <div className="p-4 rounded-3 border h-100">
            <h6 className="fw-bold mb-2">Paketleme</h6>
            <p className="text-muted mb-0">
              Geri dönüştürülebilir paketleme kullanıyor, plastik kullanımını
              minimuma indiriyoruz.
            </p>
          </div>
        </div>
        <div className="col-md-4">
          <div className="p-4 rounded-3 border h-100">
            <h6 className="fw-bold mb-2">Topluluk</h6>
            <p className="text-muted mb-0">
              Yerel üreticilerle uzun vadeli iş birlikleri kurarak adil ticareti
              destekliyoruz.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
