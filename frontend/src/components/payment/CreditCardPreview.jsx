// ═══════════════════════════════════════════════════════════════════════════════════════════════
// KREDİ KARTI ÖNİZLEME KOMPONENTİ
// Kullanıcının girdiği kart bilgilerini gerçek zamanlı olarak görsel bir kart üzerinde gösterir
// Responsive ve mobil uyumlu tasarım
// ═══════════════════════════════════════════════════════════════════════════════════════════════

import React, { useMemo } from "react";
import PropTypes from "prop-types";
import "./CreditCardPreview.css";

// ═══════════════════════════════════════════════════════════════════════════
// KART MARKA LOGOLARI (SVG)
// ═══════════════════════════════════════════════════════════════════════════
const CardLogos = {
  visa: (
    <svg
      viewBox="0 0 256 83"
      xmlns="http://www.w3.org/2000/svg"
      className="card-logo-svg"
    >
      <defs>
        <linearGradient
          y2="100%"
          y1="-2.006%"
          x2="54.877%"
          x1="45.974%"
          id="visaGradient"
        >
          <stop stopColor="#222357" offset="0%" />
          <stop stopColor="#254AA5" offset="100%" />
        </linearGradient>
      </defs>
      <path
        transform="matrix(1 0 0 -1 0 82.668)"
        d="M132.397 56.24c-.146-11.516 10.263-17.942 18.104-21.763c8.056-3.92 10.762-6.434 10.73-9.94c-.06-5.365-6.426-7.733-12.383-7.825c-10.393-.161-16.436 2.806-21.24 5.05l-3.744-17.519c4.82-2.221 13.745-4.158 23-4.243c21.725 0 35.938 10.724 36.015 27.351c.085 21.102-29.188 22.27-28.988 31.702c.069 2.86 2.798 5.912 8.778 6.688c2.96.392 11.131.692 20.395-3.574l3.636 16.95c-4.982 1.814-11.385 3.551-19.357 3.551c-20.448 0-34.83-10.87-34.946-26.428m89.241 24.968c-3.967 0-7.31-2.314-8.802-5.865L181.803 1.245h21.709l4.32 11.939h26.528l2.506-11.939H256l-16.697 79.963h-17.665m3.037-21.601l6.265-30.027h-17.158l10.893 30.027m-118.599 21.6L88.964 1.246h20.687l17.104 79.963h-20.679m-30.603 0L53.941 26.782l-8.71 46.277c-1.022 5.166-5.058 8.149-9.54 8.149H.493L0 78.886c7.226-1.568 15.436-4.097 20.41-6.803c3.044-1.653 3.912-3.098 4.912-7.026L41.819 1.245H63.68l33.516 79.963H75.473"
        fill="url(#visaGradient)"
      />
    </svg>
  ),
  mastercard: (
    <svg
      viewBox="0 0 131.39 86.9"
      xmlns="http://www.w3.org/2000/svg"
      className="card-logo-svg"
    >
      <circle cx="43.45" cy="43.45" r="43.45" fill="#eb001b" />
      <circle cx="87.94" cy="43.45" r="43.45" fill="#f79e1b" />
      <path
        d="M65.7 11.39a43.36 43.36 0 000 64.13 43.36 43.36 0 000-64.13z"
        fill="#ff5f00"
      />
    </svg>
  ),
  amex: (
    <svg
      viewBox="0 0 750 471"
      xmlns="http://www.w3.org/2000/svg"
      className="card-logo-svg"
    >
      <path d="M0 0h750v471H0z" fill="#2557D6" />
      <path
        d="M.253 221.169L26.055 163h20.996l14.924 34.012V163h26.047l4.5 15.932L97.476 163h131.789v8.014c0 2.461 1.966 4.459 4.391 4.459h24.68v-12.473h26.266v12.473h88.643V163H489.5l11.074 13.018L512.032 163h56.795l-43.2 51.007v-8.84H494.98l-7.266 9.457 7.266 9.176h30.647v-8.84h43.2l43.2-51.007h27.18v58.085L653.7 163h25.83l26.014 58.169h-25.174l-4.717-11.43h-22.377l-4.717 11.43h-36.283l.032-58.168-27.088 58.168h-17.48L540.492 163v58.169h-43.295l-7.236-8.963-7.517 8.963h-98.686v-58.168h-73.95v58.168h-36.284l-4.686-11.43h-22.361l-4.717 11.43H215.09c-3.748 0-6.78-3.054-6.78-6.823V163h-24.076l-16.846 38.41L150.44 163h-23.984v51.346L104.32 163H81.03l-26.174 58.169h17.043l4.686-11.43h22.361l4.717 11.43h36.284V172.16l22.893 49.009h13.877l22.799-48.917v48.917h18.152l-.031-58.168h73.918v58.168h36.283l4.717-11.43h22.377l4.717 11.43h73.95v-8.963l6.639 8.963h36.846V163h-27.18l.032 58.169H.253zm678.262-38.442l-8.34-20.192-8.371 20.192h16.71zm-569.172 0l-8.34-20.192-8.371 20.192h16.71z"
        fill="#fff"
      />
    </svg>
  ),
  troy: (
    <svg
      viewBox="0 0 100 50"
      xmlns="http://www.w3.org/2000/svg"
      className="card-logo-svg"
    >
      <rect width="100" height="50" rx="5" fill="#00A651" />
      <text
        x="50"
        y="32"
        textAnchor="middle"
        fill="white"
        fontSize="18"
        fontWeight="bold"
      >
        TROY
      </text>
    </svg>
  ),
  unknown: (
    <svg
      viewBox="0 0 24 24"
      xmlns="http://www.w3.org/2000/svg"
      className="card-logo-svg card-logo-unknown"
    >
      <path
        d="M20 4H4c-1.11 0-1.99.89-1.99 2L2 18c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V6c0-1.11-.89-2-2-2zm0 14H4v-6h16v6zm0-10H4V6h16v2z"
        fill="#6c757d"
      />
    </svg>
  ),
};

// ═══════════════════════════════════════════════════════════════════════════
// KART TİPİ TESPİTİ
// BIN numarasına göre kart markasını belirler
// ═══════════════════════════════════════════════════════════════════════════
const detectCardType = (cardNumber) => {
  const cleanNumber = cardNumber?.replace(/\s/g, "") || "";

  if (/^4/.test(cleanNumber)) {
    return {
      type: "visa",
      name: "VISA",
      gradient: "linear-gradient(135deg, #1a1f71 0%, #4169e1 100%)",
    };
  }
  if (/^5[1-5]/.test(cleanNumber) || /^2[2-7]/.test(cleanNumber)) {
    return {
      type: "mastercard",
      name: "Mastercard",
      gradient: "linear-gradient(135deg, #0a0a0a 0%, #434343 100%)",
    };
  }
  if (/^3[47]/.test(cleanNumber)) {
    return {
      type: "amex",
      name: "American Express",
      gradient: "linear-gradient(135deg, #006fcf 0%, #00a4e4 100%)",
    };
  }
  if (/^9792/.test(cleanNumber)) {
    return {
      type: "troy",
      name: "TROY",
      gradient: "linear-gradient(135deg, #00a651 0%, #4caf50 100%)",
    };
  }

  return {
    type: "unknown",
    name: "",
    gradient: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
  };
};

// ═══════════════════════════════════════════════════════════════════════════
// KART NUMARASI MASKELEME
// ═══════════════════════════════════════════════════════════════════════════
const formatDisplayNumber = (cardNumber) => {
  const cleanNumber = cardNumber?.replace(/\s/g, "") || "";
  const paddedNumber = cleanNumber.padEnd(16, "•");

  // 4'lü gruplar halinde göster
  const groups = paddedNumber.match(/.{1,4}/g) || [
    "••••",
    "••••",
    "••••",
    "••••",
  ];
  return groups.join(" ");
};

// ═══════════════════════════════════════════════════════════════════════════
// ANA COMPONENT
// ═══════════════════════════════════════════════════════════════════════════
const CreditCardPreview = ({
  cardNumber = "",
  cardHolderName = "",
  expiryDate = "",
  isFlipped = false,
  cvv = "",
}) => {
  // Kart tipini tespit et
  const cardType = useMemo(() => detectCardType(cardNumber), [cardNumber]);

  // Gösterilecek değerleri hazırla
  const displayNumber = useMemo(
    () => formatDisplayNumber(cardNumber),
    [cardNumber],
  );
  const displayName = cardHolderName?.trim() || "KART SAHİBİ";
  const displayExpiry = expiryDate || "AA/YY";
  const displayCvv = cvv?.replace(/./g, "•") || "•••";

  return (
    <div className="credit-card-preview-container">
      <div className={`credit-card-preview ${isFlipped ? "flipped" : ""}`}>
        {/* Ön Yüz */}
        <div className="card-front" style={{ background: cardType.gradient }}>
          {/* Chip */}
          <div className="card-chip">
            <svg viewBox="0 0 50 40" className="chip-svg">
              <rect x="0" y="0" width="50" height="40" rx="5" fill="#d4af37" />
              <line
                x1="0"
                y1="13"
                x2="50"
                y2="13"
                stroke="#b8860b"
                strokeWidth="2"
              />
              <line
                x1="0"
                y1="27"
                x2="50"
                y2="27"
                stroke="#b8860b"
                strokeWidth="2"
              />
              <line
                x1="25"
                y1="0"
                x2="25"
                y2="40"
                stroke="#b8860b"
                strokeWidth="2"
              />
            </svg>
          </div>

          {/* Temassız ödeme ikonu */}
          <div className="contactless-icon">
            <svg viewBox="0 0 24 24" fill="white">
              <path
                d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8z"
                opacity="0.3"
              />
              <path d="M7.5 10.5c.83-1.75 2.63-3 4.5-3s3.67 1.25 4.5 3" />
              <path d="M5.5 8.5c1.38-2.75 4.22-4.5 6.5-4.5s5.12 1.75 6.5 4.5" />
              <path d="M9.5 12.5c.55-1.08 1.67-1.75 2.5-1.75s1.95.67 2.5 1.75" />
            </svg>
          </div>

          {/* Logo */}
          <div className="card-logo">{CardLogos[cardType.type]}</div>

          {/* Kart Numarası */}
          <div className="card-number">{displayNumber}</div>

          {/* Alt Bilgiler */}
          <div className="card-bottom">
            <div className="card-holder">
              <span className="label">Kart Sahibi</span>
              <span className="value">{displayName.toUpperCase()}</span>
            </div>
            <div className="card-expiry">
              <span className="label">Son Kul. Tar.</span>
              <span className="value">{displayExpiry}</span>
            </div>
          </div>
        </div>

        {/* Arka Yüz */}
        <div className="card-back">
          <div className="card-stripe"></div>
          <div className="card-cvv-section">
            <div className="cvv-label">CVV</div>
            <div className="cvv-band">
              <span className="cvv-value">{displayCvv}</span>
            </div>
          </div>
          <div className="card-back-info">
            <p>Bu kart sahibi adına düzenlenmiştir.</p>
            <p>Yetkisiz kullanım yasaktır.</p>
          </div>
        </div>
      </div>

      {/* Kart Tipi Etiketi */}
      {cardType.type !== "unknown" && (
        <div className="card-type-badge">{cardType.name}</div>
      )}
    </div>
  );
};

CreditCardPreview.propTypes = {
  cardNumber: PropTypes.string,
  cardHolderName: PropTypes.string,
  expiryDate: PropTypes.string,
  isFlipped: PropTypes.bool,
  cvv: PropTypes.string,
};

export default CreditCardPreview;
