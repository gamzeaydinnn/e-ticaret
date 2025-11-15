import React from "react";

export default function About() {
  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        Hakkımızda
      </h1>
      <p className="text-muted mb-4">
        Gölköy Gurme, yerel üreticilerden özenle seçilen doğal ürünleri, butik
        bir alışveriş deneyimi ile müşterilerimize ulaştırmak amacıyla kuruldu.
        Her adımda şeffaflık, adil ticaret ve sürdürülebilir üretimi önceleyen
        bir yaklaşım benimsiyoruz.
      </p>

      <div className="row">
        <div className="col-md-6 mb-4">
          <h5 className="fw-bold mb-3">Değerlerimiz</h5>
          <ul className="text-muted ps-3 mb-0">
            <li>Yerel üreticiyi destekleyen iş modelleri</li>
            <li>Doğal, katkısız ve taze ürün politikası</li>
            <li>En yüksek müşteri memnuniyeti hedefi</li>
            <li>Çevreye duyarlı operasyon süreçleri</li>
          </ul>
        </div>
        <div className="col-md-6 mb-4">
          <h5 className="fw-bold mb-3">Hikayemiz</h5>
          <p className="text-muted mb-0">
            Bodrum Gölköy&apos;de başlayan yolculuğumuz kısa sürede tüm
            Türkiye&apos;ye yayıldı. Bugün; çiftlikten sofraya uzanan tedarik
            zincirimizi dijital kanallarla güçlendirerek aynı kaliteyi online
            mağazamızda da sunuyoruz.
          </p>
        </div>
      </div>
    </div>
  );
}
