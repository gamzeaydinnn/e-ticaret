import React from "react";

export default function ShippingInfo() {
  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        Kargo Bilgileri
      </h1>
      <p className="text-muted mb-4">
        Siparişleriniz en kısa sürede, ürünlerin tazeliği korunarak teslim
        edilmektedir. Teslimat bölgeleri ve süreleri aşağıdaki gibidir.
      </p>
      <ul className="list-unstyled">
        <li className="mb-3">
          <strong>Bodrum ve çevresi</strong>
          <br />
          Aynı gün / ertesi gün teslimat. Teslimat saatleri sipariş yoğunluğuna
          göre değişebilir.
        </li>
        <li className="mb-3">
          <strong>Diğer bölgeler</strong>
          <br />
          Anlaşmalı kargo firmaları aracılığıyla gönderim yapılır. Kargo
          firmasının teslimat süresi geçerlidir.
        </li>
        <li className="mb-3">
          <strong>Kargo ücreti</strong>
          <br />
          Kampanya ve sepet tutarına göre değişebilir. Güncel koşullar ödeme
          sayfasında gösterilir.
        </li>
      </ul>
    </div>
  );
}

