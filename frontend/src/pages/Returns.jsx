import React from "react";

export default function Returns() {
  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        İade &amp; Değişim
      </h1>
      <p className="text-muted mb-4">
        Memnun kalmadığınız ürünler için kolay iade ve değişim imkanı sunuyoruz.
        Aşağıdaki adımları izleyerek talebinizi bize iletebilirsiniz.
      </p>
      <ul className="list-unstyled">
        <li className="mb-3">
          <strong>1. Talep oluşturun</strong>
          <br />
          Sipariş numaranız ile birlikte iade/değişim talebinizi İletişim
          sayfasından veya müşteri hizmetlerimizden iletebilirsiniz.
        </li>
        <li className="mb-3">
          <strong>2. Ürünü hazırlayın</strong>
          <br />
          Mümkünse orijinal ambalajı ve faturası ile birlikte ürünü teslimata
          hazır hale getirin.
        </li>
        <li className="mb-3">
          <strong>3. İnceleme ve sonuç</strong>
          <br />
          Ürün tarafımıza ulaştıktan sonra gerekli kontroller yapılır ve iade /
          değişim süreci hakkında sizi bilgilendiririz.
        </li>
      </ul>
    </div>
  );
}

