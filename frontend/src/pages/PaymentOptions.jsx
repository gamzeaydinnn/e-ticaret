import React from "react";

export default function PaymentOptions() {
  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        Ödeme Seçenekleri
      </h1>
      <p className="text-muted mb-4">
        Güvenli ödeme altyapımız ile farklı ödeme seçeneklerini kullanarak
        alışverişinizi tamamlayabilirsiniz.
      </p>
      <ul className="list-unstyled">
        <li className="mb-3">
          <strong>Kredi / Banka Kartı</strong>
          <br />
          Visa, MasterCard ve banka kartları ile online ödeme yapabilirsiniz.
        </li>
        <li className="mb-3">
          <strong>Kapıda Ödeme (varsa)</strong>
          <br />
          Bazı bölgelerde kapıda ödeme seçeneği sunulabilir. Uygunluk ödeme
          adımında görüntülenir.
        </li>
        <li className="mb-3">
          <strong>Kupon ve kampanyalar</strong>
          <br />
          İndirim kuponlarınızı ödeme adımında kullanabilirsiniz.
        </li>
      </ul>
    </div>
  );
}

