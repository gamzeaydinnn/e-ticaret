import React from "react";

export default function Faq() {
  const items = [
    {
      q: "Siparişim ne zaman gelir?",
      a: "Teslimat süresi bölgenize göre değişmekle birlikte, Bodrum ve çevresinde aynı gün / ertesi gün teslimat yapılır.",
    },
    {
      q: "İade ve değişim şartları nelerdir?",
      a: "İade & Değişim sayfamızda detayları bulabilirsiniz. Ürün hasarlı veya bozuk ise en kısa sürede bizimle iletişime geçin.",
    },
    {
      q: "Ödeme yöntemleri nelerdir?",
      a: "Kredi / banka kartı ile online ödeme yapabilirsiniz. Kampanya dönemlerinde ek seçenekler sunulabilir.",
    },
  ];

  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        Sıkça Sorulan Sorular
      </h1>
      <p className="text-muted mb-4">
        Aşağıda kullanıcılarımızdan en sık aldığımız soruları ve yanıtlarını
        bulabilirsiniz.
      </p>
      <div className="accordion" id="faqAccordion">
        {items.map((item, idx) => (
          <div className="accordion-item" key={idx}>
            <h2 className="accordion-header" id={`heading-${idx}`}>
              <button
                className={`accordion-button ${idx !== 0 ? "collapsed" : ""}`}
                type="button"
                data-bs-toggle="collapse"
                data-bs-target={`#collapse-${idx}`}
                aria-expanded={idx === 0}
                aria-controls={`collapse-${idx}`}
              >
                {item.q}
              </button>
            </h2>
            <div
              id={`collapse-${idx}`}
              className={`accordion-collapse collapse ${
                idx === 0 ? "show" : ""
              }`}
              aria-labelledby={`heading-${idx}`}
              data-bs-parent="#faqAccordion"
            >
              <div className="accordion-body">{item.a}</div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

