import React from "react";

export default function Returns() {
  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        İade &amp; Değişim
      </h1>
      <p className="text-muted mb-4">
        Gölköy Gurme olarak müşteri memnuniyetini ön planda tutuyoruz.
        Siparişinizle ilgili herhangi bir sorun yaşamanız durumunda aşağıdaki
        koşullar dahilinde iade ve değişim yapabilirsiniz.
      </p>

      <div className="mb-4">
        <h5 className="fw-bold mb-2" style={{ color: "#f57c00" }}>
          <i className="fas fa-apple-alt me-2"></i>
          Taze Ürünler (Meyve, Sebze, Et, Süt Ürünleri)
        </h5>
        <ul className="list-unstyled ps-4">
          <li className="mb-2">
            <i className="fas fa-check text-success me-2"></i>
            Teslimat sırasında ürünleri kontrol ediniz
          </li>
          <li className="mb-2">
            <i className="fas fa-check text-success me-2"></i>
            Bozuk, ezik veya hasarlı ürünleri kuryeye iade edebilirsiniz
          </li>
          <li className="mb-2">
            <i className="fas fa-check text-success me-2"></i>
            Teslimat sonrası fark edilen sorunlar için aynı gün içinde bizimle
            iletişime geçiniz
          </li>
          <li className="mb-2">
            <i className="fas fa-times text-danger me-2"></i>
            Taze ürünlerde müşteri kaynaklı iade kabul edilmemektedir
          </li>
        </ul>
      </div>

      <div className="mb-4">
        <h5 className="fw-bold mb-2" style={{ color: "#f57c00" }}>
          <i className="fas fa-box me-2"></i>
          Paketli Ürünler (Temel Gıda, Temizlik, İçecek)
        </h5>
        <ul className="list-unstyled ps-4">
          <li className="mb-2">
            <i className="fas fa-check text-success me-2"></i>
            Ambalajı açılmamış, hasarsız ürünler 7 gün içinde iade edilebilir
          </li>
          <li className="mb-2">
            <i className="fas fa-check text-success me-2"></i>
            Ürünün son kullanma tarihi geçmemiş olmalıdır
          </li>
          <li className="mb-2">
            <i className="fas fa-check text-success me-2"></i>
            Kuryemiz bir sonraki siparişinizde iade ürünü alacaktır
          </li>
        </ul>
      </div>

      <div className="mb-4">
        <h5 className="fw-bold mb-2" style={{ color: "#f57c00" }}>
          <i className="fas fa-phone-alt me-2"></i>
          İade/Değişim İçin İletişim
        </h5>
        <p className="ps-4">
          WhatsApp veya telefon yoluyla müşteri hizmetlerimize ulaşarak sipariş
          numaranızı ve sorunu bildirmeniz yeterlidir. Kuryemiz en kısa sürede
          gerekli işlemi gerçekleştirecektir.
        </p>
      </div>

      <div className="alert alert-info">
        <i className="fas fa-info-circle me-2"></i>
        <strong>Not:</strong> İade koşullarını sağlayan ürünler için ödemeniz
        aynı ödeme yöntemi ile iade edilir veya bir sonraki siparişinizden
        düşülür.
      </div>
    </div>
  );
}
