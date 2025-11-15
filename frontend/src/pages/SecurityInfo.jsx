import React from "react";

export default function SecurityInfo() {
  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        Güvenli Alışveriş
      </h1>
      <p className="text-muted mb-4">
        Kişisel verileriniz ve ödeme bilgileriniz güvenli bağlantı (HTTPS) ve
        güncel güvenlik standartları ile korunmaktadır.
      </p>
      <ul className="list-unstyled">
        <li className="mb-3">
          <strong>SSL / TLS şifreleme</strong>
          <br />
          Sitemizde yaptığınız tüm işlemler SSL / TLS ile şifrelenmektedir.
        </li>
        <li className="mb-3">
          <strong>Kart bilgileri</strong>
          <br />
          Kart bilgileriniz sistemlerimizde saklanmaz; ödeme sağlayıcısı
          üzerinden güvenli şekilde işlenir.
        </li>
        <li className="mb-3">
          <strong>Kişisel verilerin korunması</strong>
          <br />
          KVKK kapsamında kişisel verileriniz yalnızca sipariş ve müşteri
          desteği süreçlerinde kullanılır.
        </li>
      </ul>
    </div>
  );
}

