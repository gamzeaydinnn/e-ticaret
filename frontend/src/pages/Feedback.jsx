import React, { useState } from "react";

export default function Feedback() {
  const [message, setMessage] = useState("");
  const [email, setEmail] = useState("");
  const [submitted, setSubmitted] = useState(false);

  const handleSubmit = (e) => {
    e.preventDefault();
    // Şimdilik sadece frontend tarafında formu gösteriyoruz.
    setSubmitted(true);
  };

  return (
    <div className="container py-5">
      <h1 className="h3 fw-bold mb-3" style={{ color: "#2d3748" }}>
        Geri Bildirim
      </h1>
      <p className="text-muted mb-4">
        Deneyiminizi daha iyi hale getirebilmemiz için görüş ve önerilerinizi
        bizimle paylaşabilirsiniz.
      </p>

      {submitted ? (
        <div className="alert alert-success">
          Geri bildiriminiz için teşekkür ederiz. En kısa sürede değerlendireceğiz.
        </div>
      ) : (
        <form onSubmit={handleSubmit} style={{ maxWidth: 600 }}>
          <div className="mb-3">
            <label className="form-label">E-posta (isteğe bağlı)</label>
            <input
              type="email"
              className="form-control"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="ornek@ornek.com"
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Mesajınız</label>
            <textarea
              className="form-control"
              rows="5"
              required
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              placeholder="Görüş, öneri veya şikayetinizi yazabilirsiniz..."
            ></textarea>
          </div>
          <button
            type="submit"
            className="btn btn-warning fw-bold"
            style={{ borderRadius: 20, paddingInline: 24 }}
          >
            Gönder
          </button>
        </form>
      )}
    </div>
  );
}

