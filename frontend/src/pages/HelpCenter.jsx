import React from "react";

export default function HelpCenter() {
  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        Yardım Merkezi
      </h1>
      <p className="text-muted mb-4">
        Sipariş, teslimat, iade ve ödeme süreçleriyle ilgili en sık sorulan
        soruları bu sayfada bulabilirsiniz. Aradığınız cevabı bulamazsanız
        İletişim sayfasından bize ulaşabilirsiniz.
      </p>
      <ul className="list-unstyled">
        <li className="mb-3">
          <strong>Sipariş nasıl verilir?</strong>
          <br />
          Ürünleri sepetinize ekledikten sonra, sepet ikonuna tıklayıp
          &quot;Ödeme Adımına Geç&quot; butonuyla siparişinizi
          tamamlayabilirsiniz.
        </li>
        <li className="mb-3">
          <strong>Teslimat süresi nedir?</strong>
          <br />
          Bodrum ve çevresine aynı gün / ertesi gün teslimat; diğer bölgeler
          için kargo firmasının teslimat süreleri geçerlidir.
        </li>
        <li className="mb-3">
          <strong>Ürünüm hasarlı geldi, ne yapmalıyım?</strong>
          <br />
          Hasarlı ürünlerle ilgili detaylı bilgi için &quot;İade &amp;
          Değişim&quot; sayfasını inceleyebilir veya bizimle iletişime
          geçebilirsiniz.
        </li>
      </ul>
    </div>
  );
}

