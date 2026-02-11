import React, { useState } from "react";
import api from "../services/api";

export default function Feedback() {
  const [message, setMessage] = useState("");
  const [email, setEmail] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      const response = await api.post("/api/feedback", { email, message });
      if (response.success) {
        setSubmitted(true);
      } else {
        setError(response.message || "Bir hata oluştu.");
      }
    } catch (err) {
      setError(
        err.message || "Geri bildirim gönderilemedi. Lütfen tekrar deneyiniz.",
      );
    } finally {
      setLoading(false);
    }
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
          <i className="fas fa-check-circle me-2"></i>
          Geri bildiriminiz başarıyla iletildi. Teşekkür ederiz!
        </div>
      ) : (
        <form onSubmit={handleSubmit} style={{ maxWidth: 600 }}>
          {error && (
            <div className="alert alert-danger mb-3">
              <i className="fas fa-exclamation-circle me-2"></i>
              {error}
            </div>
          )}

          <div className="mb-3">
            <label className="form-label">E-posta (isteğe bağlı)</label>
            <input
              type="email"
              className="form-control"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="ornek@ornek.com"
              disabled={loading}
            />
            <small className="text-muted">
              Yanıt almak isterseniz e-posta adresinizi giriniz.
            </small>
          </div>

          <div className="mb-3">
            <label className="form-label">
              Mesajınız <span className="text-danger">*</span>
            </label>
            <textarea
              className="form-control"
              rows="5"
              required
              minLength={10}
              maxLength={2000}
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              placeholder="Görüş, öneri veya şikayetinizi yazabilirsiniz..."
              disabled={loading}
            ></textarea>
            <small className="text-muted">{message.length}/2000 karakter</small>
          </div>

          <button
            type="submit"
            className="btn btn-warning fw-bold"
            style={{ borderRadius: 20, paddingInline: 24 }}
            disabled={loading || message.length < 10}
          >
            {loading ? (
              <>
                <span
                  className="spinner-border spinner-border-sm me-2"
                  role="status"
                ></span>
                Gönderiliyor...
              </>
            ) : (
              <>
                <i className="fas fa-paper-plane me-2"></i>
                Gönder
              </>
            )}
          </button>
        </form>
      )}
    </div>
  );
}
