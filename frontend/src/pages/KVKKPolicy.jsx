import React from "react";
import { Helmet } from "react-helmet-async";

export default function KVKKPolicy() {
  return (
    <div className="container py-5">
      <Helmet>
        <title>KVKK Aydınlatma Metni — Gölköy Gurme</title>
        <meta
          name="description"
          content="Gölköy Gurme KVKK (Kişisel Verilerin Korunması Kanunu) kapsamında aydınlatma metni."
        />
      </Helmet>

      <h1 className="h3 fw-bold mb-4" style={{ color: "#2d3748" }}>
        <i className="fas fa-user-shield me-2" style={{ color: "#f57c00" }}></i>
        KVKK Aydınlatma Metni
      </h1>

      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <p className="text-muted mb-3">
            <strong>Son Güncelleme:</strong> 01.01.2026
          </p>
          <p className="text-muted">
            Gölköy Gurme Market olarak, 6698 sayılı Kişisel Verilerin Korunması
            Kanunu (KVKK) kapsamında, veri sorumlusu sıfatıyla kişisel
            verilerinizin işlenmesine ilişkin sizleri bilgilendirmek amacıyla bu
            aydınlatma metnini hazırladık.
          </p>
        </div>
      </div>

      {/* 1. Veri Sorumlusu */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            1. Veri Sorumlusu
          </h5>
          <p className="text-muted">
            6698 sayılı Kanun uyarınca kişisel verileriniz; veri sorumlusu
            olarak Gölköy Gurme Market tarafından aşağıda açıklanan kapsamda
            işlenebilecektir.
          </p>
          <ul className="text-muted ps-3 mb-0">
            <li className="mb-2">
              <strong>Unvan:</strong> Gölköy Gurme Market
            </li>
            <li className="mb-2">
              <strong>Adres:</strong> Gölköy Mah. Bodrum / Muğla
            </li>
            <li className="mb-2">
              <strong>E-posta:</strong>{" "}
              <a
                href="mailto:golturkbuku@golkoygurme.com.tr"
                style={{ color: "#f57c00" }}
              >
                golturkbuku@golkoygurme.com.tr
              </a>
            </li>
          </ul>
        </div>
      </div>

      {/* 2. İşlenen Kişisel Veriler */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            2. İşlenen Kişisel Veri Kategorileri
          </h5>
          <div className="table-responsive">
            <table className="table table-bordered">
              <thead style={{ backgroundColor: "#f8f9fa" }}>
                <tr>
                  <th style={{ color: "#2d3748" }}>Veri Kategorisi</th>
                  <th style={{ color: "#2d3748" }}>Açıklama</th>
                </tr>
              </thead>
              <tbody className="text-muted">
                <tr>
                  <td>
                    <strong>Kimlik Bilgileri</strong>
                  </td>
                  <td>Ad, soyad</td>
                </tr>
                <tr>
                  <td>
                    <strong>İletişim Bilgileri</strong>
                  </td>
                  <td>E-posta, telefon numarası, adres</td>
                </tr>
                <tr>
                  <td>
                    <strong>Müşteri İşlem Bilgileri</strong>
                  </td>
                  <td>
                    Sipariş bilgileri, fatura bilgileri, teslimat bilgileri
                  </td>
                </tr>
                <tr>
                  <td>
                    <strong>Finansal Bilgiler</strong>
                  </td>
                  <td>
                    Ödeme yöntemi bilgisi (kart numaraları tarafımızca
                    saklanmaz, ödeme kuruluşu tarafından işlenir)
                  </td>
                </tr>
                <tr>
                  <td>
                    <strong>İşlem Güvenliği Bilgileri</strong>
                  </td>
                  <td>
                    IP adresi, oturum bilgileri, şifre (şifrelenmiş olarak)
                  </td>
                </tr>
                <tr>
                  <td>
                    <strong>Pazarlama Bilgileri</strong>
                  </td>
                  <td>Alışveriş geçmişi, ürün tercihleri, çerez verileri</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* 3. İşleme Amaçları */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            3. Kişisel Verilerin İşlenme Amaçları
          </h5>
          <p className="text-muted mb-3">
            Kişisel verileriniz KVKK'nın 5. ve 6. maddelerinde belirtilen işleme
            şartlarına uygun olarak aşağıdaki amaçlarla işlenmektedir:
          </p>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              Mal / hizmet satış süreçlerinin yürütülmesi
            </li>
            <li className="mb-2">
              Mal / hizmet satış sonrası destek hizmetlerinin yürütülmesi
            </li>
            <li className="mb-2">
              Müşteri ilişkileri yönetimi süreçlerinin yürütülmesi
            </li>
            <li className="mb-2">Sipariş ve teslimat süreçlerinin takibi</li>
            <li className="mb-2">Fatura ve ödeme süreçlerinin yönetimi</li>
            <li className="mb-2">İletişim faaliyetlerinin yürütülmesi</li>
            <li className="mb-2">
              Yetkili kuruluşlara mevzuattan kaynaklı bilgi verilmesi
            </li>
            <li className="mb-2">
              Pazarlama ve analiz çalışmalarının yürütülmesi (açık rızanız ile)
            </li>
          </ul>
        </div>
      </div>

      {/* 4. Hukuki Sebepler */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            4. Kişisel Verilerin İşlenmesinin Hukuki Sebepleri
          </h5>
          <p className="text-muted mb-3">
            Kişisel verileriniz, KVKK'nın 5. maddesinin 2. fıkrasında yer alan
            aşağıdaki hukuki sebeplere dayanılarak işlenmektedir:
          </p>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              Bir sözleşmenin kurulması veya ifasıyla doğrudan ilgili olması
            </li>
            <li className="mb-2">
              Hukuki yükümlülüğün yerine getirilmesi için zorunlu olması
            </li>
            <li className="mb-2">
              Meşru menfaatimiz için veri işlenmesinin zorunlu olması
            </li>
            <li className="mb-2">
              Açık rızanızın bulunması (pazarlama faaliyetleri için)
            </li>
          </ul>
        </div>
      </div>

      {/* 5. Aktarım */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            5. Kişisel Verilerin Aktarılması
          </h5>
          <p className="text-muted mb-3">
            Kişisel verileriniz, yukarıda belirtilen amaçların
            gerçekleştirilmesi doğrultusunda aşağıdaki taraflara aktarılabilir:
          </p>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              Teslimat süreçlerinin yürütülmesi amacıyla kurye ekibimize
            </li>
            <li className="mb-2">
              Ödeme işlemlerinin gerçekleştirilmesi amacıyla ödeme kuruluşlarına
            </li>
            <li className="mb-2">
              Yasal yükümlülükler kapsamında yetkili kamu kurum ve kuruluşlarına
            </li>
            <li className="mb-2">
              Mevzuatın öngördüğü durumlarda denetim organlarına
            </li>
          </ul>
        </div>
      </div>

      {/* 6. Saklama Süresi */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            6. Kişisel Verilerin Saklanma Süresi
          </h5>
          <p className="text-muted">
            Kişisel verileriniz, işlenme amaçlarının gerektirdiği süre boyunca
            ve ilgili mevzuatta öngörülen zamanaşımı süreleri boyunca saklanır.
            Bu sürelerin sona ermesiyle birlikte kişisel verileriniz silinir,
            yok edilir veya anonim hale getirilir.
          </p>
        </div>
      </div>

      {/* 7. İlgili Kişi Hakları */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            7. İlgili Kişi Olarak Haklarınız
          </h5>
          <p className="text-muted mb-3">
            KVKK'nın 11. maddesi gereğince aşağıdaki haklarınızı
            kullanabilirsiniz:
          </p>
          <ul className="text-muted ps-3">
            <li className="mb-2">
              Kişisel verilerinizin işlenip işlenmediğini öğrenme
            </li>
            <li className="mb-2">
              Kişisel verileriniz işlenmişse buna ilişkin bilgi talep etme
            </li>
            <li className="mb-2">
              Kişisel verilerinizin işlenme amacını ve amacına uygun kullanılıp
              kullanılmadığını öğrenme
            </li>
            <li className="mb-2">
              Kişisel verilerinizin yurt içinde veya yurt dışında aktarıldığı
              üçüncü kişileri bilme
            </li>
            <li className="mb-2">
              Kişisel verilerinizin eksik veya yanlış işlenmiş olması halinde
              düzeltilmesini isteme
            </li>
            <li className="mb-2">
              KVKK'nın 7. maddesindeki şartlar çerçevesinde kişisel
              verilerinizin silinmesini veya yok edilmesini isteme
            </li>
            <li className="mb-2">
              Düzeltme, silme ve yok etme işlemlerinin kişisel verilerinizin
              aktarıldığı üçüncü kişilere bildirilmesini isteme
            </li>
            <li className="mb-2">
              İşlenen verilerin münhasıran otomatik sistemler vasıtasıyla analiz
              edilmesi suretiyle aleyhinize bir sonuç ortaya çıkmasına itiraz
              etme
            </li>
            <li className="mb-2">
              KVKK'ya aykırı olarak işlenmesi sebebiyle zarara uğramanız halinde
              zararın giderilmesini talep etme
            </li>
          </ul>
        </div>
      </div>

      {/* 8. Başvuru */}
      <div className="card border-0 shadow-sm mb-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3" style={{ color: "#2d3748" }}>
            8. Başvuru Yöntemi
          </h5>
          <p className="text-muted mb-3">
            Yukarıda belirtilen haklarınızı kullanmak için aşağıdaki yöntemlerle
            tarafımıza başvurabilirsiniz:
          </p>
          <div className="table-responsive">
            <table className="table table-bordered">
              <thead style={{ backgroundColor: "#f8f9fa" }}>
                <tr>
                  <th style={{ color: "#2d3748" }}>Başvuru Yöntemi</th>
                  <th style={{ color: "#2d3748" }}>Adres / Bilgi</th>
                </tr>
              </thead>
              <tbody className="text-muted">
                <tr>
                  <td>
                    <strong>E-posta</strong>
                  </td>
                  <td>
                    <a
                      href="mailto:golturkbuku@golkoygurme.com.tr"
                      style={{ color: "#f57c00" }}
                    >
                      golturkbuku@golkoygurme.com.tr
                    </a>
                    <br />
                    <small>(Konu satırına "KVKK Bilgi Talebi" yazınız)</small>
                  </td>
                </tr>
                <tr>
                  <td>
                    <strong>Yazılı Başvuru</strong>
                  </td>
                  <td>
                    Gölköy Mah. Bodrum / Muğla
                    <br />
                    <small>
                      (Islak imzalı dilekçe ile elden veya noter aracılığıyla)
                    </small>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <p className="text-muted mt-3 mb-0">
            Başvurunuz en geç 30 (otuz) gün içinde sonuçlandırılacaktır.
          </p>
        </div>
      </div>
    </div>
  );
}
